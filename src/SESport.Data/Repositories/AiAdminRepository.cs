using Npgsql;
using SESport.Core.AI;
using System.Text.Json.Nodes;

namespace SESport.Data.Repositories;

public sealed class AiAdminRepository(NpgsqlDataSource dataSource)
{
   public async Task<IReadOnlyList<AiAutomationRuleListItem>>
      GetAutomationRulesAsync(CancellationToken cancellationToken)
   {
      const string sql = """
         select r.id, r.event_id, r.job_id, j.label, r.enabled
         from ai_automation_rules r
         join ai_jobs j on j.id = r.job_id
         order by r.event_id, j.label
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var rules = new List<AiAutomationRuleListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         rules.Add(
            new AiAutomationRuleListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetBoolean(4)
            )
         );
      }

      return rules;
   }

   public async Task<AiAutomationRuleEditModel?> GetAutomationRuleAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select id, event_id, job_id, enabled
         from ai_automation_rules
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      return await reader.ReadAsync(cancellationToken)
         ? new AiAutomationRuleEditModel
         {
            Id = reader.GetGuid(0),
            EventId = reader.GetString(1),
            JobId = reader.GetString(2),
            Enabled = reader.GetBoolean(3)
         }
         : null;
   }

   public async Task SaveAutomationRuleAsync(
      AiAutomationRuleEditModel model,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into ai_automation_rules (
            id, event_id, job_id, enabled
         )
         values (@id, @event_id, @job_id, @enabled)
         on conflict (id) do update
         set
            event_id = excluded.event_id,
            job_id = excluded.job_id,
            enabled = excluded.enabled,
            updated_at = now()
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", model.Id ?? Guid.NewGuid());
      command.Parameters.AddWithValue("event_id", model.EventId.Trim());
      command.Parameters.AddWithValue("job_id", model.JobId.Trim());
      command.Parameters.AddWithValue("enabled", model.Enabled);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task DeleteAutomationRuleAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from ai_automation_rules
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

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

      while(await reader.ReadAsync(cancellationToken))
      {
         items.Add(
            new AiProviderListItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2),
               PostgresHelpers.ReadNullableString(reader, 3),
               PostgresHelpers.ReadNullableString(reader, 4),
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
         BaseAddress = PostgresHelpers.ReadNullableString(reader, 3),
         Model = PostgresHelpers.ReadNullableString(reader, 4),
         ApiKeySource = PostgresHelpers.ReadNullableString(reader, 5),
         RequestOptionsJson =
            PostgresHelpers.ReadNullableString(reader, 6) ?? "{}",
         CodexProfile = ReadCodexProfile(
            PostgresHelpers.ReadNullableString(reader, 6)
         ),
         CodexSystemInstruction = ReadCodexSystemInstruction(
            PostgresHelpers.ReadNullableString(reader, 6)
         ),
         Enabled = reader.GetBoolean(7)
      };
   }

   public async Task DeleteProviderAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from ai_providers
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await command.ExecuteNonQueryAsync(cancellationToken);
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
            label = @label,
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
            provider.kind,
            j.queue_priority,
            j.output_mode,
            p.version,
            j.enabled
         from ai_jobs j
         join ai_providers provider on provider.id = j.provider_id
         left join ai_job_prompts p on p.id = j.active_prompt_id
         order by j.label
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var items = new List<AiJobListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         items.Add(
            new AiJobListItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetInt32(4),
               reader.GetString(5),
               PostgresHelpers.ReadNullableInt32(reader, 6),
               reader.GetBoolean(7)
            )
         );
      }

      return items;
   }

   public async Task DeleteJobAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from ai_jobs
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await command.ExecuteNonQueryAsync(cancellationToken);
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
            model,
            queue_priority,
            output_mode,
            tools_json::text,
            conditional_tools_json::text,
            tool_call_max_tokens,
            active_prompt_id,
            requires_web_search,
            include_social_media,
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
         Description = PostgresHelpers.ReadNullableString(reader, 2),
         ProviderId = reader.GetString(3),
         Model = PostgresHelpers.ReadNullableString(reader, 4),
         QueuePriority = reader.GetInt32(5),
         OutputMode = reader.GetString(6),
         ToolsJson = PostgresHelpers.ReadNullableString(reader, 7),
         ConditionalToolsJson = PostgresHelpers.ReadNullableString(reader, 8),
         ToolCallMaxTokens = PostgresHelpers.ReadNullableInt32(reader, 9),
         ActivePromptId =
            PostgresHelpers.ReadNullableGuid(reader, 10)?.ToString(),
         RequiresWebSearch = reader.GetBoolean(11),
         IncludeSocialMedia = reader.GetBoolean(12),
         Enabled = reader.GetBoolean(13)
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
            model,
            queue_priority,
            output_mode,
            tools_json,
            conditional_tools_json,
            tool_call_max_tokens,
            active_prompt_id,
            requires_web_search,
            include_social_media,
            enabled
         )
         values (
               @id,
               @label,
            @description,
            @provider_id,
            @model,
            @queue_priority,
            @output_mode,
            @tools_json,
            @conditional_tools_json,
            @tool_call_max_tokens,
            @active_prompt_id,
            @requires_web_search,
            @include_social_media,
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
            label = @label,
            description = @description,
            provider_id = @provider_id,
            model = @model,
            queue_priority = @queue_priority,
            output_mode = @output_mode,
            tools_json = @tools_json,
            conditional_tools_json = @conditional_tools_json,
            tool_call_max_tokens = @tool_call_max_tokens,
            active_prompt_id = @active_prompt_id,
            requires_web_search = @requires_web_search,
            include_social_media = @include_social_media,
            enabled = @enabled,
            updated_at = now()
         where id = @original_id
         """;

      await using var updateCommand = dataSource.CreateCommand(updateSql);
      AddJobParameters(updateCommand, id, model);
      updateCommand.Parameters.AddWithValue("original_id", originalId);
      await updateCommand.ExecuteNonQueryAsync(cancellationToken);
   }

