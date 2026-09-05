using Npgsql;

using SESport.Core.AI;
using SESport.Core.Sources;
using SESport.Data.Models;

using System.Text.Json;

namespace SESport.Data.Activities;

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

         var uniqueSources = BuildUniqueSources(result);
         var sourceIds = await InsertSourcesAsync(
            connection,
            transaction,
            result.ActivityId,
            uniqueSources,
            cancellationToken
         );

         if(uniqueSources.Count == 0)
         {
            throw new InvalidOperationException(
               "Participant AI results require at least one source."
            );
         }

         for(var index = 0; index < result.Values.Count; index++)
         {
            var value = result.Values[index];

            if(!TryGetSourceId(value, sourceIds, out var sourceId))
            {
               throw new InvalidOperationException(
                  "Participant AI result value requires a source."
               );
            }

            await InsertValueAsync(
               connection,
               transaction,
               result.ActivityId,
               result.JobId,
               result.RunId,
               value,
               sourceId,
               index,
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

      await LoadValueRowsAsync(
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

   public async Task<bool> UpdateValueAsync(
      Guid id,
      string? valueText,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update activity_participant_ai_results
         set value_text = @value_text,
            value_json = @value_json,
            updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "value_text",
         (object?)valueText ?? DBNull.Value
      );
      PostgresHelpers.AddJsonbParameter(
         command,
         "value_json",
         JsonSerializer.Serialize(valueText)
      );
      command.Parameters.AddWithValue("id", id);

      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }

   private async Task<Dictionary<string, Builder>>
      LoadResultSetBuildersAsync(
         Guid activityId,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            r.job_id,
            coalesce(j.label, r.job_id) as job_label,
            r.run_id,
            run.status_id,
            run.output_text,
            run.started_at,
            run.completed_at,
            min(r.created_at) as created_at,
            max(r.updated_at) as updated_at
         from activity_participant_ai_results r
         left join ai_job_runs run on run.id = r.run_id
         left join ai_jobs j on j.id = r.job_id
         where r.activity_id = @activity_id
         group by
            r.job_id,
            j.label,
            r.run_id,
            run.status_id,
            run.output_text,
            run.started_at,
            run.completed_at
         order by max(r.updated_at) desc, r.job_id
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
            PostgresHelpers.ReadNullableGuid(reader, 2),
            PostgresHelpers.ReadNullableString(reader, 3),
            AiRunSummaryFormatter.Format(
               PostgresHelpers.ReadNullableString(reader, 4),
               reader.GetString(1)
            ),
            ReadNullableDateTimeOffset(reader, 5),
            ReadNullableDateTimeOffset(reader, 6),
            reader.GetFieldValue<DateTimeOffset>(7),
            reader.GetFieldValue<DateTimeOffset>(8)
         );
         buildersByJobId[builder.JobId] = builder;
      }

      return buildersByJobId;
   }

   private async Task LoadValueRowsAsync(
      Guid activityId,
      IReadOnlyDictionary<string, Builder> buildersByJobId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            r.id,
            r.job_id,
            r.entity_id,
            coalesce(e.canonical_name, '') as entity_name,
            r.field_key,
            r.value_text,
            r.value_json::text,
            r.sort_order,
            src.url,
            src.title,
            src.excerpt
         from activity_participant_ai_results r
         left join entities e on e.id = r.entity_id
         left join sources src on src.id = r.source_id
         where r.activity_id = @activity_id
         order by r.job_id, r.sort_order, r.id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      while(await reader.ReadAsync(cancellationToken))
      {
         if(reader.IsDBNull(0) || reader.IsDBNull(1) ||
            reader.IsDBNull(2) || reader.IsDBNull(4) ||
            reader.IsDBNull(6) || reader.IsDBNull(7))
         {
            continue;
         }

         if(!buildersByJobId.TryGetValue(reader.GetString(1), out var builder))
         {
            continue;
         }

         var id = reader.GetGuid(0);
         var entityId = reader.GetGuid(2);
         var entityName = PostgresHelpers.ReadNullableString(reader, 3);
         var fieldKey = reader.GetString(4);
         var valueText = PostgresHelpers.ReadNullableString(reader, 5);
         var valueJson = reader.GetString(6);
         var source = ReadSourceEvidence(reader, 8, 9, 10);

         builder.AddValue(
            id,
            entityId,
            string.IsNullOrWhiteSpace(entityName)
               ? entityId.ToString("N")
               : entityName,
            fieldKey,
            valueText,
            valueJson,
            source
         );
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

   private static bool TryGetSourceId(
      ActivityParticipantAiResultValueDraft value,
      IReadOnlyDictionary<string, Guid> sourceIds,
      out Guid sourceId
   )
   {
      foreach(var source in value.Sources)
      {
         if(sourceIds.TryGetValue(source.Url, out sourceId))
         {
            return true;
         }
      }

      sourceId = default;
      return false;
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

   private static async Task InsertValueAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      string jobId,
      Guid runId,
      ActivityParticipantAiResultValueDraft value,
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
            run_id,
            entity_id,
            field_key,
            value_text,
            value_json,
            source_id,
            sort_order
         )
         values (
            @id,
            @activity_id,
            @job_id,
            @run_id,
            @entity_id,
            @field_key,
            @value_text,
            @value_json,
            @source_id,
            @sort_order
         )
         """;

      await using var command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText = sql;
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("job_id", jobId);
      command.Parameters.AddWithValue("run_id", runId);
      command.Parameters.AddWithValue("entity_id", value.EntityId);
      command.Parameters.AddWithValue("field_key", value.FieldKey);
      command.Parameters.AddWithValue(
         "value_text",
         (object?)value.ValueText ?? DBNull.Value
      );
      PostgresHelpers.AddJsonbParameter(command, "value_json", value.ValueJson);
      command.Parameters.AddWithValue("source_id", sourceId);
      command.Parameters.AddWithValue("sort_order", sortOrder);
      await command.ExecuteNonQueryAsync(cancellationToken);
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

   private static SourceEvidenceDraft? ReadSourceEvidence(
      NpgsqlDataReader reader,
      int urlOrdinal,
      int titleOrdinal,
      int excerptOrdinal
   )
   {
      if(reader.IsDBNull(urlOrdinal))
      {
         return null;
      }

      return new SourceEvidenceDraft(
         reader.GetString(urlOrdinal),
         PostgresHelpers.ReadNullableString(reader, titleOrdinal),
         PostgresHelpers.ReadNullableString(reader, excerptOrdinal)
      );
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

   private sealed class Builder(
      string JobId,
      string JobLabel,
      Guid? RunId,
      string? RunStatusId,
      string? ResultSummary,
      DateTimeOffset? StartedAt,
      DateTimeOffset? CompletedAt,
      DateTimeOffset CreatedAt,
      DateTimeOffset UpdatedAt
   )
   {
      private readonly HashSet<string> checkedSourceUrls =
         new(StringComparer.OrdinalIgnoreCase);

      public List<SourceEvidenceDraft> CheckedSources { get; } = [];

      public List<ValueBuilder> Values { get; } = [];

      public string JobId { get; } = JobId;

      public string JobLabel { get; } = JobLabel;

      public Guid? RunId { get; } = RunId;

      public string? RunStatusId { get; } = RunStatusId;

      public string? ResultSummary { get; } = ResultSummary;

      public DateTimeOffset? StartedAt { get; } = StartedAt;

      public DateTimeOffset? CompletedAt { get; } = CompletedAt;

      public DateTimeOffset CreatedAt { get; } = CreatedAt;

      public DateTimeOffset UpdatedAt { get; } = UpdatedAt;

      public void AddCheckedSource(SourceEvidenceDraft source)
      {
         if(checkedSourceUrls.Add(source.Url))
         {
            CheckedSources.Add(source);
         }
      }

      public ValueBuilder AddValue(
         Guid id,
         Guid entityId,
         string entityName,
         string fieldKey,
         string? valueText,
         string valueJson,
         SourceEvidenceDraft? source
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
         if(source is not null)
         {
            value.Sources.Add(source);
            AddCheckedSource(source);
         }

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
            Id,
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
