using Npgsql;
using NpgsqlTypes;

using SESport.Core.AI;
using SESport.Core.Formatting;
using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class ActivityParticipantAiResultRepository(
   NpgsqlDataSource dataSource
)
{
   public async Task<int> UpsertAsync(
      ActivityParticipantAiResultDraft result,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      try
      {
         await DeleteExistingRowsAsync(
            connection,
            transaction,
            result.ActivityId,
            result.JobId,
            cancellationToken
         );

         await DeleteExistingSourcesAsync(
            connection,
            transaction,
            result.ActivityId,
            cancellationToken
         );

         var setRowId = await InsertResultSetAsync(
            connection,
            transaction,
            result,
            cancellationToken
         );

         var uniqueSources = BuildUniqueSources(result);
         var sourceIds = await InsertSourcesAsync(
            connection,
            transaction,
            result.ActivityId,
            uniqueSources,
            cancellationToken
         );

         var insertedValues = new List<(Guid ValueId, int Index)>();
         for(var index = 0; index < result.Values.Count; index++)
         {
            var valueId = await InsertValueAsync(
               connection,
               transaction,
               result.ActivityId,
               result.JobId,
               setRowId,
               result.Values[index],
               cancellationToken
            );
            insertedValues.Add((valueId, index));
         }

         await InsertSetSourceLinksAsync(
            connection,
            transaction,
            result.ActivityId,
            result.JobId,
            setRowId,
            result.CheckedSources,
            sourceIds,
            cancellationToken
         );

         foreach(var (valueId, index) in insertedValues)
         {
            await InsertValueSourceLinksAsync(
               connection,
               transaction,
               result.ActivityId,
               result.JobId,
               valueId,
               result.Values[index],
               sourceIds,
               cancellationToken
            );
         }

         await transaction.CommitAsync(cancellationToken);
         return result.Values.Count;
      }
      catch
      {
         await transaction.RollbackAsync(cancellationToken);
         throw;
      }
   }

   public async Task<IReadOnlyList<ActivityParticipantAiResultSetRecord>>
      GetForActivityAsync(
         Guid activityId,
         CancellationToken cancellationToken
      )
   {
      var buildersByJobId = await LoadResultSetBuildersAsync(
         activityId,
         cancellationToken
      );

      if(buildersByJobId.Count == 0)
      {
         return [];
      }

      var valuesById = await LoadValuesAsync(
         activityId,
         buildersByJobId,
         cancellationToken
      );

      await LoadSourceRowsAsync(
         activityId,
         buildersByJobId,
         valuesById,
         cancellationToken
      );

      return buildersByJobId.Values
         .OrderByDescending(builder => builder.UpdatedAt)
         .ThenBy(builder => builder.JobId, StringComparer.Ordinal)
         .Select(builder => builder.ToRecord())
         .ToList();
   }

   private async Task<Dictionary<string, Builder>>
      LoadResultSetBuildersAsync(
         Guid activityId,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            s.id,
            s.job_id,
            coalesce(j.label, s.job_id) as job_label,
            s.run_id,
            r.status_id,
            r.output_text,
            r.started_at,
            r.completed_at,
            s.created_at,
            s.updated_at
         from activity_participant_ai_results s
         join ai_job_runs r on r.id = s.run_id
         left join ai_jobs j on j.id = s.job_id
         where s.activity_id = @activity_id
            and s.row_kind = @row_kind
         order by s.updated_at desc, s.job_id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue(
         "row_kind",
         ActivityParticipantAiResultRowKinds.Set
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var buildersByJobId = new Dictionary<string, Builder>(
         StringComparer.Ordinal
      );

      while(await reader.ReadAsync(cancellationToken))
      {
         var builder = new Builder(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetGuid(3),
            reader.GetString(4),
            AiRunSummaryFormatter.Format(
               ReadNullableString(reader, 5),
               reader.GetString(2)
            ),
            reader.GetFieldValue<DateTimeOffset>(6),
            ReadNullableDateTimeOffset(reader, 7),
            reader.GetFieldValue<DateTimeOffset>(8),
            reader.GetFieldValue<DateTimeOffset>(9)
         );
         buildersByJobId[builder.JobId] = builder;
      }

      return buildersByJobId;
   }

   private async Task<Dictionary<Guid, ValueBuilder>> LoadValuesAsync(
      Guid activityId,
      IReadOnlyDictionary<string, Builder> buildersByJobId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            v.id,
            v.job_id,
            v.entity_id,
            coalesce(e.canonical_name, '') as entity_name,
            v.field_key,
            v.value_text,
            v.value_json::text
         from activity_participant_ai_results v
         left join entities e on e.id = v.entity_id
         where v.activity_id = @activity_id
            and v.row_kind = @row_kind
         order by v.job_id, coalesce(e.canonical_name, ''), v.field_key, v.id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue(
         "row_kind",
         ActivityParticipantAiResultRowKinds.Value
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var valuesById = new Dictionary<Guid, ValueBuilder>();

      while(await reader.ReadAsync(cancellationToken))
      {
         if(reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) ||
            reader.IsDBNull(4) || reader.IsDBNull(6))
         {
            continue;
         }

         if(!buildersByJobId.TryGetValue(reader.GetString(1), out var builder))
         {
            continue;
         }

         var valueId = reader.GetGuid(0);
         var entityId = reader.GetGuid(2);
         var entityName = ReadNullableString(reader, 3);
         var fieldKey = reader.GetString(4);
         var valueText = ReadNullableString(reader, 5);
         var valueJson = reader.GetString(6);
         var value = builder.AddValue(
            valueId,
            entityId,
            string.IsNullOrWhiteSpace(entityName)
               ? entityId.ToString("N")
               : entityName,
            fieldKey,
            valueText,
            valueJson
         );
         valuesById[valueId] = value;
      }

      return valuesById;
   }

   private async Task LoadSourceRowsAsync(
      Guid activityId,
      IReadOnlyDictionary<string, Builder> buildersByJobId,
      IReadOnlyDictionary<Guid, ValueBuilder> valuesById,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            rs.parent_id,
            rs.job_id,
            rs.sort_order,
            src.url,
            src.title,
            src.excerpt
         from activity_participant_ai_results rs
         join sources src on src.id = rs.source_id
         where rs.activity_id = @activity_id
            and rs.row_kind = @row_kind
         order by rs.job_id, rs.sort_order, src.id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue(
         "row_kind",
         ActivityParticipantAiResultRowKinds.Source
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      while(await reader.ReadAsync(cancellationToken))
      {
         if(reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) ||
            reader.IsDBNull(3))
         {
            continue;
         }

         var parentId = reader.GetGuid(0);
         var jobId = reader.GetString(1);
         var source = ReadSourceEvidence(reader, 3, 4, 5);

         if(valuesById.TryGetValue(parentId, out var value))
         {
            value.Sources.Add(source);
            continue;
         }

         if(buildersByJobId.TryGetValue(jobId, out var builder) &&
            builder.SetRowId == parentId)
         {
            builder.CheckedSources.Add(source);
         }
      }
   }

   private static async Task DeleteExistingRowsAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      string jobId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from activity_participant_ai_results
         where activity_id = @activity_id
            and job_id = @job_id
         """;

      await using var command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText = sql;
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("job_id", jobId);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task DeleteExistingSourcesAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from sources src
         where src.correlation_type = @correlation_type
            and src.correlation_id = @correlation_id
            and src.kind = @kind
            and not exists (
               select 1
               from activity_participant_ai_results r
               where r.source_id = src.id
            )
         """;

      await using var command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText = sql;
      command.Parameters.AddWithValue(
         "correlation_type",
         SourceCorrelationTypes.Activity
      );
      command.Parameters.AddWithValue(
         "correlation_id",
         activityId.ToString()
      );
      command.Parameters.AddWithValue(
         "kind",
         SourceKinds.ParticipantStartEvidence
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static IReadOnlyList<SourceEvidenceDraft> BuildUniqueSources(
      ActivityParticipantAiResultDraft result
   )
   {
      var uniqueSources = new List<SourceEvidenceDraft>();
      var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      AddSources(result.CheckedSources, uniqueSources, seenUrls);
      foreach(var value in result.Values)
      {
         AddSources(value.Sources, uniqueSources, seenUrls);
      }

      return uniqueSources;
   }

   private static void AddSources(
      IReadOnlyList<SourceEvidenceDraft> sources,
      List<SourceEvidenceDraft> uniqueSources,
      HashSet<string> seenUrls
   )
   {
      foreach(var source in sources)
      {
         if(seenUrls.Add(source.Url))
         {
            uniqueSources.Add(source);
         }
      }
   }

   private static async Task<IReadOnlyDictionary<string, Guid>>
      InsertSourcesAsync(
         NpgsqlConnection connection,
         NpgsqlTransaction transaction,
         Guid activityId,
         IReadOnlyList<SourceEvidenceDraft> sources,
         CancellationToken cancellationToken
      )
   {
      var sourceIds = new Dictionary<string, Guid>(
         StringComparer.OrdinalIgnoreCase
      );

      foreach(var source in sources)
      {
         var sourceId = await InsertSourceAsync(
            connection,
            transaction,
            activityId,
            source,
            cancellationToken
         );
         sourceIds[source.Url] = sourceId;
      }

      return sourceIds;
   }

   private static async Task<Guid> InsertResultSetAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      ActivityParticipantAiResultDraft result,
      CancellationToken cancellationToken
   )
   {
      var id = Guid.NewGuid();
      const string sql = """
         insert into activity_participant_ai_results (
            id,
            activity_id,
            job_id,
            run_id,
            row_kind
         )
         values (
            @id,
            @activity_id,
            @job_id,
            @run_id,
            @row_kind
         )
         """;

      await using var command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText = sql;
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("activity_id", result.ActivityId);
      command.Parameters.AddWithValue("job_id", result.JobId);
      command.Parameters.AddWithValue("run_id", result.RunId);
      command.Parameters.AddWithValue(
         "row_kind",
         ActivityParticipantAiResultRowKinds.Set
      );

      await command.ExecuteNonQueryAsync(cancellationToken);
      return id;
   }

   private static async Task<Guid> InsertValueAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      string jobId,
      Guid setRowId,
      ActivityParticipantAiResultValueDraft value,
      CancellationToken cancellationToken
   )
   {
      var id = Guid.NewGuid();
      const string sql = """
         insert into activity_participant_ai_results (
            id,
            activity_id,
            job_id,
            parent_id,
            row_kind,
            entity_id,
            field_key,
            value_text,
            value_json
         )
         values (
            @id,
            @activity_id,
            @job_id,
            @parent_id,
            @row_kind,
            @entity_id,
            @field_key,
            @value_text,
            @value_json
         )
         """;

      await using var command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText = sql;
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("job_id", jobId);
      command.Parameters.AddWithValue("parent_id", setRowId);
      command.Parameters.AddWithValue(
         "row_kind",
         ActivityParticipantAiResultRowKinds.Value
      );
      command.Parameters.AddWithValue("entity_id", value.EntityId);
      command.Parameters.AddWithValue("field_key", value.FieldKey);
      command.Parameters.AddWithValue(
         "value_text",
         (object?)value.ValueText ?? DBNull.Value
      );
      AddJsonbParameter(command, "value_json", value.ValueJson);
      await command.ExecuteNonQueryAsync(cancellationToken);
      return id;
   }

   private static async Task InsertSetSourceLinksAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      string jobId,
      Guid setRowId,
      IReadOnlyList<SourceEvidenceDraft> sources,
      IReadOnlyDictionary<string, Guid> sourceIds,
      CancellationToken cancellationToken
   )
   {
      for(var index = 0; index < sources.Count; index++)
      {
         if(!sourceIds.TryGetValue(sources[index].Url, out var sourceId))
         {
            continue;
         }

         await InsertSourceRowAsync(
            connection,
            transaction,
            activityId,
            jobId,
            setRowId,
            sourceId,
            index,
            cancellationToken
         );
      }
   }

   private static async Task InsertValueSourceLinksAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      string jobId,
      Guid valueId,
      ActivityParticipantAiResultValueDraft value,
      IReadOnlyDictionary<string, Guid> sourceIds,
      CancellationToken cancellationToken
   )
   {
      for(var index = 0; index < value.Sources.Count; index++)
      {
         if(!sourceIds.TryGetValue(value.Sources[index].Url, out var sourceId))
         {
            continue;
         }

         await InsertSourceRowAsync(
            connection,
            transaction,
            activityId,
            jobId,
            valueId,
            sourceId,
            index,
            cancellationToken
         );
      }
   }

   private static async Task<Guid> InsertSourceAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      SourceEvidenceDraft source,
      CancellationToken cancellationToken
   )
   {
      var id = Guid.NewGuid();
      const string sql = """
         insert into sources (
            id,
            correlation_type,
            correlation_id,
            kind,
            url,
            title,
            excerpt,
            observed_at
         )
         values (
            @id,
            @correlation_type,
            @correlation_id,
            @kind,
            @url,
            @title,
            @excerpt,
            @observed_at
         )
         """;

      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue(
         "correlation_type",
         SourceCorrelationTypes.Activity
      );
      command.Parameters.AddWithValue(
         "correlation_id",
         activityId.ToString()
      );
      command.Parameters.AddWithValue(
         "kind",
         SourceKinds.ParticipantStartEvidence
      );
      command.Parameters.AddWithValue("url", source.Url);
      command.Parameters.AddWithValue(
         "title",
         (object?)source.Title ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "excerpt",
         (object?)source.Excerpt ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "observed_at",
         DateTimeOffset.UtcNow
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
      return id;
   }

   private static async Task InsertSourceRowAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      string jobId,
      Guid parentId,
      Guid sourceId,
      int sortOrder,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into activity_participant_ai_results (
            id,
            activity_id,
            job_id,
            parent_id,
            row_kind,
            source_id,
            sort_order
         )
         values (
            @id,
            @activity_id,
            @job_id,
            @parent_id,
            @row_kind,
            @source_id,
            @sort_order
         )
         """;

      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("job_id", jobId);
      command.Parameters.AddWithValue("parent_id", parentId);
      command.Parameters.AddWithValue(
         "row_kind",
         ActivityParticipantAiResultRowKinds.Source
      );
      command.Parameters.AddWithValue("source_id", sourceId);
      command.Parameters.AddWithValue("sort_order", sortOrder);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static SourceEvidenceDraft ReadSourceEvidence(
      NpgsqlDataReader reader,
      int urlOrdinal,
      int titleOrdinal,
      int excerptOrdinal
   )
   {
      return new SourceEvidenceDraft(
         reader.GetString(urlOrdinal),
         ReadNullableString(reader, titleOrdinal),
         ReadNullableString(reader, excerptOrdinal)
      );
   }

   private static string? ReadNullableString(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetString(ordinal);
   }

   private static DateTimeOffset? ReadNullableDateTimeOffset(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetFieldValue<DateTimeOffset>(ordinal);
   }

   private static void AddJsonbParameter(
      NpgsqlCommand command,
      string name,
      string? value
   )
   {
      var normalizedValue = PostgreSqlJson.Normalize(value);
      command.Parameters.Add(
         new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
         {
            Value = (object?)normalizedValue ?? DBNull.Value
         }
      );
   }

   private sealed class Builder(
      Guid SetRowId,
      string JobId,
      string JobLabel,
      Guid RunId,
      string RunStatusId,
      string? ResultSummary,
      DateTimeOffset StartedAt,
      DateTimeOffset? CompletedAt,
      DateTimeOffset CreatedAt,
      DateTimeOffset UpdatedAt
   )
   {
      public Guid SetRowId { get; } = SetRowId;

      public string JobId { get; } = JobId;

      public string JobLabel { get; } = JobLabel;

      public Guid RunId { get; } = RunId;

      public string RunStatusId { get; } = RunStatusId;

      public string? ResultSummary { get; } = ResultSummary;

      public DateTimeOffset StartedAt { get; } = StartedAt;

      public DateTimeOffset? CompletedAt { get; } = CompletedAt;

      public DateTimeOffset CreatedAt { get; } = CreatedAt;

      public DateTimeOffset UpdatedAt { get; } = UpdatedAt;

      public List<SourceEvidenceDraft> CheckedSources { get; } = [];

      public List<ValueBuilder> Values { get; } = [];

      public ValueBuilder AddValue(
         Guid id,
         Guid entityId,
         string entityName,
         string fieldKey,
         string? valueText,
         string valueJson
      )
      {
         var value = new ValueBuilder(
            id,
            entityId,
            entityName,
            fieldKey,
            valueText,
            valueJson
         );

         Values.Add(value);
         return value;
      }

      public ActivityParticipantAiResultSetRecord ToRecord()
      {
         return new ActivityParticipantAiResultSetRecord(
            JobId,
            JobLabel,
            RunId,
            RunStatusId,
            ResultSummary,
            StartedAt,
            CompletedAt,
            CreatedAt,
            UpdatedAt,
            CheckedSources,
            Values.Select(value => value.ToRecord()).ToList()
         );
      }
   }

   private sealed class ValueBuilder(
      Guid Id,
      Guid EntityId,
      string EntityName,
      string FieldKey,
      string? ValueText,
      string ValueJson
   )
   {
      public Guid Id { get; } = Id;

      public Guid EntityId { get; } = EntityId;

      public string EntityName { get; } = EntityName;

      public string FieldKey { get; } = FieldKey;

      public string? ValueText { get; } = ValueText;

      public string ValueJson { get; } = ValueJson;

      public List<SourceEvidenceDraft> Sources { get; } = [];

      public ActivityParticipantAiResultValueRecord ToRecord()
      {
         return new ActivityParticipantAiResultValueRecord(
            EntityId,
            EntityName,
            FieldKey,
            ValueText,
            ValueJson,
            Sources
         );
      }
   }
}