   public Task<IReadOnlyList<AiPromptListItem>> GetPromptsAsync(
      CancellationToken cancellationToken
   )
   {
      return GetPromptListAsync(null, cancellationToken);
   }

   public async Task DeletePromptAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from ai_job_prompts
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", Guid.Parse(id));
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public Task<IReadOnlyList<AiPromptListItem>> GetJobPromptsAsync(
      string jobId,
      CancellationToken cancellationToken
   )
   {
      return GetPromptListAsync(jobId, cancellationToken);
   }

   private async Task<IReadOnlyList<AiPromptListItem>> GetPromptListAsync(
      string? jobId,
      CancellationToken cancellationToken
   )
   {
      var sql = """
         select
            p.id::text,
            p.job_id,
            j.label,
            p.version,
            p.system_prompt,
            p.user_prompt_template,
            p.temperature,
            p.max_output_tokens,
            p.max_tool_rounds,
            p.min_tool_rounds,
            p.enabled,
            exists (
               select 1
               from ai_jobs active_job
               where active_job.active_prompt_id = p.id
            ) as is_in_use
         from ai_job_prompts p
         join ai_jobs j on j.id = p.job_id
         """;

      if(jobId is null)
      {
         sql += "\norder by p.job_id, p.version desc";
      }
      else
      {
         sql += "\nwhere p.job_id = @job_id order by p.version desc";
      }

      await using var command = dataSource.CreateCommand(sql);
      if(jobId is not null)
      {
         command.Parameters.AddWithValue("job_id", jobId);
      }

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var items = new List<AiPromptListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         items.Add(ReadPromptListItem(reader));
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
            min_tool_rounds,
            enabled,
            codex_reasoning_effort
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
         OutputSchemaJson = PostgresHelpers.ReadNullableString(reader, 5),
         RequestOptionsJson =
            PostgresHelpers.ReadNullableString(reader, 6) ?? "{}",
         Temperature = PostgresHelpers.ReadNullableDecimal(reader, 7),
         MaxOutputTokens = PostgresHelpers.ReadNullableInt32(reader, 8),
         MaxToolRounds = PostgresHelpers.ReadNullableInt32(reader, 9),
         MinToolRounds = PostgresHelpers.ReadNullableInt32(reader, 10),
         Enabled = reader.GetBoolean(11),
         CodexReasoningEffort = PostgresHelpers.ReadNullableString(reader, 12)
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
               max_output_tokens, max_tool_rounds, min_tool_rounds,
               codex_reasoning_effort, enabled
            )
            values (
               @id, @job_id, @version, @system_prompt,
               @user_prompt_template, @output_schema, @request_options,
               @temperature, @max_output_tokens, @max_tool_rounds,
               @min_tool_rounds, @codex_reasoning_effort, @enabled
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
            system_prompt = @system_prompt,
            user_prompt_template = @user_prompt_template,
            output_schema = @output_schema,
            request_options = @request_options,
            temperature = @temperature,
            max_output_tokens = @max_output_tokens,
            max_tool_rounds = @max_tool_rounds,
            min_tool_rounds = @min_tool_rounds,
            codex_reasoning_effort = @codex_reasoning_effort,
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
         PostgresHelpers.BlankToDbNull(model.BaseAddress)
      );
      command.Parameters.AddWithValue(
         "model",
         PostgresHelpers.BlankToDbNull(model.Model)
      );
      command.Parameters.AddWithValue(
         "api_key_source",
         PostgresHelpers.BlankToDbNull(model.ApiKeySource)
      );
      PostgresHelpers.AddJsonbParameter(
         command,
         "request_options",
         BuildProviderRequestOptions(model)
      );
      command.Parameters.AddWithValue("enabled", model.Enabled);
   }

   private static string BuildProviderRequestOptions(AiProviderEditModel model)
   {
      var requestOptions = string.IsNullOrWhiteSpace(model.RequestOptionsJson)
         ? "{}"
         : model.RequestOptionsJson;
      var node = JsonNode.Parse(requestOptions) as JsonObject
         ?? new JsonObject();

      SetRequestOption(node, "codex_profile", model.CodexProfile);
      SetRequestOption(
         node,
         "codex_system_instruction",
         model.CodexSystemInstruction
      );

      return node.ToJsonString();
   }

   private static void SetRequestOption(
      JsonObject node,
      string key,
      string? value
   )
   {
      var trimmed = value?.Trim();
      if(string.IsNullOrWhiteSpace(trimmed))
      {
         node.Remove(key);
         return;
      }

      node[key] = trimmed;
   }

   private static string? ReadCodexSystemInstruction(string? requestOptionsJson)
   {
      if(string.IsNullOrWhiteSpace(requestOptionsJson))
      {
         return null;
      }

      var node = JsonNode.Parse(requestOptionsJson) as JsonObject;
      if(node is null)
      {
         return null;
      }

      if(node.TryGetPropertyValue("codex_system_instruction", out var value))
      {
         return value is JsonValue stringValue
            ? stringValue.ToString()
            : value?.ToJsonString();
      }

      return null;
   }

   private static string? ReadCodexProfile(string? requestOptionsJson)
   {
      if(string.IsNullOrWhiteSpace(requestOptionsJson))
      {
         return null;
      }

      var node = JsonNode.Parse(requestOptionsJson) as JsonObject;
      if(node is null)
      {
         return null;
      }

      if(node.TryGetPropertyValue("codex_profile", out var profile))
      {
         return profile is JsonValue value
            ? value.ToString()
            : profile?.ToJsonString();
      }

      return null;
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
         PostgresHelpers.BlankToDbNull(model.Description)
      );
      command.Parameters.AddWithValue("provider_id", model.ProviderId.Trim());
      command.Parameters.AddWithValue(
         "model",
         PostgresHelpers.BlankToDbNull(model.Model)
      );
      command.Parameters.AddWithValue("queue_priority", model.QueuePriority);
      command.Parameters.AddWithValue("output_mode", model.OutputMode.Trim());
      PostgresHelpers.AddJsonbParameter(
         command,
         "tools_json",
         model.ToolsJson
      );
      PostgresHelpers.AddJsonbParameter(
         command,
         "conditional_tools_json",
         model.ConditionalToolsJson
      );
      command.Parameters.AddWithValue(
         "tool_call_max_tokens",
         (object?)model.ToolCallMaxTokens ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "active_prompt_id",
         PostgresHelpers.BlankToDbNullGuid(model.ActivePromptId)
      );
      command.Parameters.AddWithValue(
         "requires_web_search",
         model.RequiresWebSearch
      );
      command.Parameters.AddWithValue(
         "include_social_media",
         model.IncludeSocialMedia
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
      PostgresHelpers.AddJsonbParameter(
         command,
         "output_schema",
         model.OutputSchemaJson
      );
      PostgresHelpers.AddJsonbParameter(
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
      command.Parameters.AddWithValue(
         "min_tool_rounds",
         (object?)model.MinToolRounds ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "codex_reasoning_effort",
         PostgresHelpers.BlankToDbNull(model.CodexReasoningEffort)
      );
      command.Parameters.AddWithValue("enabled", model.Enabled);
   }

   private static AiPromptListItem ReadPromptListItem(
      NpgsqlDataReader reader
   )
   {
      return new AiPromptListItem(
         reader.GetString(0),
         reader.GetString(1),
         reader.GetString(2),
         reader.GetInt32(3),
         reader.GetString(4),
         reader.GetString(5),
         PostgresHelpers.ReadNullableDecimal(reader, 6),
         PostgresHelpers.ReadNullableInt32(reader, 7),
         PostgresHelpers.ReadNullableInt32(reader, 8),
         PostgresHelpers.ReadNullableInt32(reader, 9),
         reader.GetBoolean(10),
         reader.GetBoolean(11)
      );
   }

}
