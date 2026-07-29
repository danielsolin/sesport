using System.Text.Json;

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

         await DeleteExistingValuesAsync(
            connection,
            transaction,
            result.ActivityId,
            result.JobId,
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
            s.updated_at,
            s.checked_sources::text,
            v.entity_id,
            coalesce(e.canonical_name, '') as entity_name,
            v.field_key,
            v.value_text,
            v.value_json::text,
            v.sources::text
         from activity_participant_ai_result_sets s
         join ai_job_runs r on r.id = s.run_id
         left join ai_jobs j on j.id = s.job_id
         left join activity_participant_ai_result_values v
            on v.activity_id = s.activity_id
           and v.job_id = s.job_id
         left join entities e on e.id = v.entity_id
         where s.activity_id = @activity_id
         order by s.updated_at desc, s.job_id, e.canonical_name, v.field_key
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var setsByKey = new Dictionary<AiResultSetKey, Builder>();

      while(await reader.ReadAsync(cancellationToken))
      {
         var key = new AiResultSetKey(
            reader.GetString(0),
            reader.GetGuid(2)
         );

         if(!setsByKey.TryGetValue(key, out var builder))
         {
            builder = new Builder(
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
               reader.GetFieldValue<DateTimeOffset>(8),
               ReadSourceEvidenceList(ReadNullableString(reader, 9))
            );
            setsByKey[key] = builder;
         }

         if(reader.IsDBNull(10) || reader.IsDBNull(12) || reader.IsDBNull(14))
         {
            continue;
         }

         var entityId = reader.GetGuid(10);
         var entityName = ReadNullableString(reader, 11);
         var fieldKey = reader.GetString(12);
         var valueText = ReadNullableString(reader, 13);
         var valueJson = reader.GetString(14);
         var sources = ReadSourceEvidenceList(ReadNullableString(reader, 15));

         builder.Values.Add(
            new ActivityParticipantAiResultValueRecord(
               entityId,
               string.IsNullOrWhiteSpace(entityName)
                  ? entityId.ToString("N")
                  : entityName,
               fieldKey,
               valueText,
               valueJson,
               sources
            )
         );
      }

      return setsByKey.Values
         .OrderByDescending(builder => builder.UpdatedAt)
         .ThenBy(builder => builder.JobId, StringComparer.Ordinal)
         .Select(builder => builder.ToRecord())
         .ToList();
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
            run_id,
            checked_sources
         )
         values (
            @activity_id,
            @job_id,
            @run_id,
            @checked_sources
         )
         on conflict (activity_id, job_id) do update set
            run_id = excluded.run_id,
            checked_sources = excluded.checked_sources,
            updated_at = now()
         """;

      await using var command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText = sql;
      command.Parameters.AddWithValue("activity_id", result.ActivityId);
      command.Parameters.AddWithValue("job_id", result.JobId);
      command.Parameters.AddWithValue("run_id", result.RunId);
      AddJsonbParameter(
         command,
         "checked_sources",
         JsonSerializer.Serialize(result.CheckedSources)
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
            value_json,
            sources
         )
         values (
            @activity_id,
            @job_id,
            @entity_id,
            @field_key,
            @value_text,
            @value_json,
            @sources
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
      AddJsonbParameter(
         command,
         "sources",
         JsonSerializer.Serialize(value.Sources)
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
      return 1;
   }

   private static IReadOnlyList<SourceEvidenceDraft> ReadSourceEvidenceList(
      string? value
   )
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return [];
      }

      try
      {
         return JsonSerializer.Deserialize<SourceEvidenceDraft[]>(
               value
            ) ?? [];
      }
      catch(JsonException)
      {
         return [];
      }
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

   private readonly record struct AiResultSetKey(
      string JobId,
      Guid RunId
   );

   private sealed class Builder(
      string JobId,
      string JobLabel,
      Guid RunId,
      string RunStatusId,
      string? ResultSummary,
      DateTimeOffset StartedAt,
      DateTimeOffset? CompletedAt,
      DateTimeOffset CreatedAt,
      DateTimeOffset UpdatedAt,
      IReadOnlyList<SourceEvidenceDraft> CheckedSources
   )
   {
      public List<ActivityParticipantAiResultValueRecord> Values { get; } = [];

      public string JobId { get; } = JobId;

      public string JobLabel { get; } = JobLabel;

      public Guid RunId { get; } = RunId;

      public string RunStatusId { get; } = RunStatusId;

      public string? ResultSummary { get; } = ResultSummary;

      public DateTimeOffset StartedAt { get; } = StartedAt;

      public DateTimeOffset? CompletedAt { get; } = CompletedAt;

      public DateTimeOffset CreatedAt { get; } = CreatedAt;

      public DateTimeOffset UpdatedAt { get; } = UpdatedAt;

      public IReadOnlyList<SourceEvidenceDraft> CheckedSources { get; } =
         CheckedSources;

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
            Values
         );
      }
   }
}
