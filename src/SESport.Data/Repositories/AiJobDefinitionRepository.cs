using Npgsql;

using SESport.Core.AI;

namespace SESport.Data.Repositories;

public sealed class AiJobDefinitionRepository(NpgsqlDataSource dataSource)
   : IAiJobDefinitionRepository
{
   public async Task<AiJobDefinition?> GetJobAsync(
      string jobId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            id,
            label,
            description,
            provider_id,
            model,
            queue_priority,
            output_mode,
            tools_json::text,
            conditional_tools_json::text,
            tool_call_max_tokens,
            requires_web_search,
            include_social_media,
            active_prompt_id,
            enabled
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
         PostgresHelpers.ReadNullableString(reader, 2),
         reader.GetString(3),
         reader.GetString(6),
         PostgresHelpers.ReadNullableString(reader, 7),
         PostgresHelpers.ReadNullableString(reader, 8),
         PostgresHelpers.ReadNullableInt32(reader, 9),
         reader.GetBoolean(10),
         reader.GetBoolean(13),
         PostgresHelpers.ReadNullableGuid(reader, 12),
         PostgresHelpers.ReadNullableString(reader, 4),
         reader.GetInt32(5),
         reader.GetBoolean(11)
      );
   }

   public async Task<AiPromptDefinition?> GetActivePromptAsync(
      string jobId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select active_prompt_id
         from ai_jobs
         where id = @job_id
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

      if(reader.IsDBNull(0))
      {
         return await GetLatestEnabledPromptAsync(
            jobId,
            cancellationToken
         );
      }

      var promptId = reader.GetGuid(0);
      const string promptSql = """
         select
            id,
            job_id,
            version,
            system_prompt,
            user_prompt_template,
            output_schema::text,
            request_options::text,
            temperature,
            max_output_tokens,
            max_tool_rounds,
            min_tool_rounds,
            enabled,
            codex_reasoning_effort
         from ai_job_prompts
         where id = @id
         """;

      await using var promptCommand = dataSource.CreateCommand(promptSql);
      promptCommand.Parameters.AddWithValue("id", promptId);
      await using var promptReader = await promptCommand.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await promptReader.ReadAsync(cancellationToken))
      {
         return await GetLatestEnabledPromptAsync(
            jobId,
            cancellationToken
         );
      }

      return new AiPromptDefinition(
         promptReader.GetGuid(0),
         promptReader.GetString(1),
         promptReader.GetInt32(2),
         promptReader.GetString(3),
         promptReader.GetString(4),
         PostgresHelpers.ReadNullableString(promptReader, 5),
         PostgresHelpers.ReadNullableString(promptReader, 6) ?? "{}",
         PostgresHelpers.ReadNullableDecimal(promptReader, 7),
         PostgresHelpers.ReadNullableInt32(promptReader, 8),
         PostgresHelpers.ReadNullableInt32(promptReader, 9),
         promptReader.GetBoolean(11),
         PostgresHelpers.ReadNullableInt32(promptReader, 10),
         PostgresHelpers.ReadNullableString(promptReader, 12)
      );
   }

   public async Task<AiPromptDefinition?> GetPromptAsync(
      Guid promptId,
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
            request_options::text,
            temperature,
            max_output_tokens,
            max_tool_rounds,
            min_tool_rounds,
            enabled,
            codex_reasoning_effort
         from ai_job_prompts
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", promptId);
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
         PostgresHelpers.ReadNullableString(reader, 5),
         PostgresHelpers.ReadNullableString(reader, 6) ?? "{}",
         PostgresHelpers.ReadNullableDecimal(reader, 7),
         PostgresHelpers.ReadNullableInt32(reader, 8),
         PostgresHelpers.ReadNullableInt32(reader, 9),
         reader.GetBoolean(11),
         PostgresHelpers.ReadNullableInt32(reader, 10),
         PostgresHelpers.ReadNullableString(reader, 12)
      );
   }

   private async Task<AiPromptDefinition?> GetLatestEnabledPromptAsync(
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
            request_options::text,
            temperature,
            max_output_tokens,
            max_tool_rounds,
            min_tool_rounds,
            enabled,
            codex_reasoning_effort
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
      PostgresHelpers.ReadNullableString(reader, 5),
      PostgresHelpers.ReadNullableString(reader, 6) ?? "{}",
      PostgresHelpers.ReadNullableDecimal(reader, 7),
      PostgresHelpers.ReadNullableInt32(reader, 8),
      PostgresHelpers.ReadNullableInt32(reader, 9),
      reader.GetBoolean(11),
      PostgresHelpers.ReadNullableInt32(reader, 10),
      PostgresHelpers.ReadNullableString(reader, 12)
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
         PostgresHelpers.ReadNullableString(reader, 3),
         PostgresHelpers.ReadNullableString(reader, 4),
         PostgresHelpers.ReadNullableString(reader, 5),
         reader.GetString(6),
         reader.GetBoolean(7)
      );
   }

   private static DateOnly? ReadNullableDateOnly(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetFieldValue<DateOnly>(ordinal);
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

}
