using Npgsql;
using NpgsqlTypes;

using SESport.Core.AI;
using SESport.Core.Sources;
using SESport.Core.Formatting;
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
         await UpsertResultSetAsync(
            connection,
            transaction,
            result,
            cancellationToken
         );

         await DeleteExistingSourcesAsync(
            connection,
            transaction,
            result.ActivityId,
            cancellationToken
         );

         await DeleteExistingValuesAsync(
            connection,
            transaction,
            result.ActivityId,
            result.JobId,
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

         var valueCount = 0;
         foreach(var value in result.Values)
         {
            valueCount += await InsertValueAsync(
               connection,
               transaction,
               result.ActivityId,
               result.JobId,
               value,
               cancellationToken
            );
         }

         await InsertSetSourceLinksAsync(
            connection,
            transaction,
            result.ActivityId,
            result.JobId,
            result.CheckedSources,
            sourceIds,
            cancellationToken
         );

         foreach(var value in result.Values)
         {
            await InsertValueSourceLinksAsync(
               connection,
               transaction,
               result.ActivityId,
               result.JobId,
               value,
               sourceIds,
               cancellationToken
            );
         }

         await transaction.CommitAsync(cancellationToken);
         return valueCount;
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

      await LoadResultSetSourcesAsync(
         activityId,
         buildersByJobId,
         cancellationToken
      );
      await LoadValuesAsync(
         activityId,
         buildersByJobId,
         cancellationToken
      );
      await LoadValueSourcesAsync(
         activityId,
         buildersByJobId,
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
            s.job_id,
            coalesce(j.label, s.job_id) as job_label,
            s.run_id,
            r.status_id,
            r.output_text,
            r.started_at,
            r.completed_at,
            s.created_at,
            s.updated_at
         from activity_participant_ai_result_sets s
         join ai_job_runs r on r.id = s.run_id
         left join ai_jobs j on j.id = s.job_id
         where s.activity_id = @activity_id
         order by s.updated_at desc, s.job_id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var buildersByJobId = new Dictionary<string, Builder>(
         StringComparer.Ordinal
      );

      while(await reader.ReadAsync(cancellationToken))
      {
         var builder = new Builder(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetGuid(2),
            reader.GetString(3),
            AiRunSummaryFormatter.Format(
               ReadNullableString(reader, 4),
               reader.GetString(1)
            ),
            reader.GetFieldValue<DateTimeOffset>(5),
            ReadNullableDateTimeOffset(reader, 6),
            reader.GetFieldValue<DateTimeOffset>(7),
            reader.GetFieldValue<DateTimeOffset>(8)
         );
         buildersByJobId[builder.JobId] = builder;
      }

      return buildersByJobId;
   }

   private async Task LoadResultSetSourcesAsync(
      Guid activityId,
      IReadOnlyDictionary<string, Builder> buildersByJobId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            rs.job_id,
            rs.sort_order,
            src.url,
            src.title,
            src.excerpt
         from activity_participant_ai_result_sources rs
         join sources src on src.id = rs.source_id
         where rs.activity_id = @activity_id
            and rs.entity_id is null
            and rs.field_key is null
         order by rs.job_id, rs.sort_order, src.id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      while(await reader.ReadAsync(cancellationToken))
      {
         if(reader.IsDBNull(0) || reader.IsDBNull(2))
         {
            continue;
         }

         if(!buildersByJobId.TryGetValue(reader.GetString(0), out var builder))
         {
            continue;
         }

         builder.CheckedSources.Add(
            ReadSourceEvidence(reader, 2, 3, 4)
         );
      }
   }

   private async Task LoadValuesAsync(
      Guid activityId,
      IReadOnlyDictionary<string, Builder> buildersByJobId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            v.job_id,
            v.entity_id,
            coalesce(e.canonical_name, '') as entity_name,
            v.field_key,
            v.value_text,
            v.value_json::text
         from activity_participant_ai_result_values v
         left join entities e on e.id = v.entity_id
         where v.activity_id = @activity_id
         order by v.job_id, coalesce(e.canonical_name, ''), v.field_key
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      while(await reader.ReadAsync(cancellationToken))
      {
         if(reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(3) ||
            reader.IsDBNull(5))
         {
            continue;
         }

         if(!buildersByJobId.TryGetValue(reader.GetString(0), out var builder))
         {
            continue;
         }

         var entityId = reader.GetGuid(1);
         var entityName = ReadNullableString(reader, 2);
         var fieldKey = reader.GetString(3);
         var valueText = ReadNullableString(reader, 4);
         var valueJson = reader.GetString(5);
         builder.GetOrCreateValue(
            entityId,
            string.IsNullOrWhiteSpace(entityName)
               ? entityId.ToString("N")
               : entityName,
            fieldKey,
            valueText,
            valueJson
         );
      }
   }

   private async Task LoadValueSourcesAsync(
      Guid activityId,
      IReadOnlyDictionary<string, Builder> buildersByJobId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            rs.job_id,
            rs.entity_id,
            rs.field_key,
            rs.sort_order,
            src.url,
            src.title,
            src.excerpt
         from activity_participant_ai_result_sources rs
         join sources src on src.id = rs.source_id
         where rs.activity_id = @activity_id
            and rs.entity_id is not null
            and rs.field_key is not null
         order by rs.job_id, rs.entity_id, rs.field_key, rs.sort_order, src.id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      while(await reader.ReadAsync(cancellationToken))
      {
         if(reader.IsDBNull(0) || reader.IsDBNull(1) ||
            reader.IsDBNull(2) || reader.IsDBNull(4))
         {
            continue;
         }

         if(!buildersByJobId.TryGetValue(reader.GetString(0), out var builder))
         {
            continue;
         }

         var value = builder.GetValue(
            reader.GetGuid(1),
            reader.GetString(2)
         );

         if(value is null)
         {
            continue;
         }

         value.Sources.Add(ReadSourceEvidence(reader, 4, 5, 6));
      }
   }

   private static async Task UpsertResultSetAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      ActivityParticipantAiResultDraft result,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into activity_participant_ai_result_sets (
            activity_id,
            job_id,
            run_id
         )
         values (
            @activity_id,
            @job_id,
            @run_id
         )
         on conflict (activity_id, job_id) do update set
            run_id = excluded.run_id,
            updated_at = now()
         """;

      await using var command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText = sql;
      command.Parameters.AddWithValue("activity_id", result.ActivityId);
      command.Parameters.AddWithValue("job_id", result.JobId);
      command.Parameters.AddWithValue("run_id", result.RunId);

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
         delete from sources
         where correlation_type = @correlation_type
            and correlation_id = @correlation_id
            and kind = @kind
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

   private static async Task DeleteExistingValuesAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      string jobId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from activity_participant_ai_result_values
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

   private static async Task<int> InsertValueAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      string jobId,
      ActivityParticipantAiResultValueDraft value,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into activity_participant_ai_result_values (
            activity_id,
            job_id,
            entity_id,
            field_key,
            value_text,
            value_json
         )
         values (
            @activity_id,
            @job_id,
            @entity_id,
            @field_key,
            @value_text,
            @value_json
         )
         """;

      await using var command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText = sql;
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("job_id", jobId);
      command.Parameters.AddWithValue("entity_id", value.EntityId);
      command.Parameters.AddWithValue("field_key", value.FieldKey);
      command.Parameters.AddWithValue(
         "value_text",
         (object?)value.ValueText ?? DBNull.Value
      );
      AddJsonbParameter(command, "value_json", value.ValueJson);
      await command.ExecuteNonQueryAsync(cancellationToken);
      return 1;
   }

   private static async Task InsertSetSourceLinksAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      string jobId,
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

         await InsertSetSourceLinkAsync(
            connection,
            transaction,
            activityId,
            jobId,
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

         await InsertValueSourceLinkAsync(
            connection,
            transaction,
            activityId,
            jobId,
            value.EntityId,
            value.FieldKey,
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

   private static async Task InsertSetSourceLinkAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      string jobId,
      Guid sourceId,
      int sortOrder,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into activity_participant_ai_result_sources (
            id,
            activity_id,
            job_id,
            entity_id,
            field_key,
            source_id,
            sort_order
         )
         values (
            @id,
            @activity_id,
            @job_id,
            @entity_id,
            @field_key,
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
      command.Parameters.Add(
         new NpgsqlParameter("entity_id", NpgsqlDbType.Uuid)
         {
            Value = DBNull.Value
         }
      );
      command.Parameters.Add(
         new NpgsqlParameter("field_key", NpgsqlDbType.Text)
         {
            Value = DBNull.Value
         }
      );
      command.Parameters.AddWithValue("source_id", sourceId);
      command.Parameters.AddWithValue("sort_order", sortOrder);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task InsertValueSourceLinkAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      string jobId,
      Guid entityId,
      string fieldKey,
      Guid sourceId,
      int sortOrder,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into activity_participant_ai_result_sources (
            id,
            activity_id,
            job_id,
            entity_id,
            field_key,
            source_id,
            sort_order
         )
         values (
            @id,
            @activity_id,
            @job_id,
            @entity_id,
            @field_key,
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
      command.Parameters.AddWithValue("entity_id", entityId);
      command.Parameters.AddWithValue("field_key", fieldKey);
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
      private readonly Dictionary<ValueKey, ValueBuilder> valueBuilders = [];

      public List<SourceEvidenceDraft> CheckedSources { get; } = [];

      public string JobId { get; } = JobId;

      public string JobLabel { get; } = JobLabel;

      public Guid RunId { get; } = RunId;

      public string RunStatusId { get; } = RunStatusId;

      public string? ResultSummary { get; } = ResultSummary;

      public DateTimeOffset StartedAt { get; } = StartedAt;

      public DateTimeOffset? CompletedAt { get; } = CompletedAt;

      public DateTimeOffset CreatedAt { get; } = CreatedAt;

      public DateTimeOffset UpdatedAt { get; } = UpdatedAt;

      public List<ValueBuilder> Values { get; } = [];

      public ValueBuilder GetOrCreateValue(
         Guid entityId,
         string entityName,
         string fieldKey,
         string? valueText,
         string valueJson
      )
      {
         var key = new ValueKey(entityId, fieldKey);

         if(!valueBuilders.TryGetValue(key, out var value))
         {
            value = new ValueBuilder(
               entityId,
               entityName,
               fieldKey,
               valueText,
               valueJson
            );
            valueBuilders[key] = value;
            Values.Add(value);
         }

         return value;
      }

      public ValueBuilder? GetValue(Guid entityId, string fieldKey)
      {
         return valueBuilders.TryGetValue(
               new ValueKey(entityId, fieldKey),
               out var value
            )
            ? value
            : null;
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
      Guid EntityId,
      string EntityName,
      string FieldKey,
      string? ValueText,
      string ValueJson
   )
   {
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

   private readonly record struct ValueKey(
      Guid EntityId,
      string FieldKey
   );
}
