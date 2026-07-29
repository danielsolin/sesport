using System.Text.Json;

using Npgsql;
using NpgsqlTypes;

using SESport.Core.AI;
using SESport.Core.Sources;

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
}
