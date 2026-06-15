using Npgsql;
using NpgsqlTypes;
using SESport.AI.Models;

namespace SESport.AI.Persistence;

public sealed class AiAdminRepository(NpgsqlDataSource dataSource)
{
   public async Task<IReadOnlyList<AiProviderListItem>> GetProvidersAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select id, label, kind, base_address, model, enabled
         from ai_providers
         order by label
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var items = new List<AiProviderListItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         items.Add(
            new AiProviderListItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2),
               ReadNullableString(reader, 3),
               ReadNullableString(reader, 4),
               reader.GetBoolean(5)
            )
         );
      }

      return items;
   }

   public async Task<AiProviderEditModel?> GetProviderForEditAsync(
      string id,
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
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return new AiProviderEditModel
      {
         OriginalId = reader.GetString(0),
         Id = reader.GetString(0),
         Label = reader.GetString(1),
         Kind = reader.GetString(2),
         BaseAddress = ReadNullableString(reader, 3),
         Model = ReadNullableString(reader, 4),
         ApiKeySource = ReadNullableString(reader, 5),
         RequestOptionsJson = ReadNullableString(reader, 6) ?? "{}",
         Enabled = reader.GetBoolean(7)
      };
   }

   public async Task SaveProviderAsync(
      AiProviderEditModel model,
      CancellationToken cancellationToken
   )
   {
      var originalId = model.OriginalId?.Trim();
      var id = string.IsNullOrWhiteSpace(model.Id)
         ? Guid.NewGuid().ToString()
         : model.Id.Trim();

      if(string.IsNullOrWhiteSpace(originalId))
      {
         const string insertSql = """
            insert into ai_providers (
               id, label, kind, base_address, model, api_key_source,
               request_options, enabled
            )
            values (
               @id, @label, @kind, @base_address, @model, @api_key_source,
               @request_options, @enabled
            )
            """;

         await using var insertCommand = dataSource.CreateCommand(insertSql);
         AddProviderParameters(insertCommand, id, model);
         await insertCommand.ExecuteNonQueryAsync(cancellationToken);
         return;
      }

      const string updateSql = """
         update ai_providers
         set
            id = @id,
            label = @label,
            kind = @kind,
            base_address = @base_address,
            model = @model,
            api_key_source = @api_key_source,
            request_options = @request_options,
            enabled = @enabled,
            updated_at = now()
         where id = @original_id
         """;

      await using var updateCommand = dataSource.CreateCommand(updateSql);
      AddProviderParameters(updateCommand, id, model);
      updateCommand.Parameters.AddWithValue("original_id", originalId);
      await updateCommand.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<IReadOnlyList<AiJobListItem>> GetJobsAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            j.id,
            j.label,
            j.provider_id,
            j.output_mode,
            p.version,
            j.enabled
         from ai_jobs j
         left join ai_job_prompts p on p.id = j.active_prompt_id
         order by j.label
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var items = new List<AiJobListItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         items.Add(
            new AiJobListItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               ReadNullableInt32(reader, 4),
               reader.GetBoolean(5)
            )
         );
      }

      return items;
   }

   public async Task<AiJobEditModel?> GetJobForEditAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            id,
            label,
            description,
            provider_id,
            output_mode,
            tools_json::text,
            active_prompt_id,
            requires_web_search,
            enabled
         from ai_jobs
         where id = @id
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

      return new AiJobEditModel
      {
         OriginalId = reader.GetString(0),
         Id = reader.GetString(0),
         Label = reader.GetString(1),
         Description = ReadNullableString(reader, 2),
         ProviderId = reader.GetString(3),
         OutputMode = reader.GetString(4),
         ToolsJson = ReadNullableString(reader, 5),
         ActivePromptId = ReadNullableGuid(reader, 6)?.ToString(),
         RequiresWebSearch = reader.GetBoolean(7),
         Enabled = reader.GetBoolean(8)
      };
   }

   public async Task SaveJobAsync(
      AiJobEditModel model,
      CancellationToken cancellationToken
   )
   {
      var originalId = model.OriginalId?.Trim();
      var id = string.IsNullOrWhiteSpace(model.Id)
         ? Guid.NewGuid().ToString()
         : model.Id.Trim();

      if(string.IsNullOrWhiteSpace(originalId))
      {
         const string insertSql = """
            insert into ai_jobs (
               id,
               label,
            description,
            provider_id,
            output_mode,
            tools_json,
            active_prompt_id,
            requires_web_search,
            enabled
         )
         values (
               @id,
               @label,
            @description,
            @provider_id,
            @output_mode,
            @tools_json,
            @active_prompt_id,
            @requires_web_search,
            @enabled
         )
         """;

         await using var insertCommand = dataSource.CreateCommand(insertSql);
         AddJobParameters(insertCommand, id, model);
         await insertCommand.ExecuteNonQueryAsync(cancellationToken);
         return;
      }

      const string updateSql = """
         update ai_jobs
         set
            id = @id,
            label = @label,
            description = @description,
            provider_id = @provider_id,
            output_mode = @output_mode,
            tools_json = @tools_json,
            active_prompt_id = @active_prompt_id,
            requires_web_search = @requires_web_search,
            enabled = @enabled,
            updated_at = now()
         where id = @original_id
         """;

      await using var updateCommand = dataSource.CreateCommand(updateSql);
      AddJobParameters(updateCommand, id, model);
      updateCommand.Parameters.AddWithValue("original_id", originalId);
      await updateCommand.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<IReadOnlyList<AiPromptListItem>> GetPromptsAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            p.id::text,
            p.job_id,
            j.label,
            p.version,
            p.system_prompt,
            p.user_prompt_template,
            p.temperature,
            p.enabled
         from ai_job_prompts p
         join ai_jobs j on j.id = p.job_id
         order by p.job_id, p.version desc
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var items = new List<AiPromptListItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         items.Add(
            new AiPromptListItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetInt32(3),
               reader.GetString(4),
               reader.GetString(5),
               ReadNullableDecimal(reader, 6),
               reader.GetBoolean(7)
            )
         );
      }

      return items;
   }

   public async Task<IReadOnlyList<AiPromptListItem>> GetJobPromptsAsync(
      string jobId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            p.id::text,
            p.job_id,
            j.label,
            p.version,
            p.system_prompt,
            p.user_prompt_template,
            p.temperature,
            p.enabled
         from ai_job_prompts p
         join ai_jobs j on j.id = p.job_id
         where p.job_id = @job_id
         order by p.version desc
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("job_id", jobId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var items = new List<AiPromptListItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         items.Add(
            new AiPromptListItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetInt32(3),
               reader.GetString(4),
               reader.GetString(5),
               ReadNullableDecimal(reader, 6),
               reader.GetBoolean(7)
            )
         );
      }

      return items;
   }

   public async Task<AiPromptEditModel?> GetPromptForEditAsync(
      string id,
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
            enabled
         from ai_job_prompts
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", Guid.Parse(id));
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return new AiPromptEditModel
      {
         OriginalId = reader.GetGuid(0).ToString(),
         Id = reader.GetGuid(0).ToString(),
         JobId = reader.GetString(1),
         Version = reader.GetInt32(2),
         SystemPrompt = reader.GetString(3),
         UserPromptTemplate = reader.GetString(4),
         OutputSchemaJson = ReadNullableString(reader, 5),
         RequestOptionsJson = ReadNullableString(reader, 6) ?? "{}",
         Temperature = ReadNullableDecimal(reader, 7),
         MaxOutputTokens = ReadNullableInt32(reader, 8),
         MaxToolRounds = ReadNullableInt32(reader, 9),
         Enabled = reader.GetBoolean(10)
      };
   }

   public async Task SavePromptAsync(
      AiPromptEditModel model,
      CancellationToken cancellationToken
   )
   {
      var originalId = model.OriginalId?.Trim();
      var id = string.IsNullOrWhiteSpace(model.Id)
         ? Guid.NewGuid().ToString()
         : model.Id.Trim();

      if(string.IsNullOrWhiteSpace(originalId))
      {
         const string insertSql = """
            insert into ai_job_prompts (
               id, job_id, version, system_prompt, user_prompt_template,
               output_schema, request_options, temperature,
               max_output_tokens, max_tool_rounds, enabled
            )
            values (
               @id, @job_id, @version, @system_prompt,
               @user_prompt_template, @output_schema, @request_options,
               @temperature, @max_output_tokens, @max_tool_rounds, @enabled
            )
            """;

         await using var insertCommand = dataSource.CreateCommand(insertSql);
         AddPromptParameters(insertCommand, id, model);
         await insertCommand.ExecuteNonQueryAsync(cancellationToken);
         return;
      }

      const string updateSql = """
         update ai_job_prompts
         set
            id = @id,
            job_id = @job_id,
            version = @version,
            system_prompt = @system_prompt,
            user_prompt_template = @user_prompt_template,
            output_schema = @output_schema,
            request_options = @request_options,
            temperature = @temperature,
            max_output_tokens = @max_output_tokens,
            max_tool_rounds = @max_tool_rounds,
            enabled = @enabled,
            updated_at = now()
         where id = @original_id
         """;

      await using var updateCommand = dataSource.CreateCommand(updateSql);
      AddPromptParameters(updateCommand, id, model);
      updateCommand.Parameters.AddWithValue(
         "original_id",
         Guid.Parse(originalId)
      );
      await updateCommand.ExecuteNonQueryAsync(cancellationToken);
   }

   private static void AddProviderParameters(
      NpgsqlCommand command,
      string id,
      AiProviderEditModel model
   )
   {
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("label", model.Label.Trim());
      command.Parameters.AddWithValue("kind", model.Kind.Trim());
      command.Parameters.AddWithValue(
         "base_address",
         BlankToDbNull(model.BaseAddress)
      );
      command.Parameters.AddWithValue(
         "model",
         BlankToDbNull(model.Model)
      );
      command.Parameters.AddWithValue(
         "api_key_source",
         BlankToDbNull(model.ApiKeySource)
      );
      AddJsonbParameter(
         command,
         "request_options",
         model.RequestOptionsJson
      );
      command.Parameters.AddWithValue("enabled", model.Enabled);
   }

   private static void AddJobParameters(
      NpgsqlCommand command,
      string id,
      AiJobEditModel model
   )
   {
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("label", model.Label.Trim());
      command.Parameters.AddWithValue(
         "description",
         BlankToDbNull(model.Description)
      );
      command.Parameters.AddWithValue("provider_id", model.ProviderId.Trim());
      command.Parameters.AddWithValue("output_mode", model.OutputMode.Trim());
      AddJsonbParameter(command, "tools_json", model.ToolsJson);
      command.Parameters.AddWithValue(
         "active_prompt_id",
         BlankToDbNullGuid(model.ActivePromptId)
      );
      command.Parameters.AddWithValue(
         "requires_web_search",
         model.RequiresWebSearch
      );
      command.Parameters.AddWithValue("enabled", model.Enabled);
   }

   private static void AddPromptParameters(
      NpgsqlCommand command,
      string id,
      AiPromptEditModel model
   )
   {
      command.Parameters.AddWithValue("id", Guid.Parse(id));
      command.Parameters.AddWithValue("job_id", model.JobId.Trim());
      command.Parameters.AddWithValue("version", model.Version);
      command.Parameters.AddWithValue(
         "system_prompt",
         model.SystemPrompt.Trim()
      );
      command.Parameters.AddWithValue(
         "user_prompt_template",
         model.UserPromptTemplate.Trim()
      );
      AddJsonbParameter(
         command,
         "output_schema",
         model.OutputSchemaJson
      );
      AddJsonbParameter(
         command,
         "request_options",
         model.RequestOptionsJson
      );
      command.Parameters.AddWithValue(
         "temperature",
         (object?)model.Temperature ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "max_output_tokens",
         (object?)model.MaxOutputTokens ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "max_tool_rounds",
         (object?)model.MaxToolRounds ?? DBNull.Value
      );
      command.Parameters.AddWithValue("enabled", model.Enabled);
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

   private static object BlankToDbNull(string? value)
   {
      return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
   }

   private static string? ReadNullableString(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static Guid? ReadNullableGuid(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
   }

   private static object BlankToDbNullGuid(string? value)
   {
      return string.IsNullOrWhiteSpace(value)
         ? DBNull.Value
         : Guid.Parse(value.Trim());
   }

   private static decimal? ReadNullableDecimal(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
   }

   private static int? ReadNullableInt32(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
   }
}
