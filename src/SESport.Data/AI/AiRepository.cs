using System.Text;
using Npgsql;
using NpgsqlTypes;
using SESport.Core.AI.Abstractions;
using SESport.Core.AI.Models;

namespace SESport.Data.AI;

public sealed class AiRepository(NpgsqlDataSource dataSource)
   : IAiJobDefinitionRepository, IAiJobRunRepository
{
   public async Task<IReadOnlyList<AiRunListItem>> GetRunsAsync(
      string? jobId,
      string? statusId,
      CancellationToken cancellationToken
   )
   {
      var where = new List<string>();

      if(!string.IsNullOrWhiteSpace(jobId))
      {
         where.Add("r.job_id = @job_id");
      }

      if(!string.IsNullOrWhiteSpace(statusId))
      {
         where.Add("r.status_id = @status_id");
      }

      var sql = new StringBuilder()
         .AppendLine("select")
         .AppendLine("   r.id,")
         .AppendLine("   j.label,")
         .AppendLine("   p.label,")
         .AppendLine("   r.status_id,")
         .AppendLine("   r.started_at,")
         .AppendLine("   r.duration_seconds,")
         .AppendLine("   r.error_message")
         .AppendLine("from ai_job_runs r")
         .AppendLine("join ai_jobs j on j.id = r.job_id")
         .AppendLine("join ai_providers p on p.id = r.provider_id");

      if(where.Count > 0)
      {
         sql.AppendLine("where " + string.Join(" and ", where));
      }

      sql.AppendLine("order by r.started_at desc")
         .AppendLine("limit 200");

      await using var command = dataSource.CreateCommand(sql.ToString());
      if(!string.IsNullOrWhiteSpace(jobId))
      {
         command.Parameters.AddWithValue("job_id", jobId);
      }

      if(!string.IsNullOrWhiteSpace(statusId))
      {
         command.Parameters.AddWithValue("status_id", statusId);
      }

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var runs = new List<AiRunListItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         runs.Add(
            new AiRunListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetFieldValue<DateTimeOffset>(4),
               ReadNullableDecimal(reader, 5),
               ReadNullableString(reader, 6)
            )
         );
      }

      return runs;
   }

   public async Task<AiRunDetail?> GetRunAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            r.id,
            r.job_id,
            j.label,
            r.prompt_id,
            pr.version,
            r.provider_id,
            p.label,
            r.status_id,
            r.correlation_id,
            r.input_payload::text,
            r.rendered_prompt,
            r.raw_response::text,
            r.output_text,
            r.error_message,
            r.started_at,
            r.completed_at,
            r.duration_seconds,
            r.input_tokens,
            r.output_tokens,
            r.reasoning_tokens
         from ai_job_runs r
         join ai_jobs j on j.id = r.job_id
         join ai_providers p on p.id = r.provider_id
         join ai_job_prompts pr on pr.id = r.prompt_id
         where r.id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return new AiRunDetail(
         reader.GetGuid(0),
         reader.GetString(1),
         reader.GetString(2),
         reader.GetGuid(3),
         reader.GetInt32(4),
         reader.GetString(5),
         reader.GetString(6),
         reader.GetString(7),
         ReadNullableString(reader, 8),
         reader.GetString(9),
         reader.GetString(10),
         ReadNullableString(reader, 11),
         ReadNullableString(reader, 12),
         ReadNullableString(reader, 13),
         reader.GetFieldValue<DateTimeOffset>(14),
         ReadNullableDateTimeOffset(reader, 15),
         ReadNullableDecimal(reader, 16),
         ReadNullableInt32(reader, 17),
         ReadNullableInt32(reader, 18),
         ReadNullableInt32(reader, 19)
      );
   }

   public async Task<AiJobDefinition?> GetJobAsync(
      string jobId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select id, label, description, provider_id, output_mode, enabled
         from ai_jobs
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", jobId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return new AiJobDefinition(
         reader.GetString(0),
         reader.GetString(1),
         ReadNullableString(reader, 2),
         reader.GetString(3),
         reader.GetString(4),
         reader.GetBoolean(5)
      );
   }

   public async Task<AiPromptDefinition?> GetActivePromptAsync(
      string jobId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            id,
            job_id,
            version,
            system_prompt,
            user_prompt_template,
            output_schema::text,
            temperature,
            max_output_tokens,
            enabled
         from ai_job_prompts
         where job_id = @job_id
            and enabled = true
         order by version desc
         limit 1
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("job_id", jobId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return new AiPromptDefinition(
         reader.GetGuid(0),
         reader.GetString(1),
         reader.GetInt32(2),
         reader.GetString(3),
         reader.GetString(4),
         ReadNullableString(reader, 5),
         ReadNullableDecimal(reader, 6),
         ReadNullableInt32(reader, 7),
         reader.GetBoolean(8)
      );
   }

   public async Task<AiProviderDefinition?> GetProviderAsync(
      string providerId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            id,
            label,
            kind,
            base_address,
            model,
            api_key_source,
            request_options::text,
            enabled
         from ai_providers
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", providerId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return new AiProviderDefinition(
         reader.GetString(0),
         reader.GetString(1),
         reader.GetString(2),
         ReadNullableString(reader, 3),
         ReadNullableString(reader, 4),
         ReadNullableString(reader, 5),
         reader.GetString(6),
         reader.GetBoolean(7)
      );
   }

   public async Task StoreAsync(
      AiJobRun run,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into ai_job_runs (
            id, job_id, prompt_id, provider_id, status_id, correlation_id,
            input_payload, rendered_prompt, raw_response, output_text,
            error_message, started_at, completed_at, duration_seconds,
            input_tokens, output_tokens, reasoning_tokens
         )
         values (
            @id, @job_id, @prompt_id, @provider_id, @status_id,
            @correlation_id, @input_payload, @rendered_prompt,
            @raw_response, @output_text, @error_message, @started_at,
            @completed_at, @duration_seconds, @input_tokens,
            @output_tokens, @reasoning_tokens
         )
         """;

      await using var command = dataSource.CreateCommand(sql);
      AddRunParameters(command, run);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task UpdateAsync(
      AiJobRun run,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update ai_job_runs
         set
            status_id = @status_id,
            correlation_id = @correlation_id,
            raw_response = @raw_response,
            output_text = @output_text,
            error_message = @error_message,
            completed_at = @completed_at,
            duration_seconds = @duration_seconds,
            input_tokens = @input_tokens,
            output_tokens = @output_tokens,
            reasoning_tokens = @reasoning_tokens
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      AddRunUpdateParameters(command, run);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static void AddRunParameters(
      NpgsqlCommand command,
      AiJobRun run
   )
   {
      command.Parameters.AddWithValue("id", run.Id);
      command.Parameters.AddWithValue("job_id", run.JobId);
      command.Parameters.AddWithValue("prompt_id", run.PromptId);
      command.Parameters.AddWithValue("provider_id", run.ProviderId);
      command.Parameters.AddWithValue("status_id", ToStatusId(run.Status));
      command.Parameters.AddWithValue(
         "correlation_id",
         (object?)run.CorrelationId ?? DBNull.Value
      );
      AddJsonbParameter(command, "input_payload", run.InputPayloadJson);
      command.Parameters.AddWithValue("rendered_prompt", run.RenderedPrompt);
      AddJsonbParameter(command, "raw_response", run.RawResponseJson);
      command.Parameters.AddWithValue(
         "output_text",
         (object?)run.OutputText ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "error_message",
         (object?)run.ErrorMessage ?? DBNull.Value
      );
      command.Parameters.AddWithValue("started_at", run.StartedAt);
      command.Parameters.AddWithValue(
         "completed_at",
         (object?)run.CompletedAt ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "duration_seconds",
         (object?)run.DurationSeconds ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "input_tokens",
         (object?)run.InputTokens ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "output_tokens",
         (object?)run.OutputTokens ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "reasoning_tokens",
         (object?)run.ReasoningTokens ?? DBNull.Value
      );
   }

   private static void AddRunUpdateParameters(
      NpgsqlCommand command,
      AiJobRun run
   )
   {
      command.Parameters.AddWithValue("id", run.Id);
      command.Parameters.AddWithValue("status_id", ToStatusId(run.Status));
      command.Parameters.AddWithValue(
         "correlation_id",
         (object?)run.CorrelationId ?? DBNull.Value
      );
      AddJsonbParameter(command, "raw_response", run.RawResponseJson);
      command.Parameters.AddWithValue(
         "output_text",
         (object?)run.OutputText ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "error_message",
         (object?)run.ErrorMessage ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "completed_at",
         (object?)run.CompletedAt ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "duration_seconds",
         (object?)run.DurationSeconds ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "input_tokens",
         (object?)run.InputTokens ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "output_tokens",
         (object?)run.OutputTokens ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "reasoning_tokens",
         (object?)run.ReasoningTokens ?? DBNull.Value
      );
   }

   private static void AddJsonbParameter(
      NpgsqlCommand command,
      string name,
      string? value
   )
   {
      command.Parameters.Add(
         new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
         {
            Value = (object?)value ?? DBNull.Value
         }
      );
   }

   private static string ToStatusId(AiJobRunStatus status)
   {
      return status switch
      {
         AiJobRunStatus.Running => "running",
         AiJobRunStatus.Completed => "completed",
         AiJobRunStatus.Failed => "failed",
         _ => "pending"
      };
   }

   private static string? ReadNullableString(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static decimal? ReadNullableDecimal(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
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

   private static int? ReadNullableInt32(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
   }
}
