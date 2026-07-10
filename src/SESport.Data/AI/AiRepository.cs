using Npgsql;
using NpgsqlTypes;
using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using System.Text;
using System.Text.Json;

namespace SESport.Data.AI;

public sealed class AiRepository(NpgsqlDataSource dataSource)
   : IAiJobDefinitionRepository, IAiJobRunRepository
{
   public async Task<IReadOnlyList<AiRunListItem>> GetRunsAsync(
      DateOnly? date,
      string? jobId,
      IReadOnlyCollection<string>? statusIds,
      CancellationToken cancellationToken
   )
   {
      var where = new List<string>();

      if(date is not null)
      {
         var dateStart = TimeZoneHelper.ToUtc(
            date.Value,
            TimeOnly.MinValue,
            SportDay.TimeZoneId
         );
         var dateEnd = TimeZoneHelper.ToUtc(
            date.Value.AddDays(1),
            TimeOnly.MinValue,
            SportDay.TimeZoneId
         );

         where.Add("r.started_at >= @start");
         where.Add("r.started_at < @end");
      }

      if(!string.IsNullOrWhiteSpace(jobId))
      {
         where.Add("r.job_id = @job_id");
      }

      var normalizedStatusIds = NormalizeStatusIds(statusIds);
      if(normalizedStatusIds.Count > 0)
      {
         where.Add("r.status_id = any(@status_ids)");
      }

      var sql = new StringBuilder()
         .AppendLine("with selected_runs as (")
         .AppendLine("select")
         .AppendLine("   r.id,")
         .AppendLine("   r.execution_environment,")
         .AppendLine("   j.label as job_label,")
         .AppendLine("   coalesce(")
         .AppendLine("      r.input_payload->>'event_name',")
         .AppendLine("      r.input_payload->>'title',")
         .AppendLine("      a.title")
         .AppendLine("   ) as event_name,")
         .AppendLine("   p.label as provider_label,")
         .AppendLine("   r.provider_model,")
         .AppendLine("   r.status_id,")
         .AppendLine("   r.tool_round_count,")
         .AppendLine("   r.started_at,")
         .AppendLine("   r.duration_seconds,")
         .AppendLine("   r.conversation_character_count,")
         .AppendLine("   r.tool_trace,")
         .AppendLine("   case r.status_id")
         .AppendLine(
            $"      when '{AiJobRunStatusIds.Running}' then 0"
         )
         .AppendLine(
            $"      when '{AiJobRunStatusIds.Pending}' then 1"
         )
         .AppendLine("      else 2")
         .AppendLine("   end as status_sort_order")
         .AppendLine("from ai_job_runs r")
         .AppendLine("join ai_jobs j on j.id = r.job_id")
         .AppendLine("join ai_providers p on p.id = r.provider_id")
         .AppendLine("left join activities a")
         .AppendLine("   on a.id::text = r.correlation_id")
         .AppendLine("      and r.job_id = @teaser_job_id");

      if(where.Count > 0)
      {
         sql.AppendLine("where " + string.Join(" and ", where));
      }

      sql.AppendLine("order by")
         .AppendLine("   status_sort_order,")
         .AppendLine("   r.started_at desc,")
         .AppendLine("   r.id desc")
         .AppendLine("limit 50")
         .AppendLine(")")
         .AppendLine("select")
         .AppendLine("   sr.id,")
         .AppendLine("   sr.execution_environment,")
         .AppendLine("   sr.job_label,")
         .AppendLine("   sr.event_name,")
         .AppendLine("   sr.provider_label,")
         .AppendLine("   sr.provider_model,")
         .AppendLine("   sr.status_id,")
         .AppendLine("   sr.tool_round_count,")
         .AppendLine("   sr.started_at,")
         .AppendLine("   sr.duration_seconds,")
         .AppendLine("   greatest(")
         .AppendLine("      sr.conversation_character_count,")
         .AppendLine("      coalesce(trace.max_payload_chars, 0)")
         .AppendLine("   ) as max_payload_chars")
         .AppendLine("from selected_runs sr")
         .AppendLine("left join lateral (")
         .AppendLine("   select max((entry.value->>'payload_chars')::int)")
         .AppendLine("      as max_payload_chars")
         .AppendLine("   from jsonb_array_elements(")
         .AppendLine("      coalesce(sr.tool_trace, '[]'::jsonb)")
         .AppendLine("   ) entry(value)")
         .AppendLine("   where entry.value ? 'payload_chars'")
         .AppendLine("      and entry.value->>'payload_chars' ~ '^[0-9]+$'")
         .AppendLine(") trace on true")
         .AppendLine("order by")
         .AppendLine("   sr.status_sort_order,")
         .AppendLine("   sr.started_at desc,")
         .AppendLine("   sr.id desc");

      await using var command = dataSource.CreateCommand(sql.ToString());
      command.Parameters.AddWithValue(
         "teaser_job_id",
         AiJobIds.GenerateActivityTeaser
      );

      if(date is not null)
      {
         command.Parameters.AddWithValue("start", GetDateStart(date.Value));
         command.Parameters.AddWithValue("end", GetDateEnd(date.Value));
      }

      if(!string.IsNullOrWhiteSpace(jobId))
      {
         command.Parameters.AddWithValue("job_id", jobId);
      }

      if(normalizedStatusIds.Count > 0)
      {
         command.Parameters.AddWithValue(
            "status_ids",
            normalizedStatusIds.ToArray()
         );
      }

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var runs = new List<AiRunListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         runs.Add(
            new AiRunListItem(
               reader.GetGuid(0),
               ReadNullableString(reader, 1),
               reader.GetString(2),
               ReadNullableString(reader, 3),
               reader.GetString(4),
               ReadNullableString(reader, 5),
               reader.GetString(6),
               reader.GetInt32(7),
               reader.GetInt32(10),
               reader.GetFieldValue<DateTimeOffset>(8),
               ReadNullableDecimal(reader, 9)
            )
         );
      }

      return runs;
   }

   private static DateTimeOffset GetDateStart(DateOnly date)
   {
      return TimeZoneHelper.ToUtc(
         date,
         TimeOnly.MinValue,
         SportDay.TimeZoneId
      );
   }

   private static DateTimeOffset GetDateEnd(DateOnly date)
   {
      return TimeZoneHelper.ToUtc(
         date.AddDays(1),
         TimeOnly.MinValue,
         SportDay.TimeZoneId
      );
   }

   private static IReadOnlyList<string> NormalizeStatusIds(
      IReadOnlyCollection<string>? statusIds
   )
   {
      var normalizedStatusIds = statusIds?
         .Where(statusId => !string.IsNullOrWhiteSpace(statusId))
         .Select(statusId => statusId.Trim())
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList()
         ?? [];

      return normalizedStatusIds.Count > 0
         ? normalizedStatusIds
         : AiJobRunStatusIds.DefaultRunListStatuses;
   }

   public async Task<IReadOnlyList<AiRunListItem>> GetRunsByIdsAsync(
      IReadOnlyCollection<Guid> ids,
      CancellationToken cancellationToken
   )
   {
      if(ids.Count == 0)
      {
         return [];
      }

      const string sql = """
         select
            r.id,
            r.execution_environment,
            j.label,
            coalesce(
               r.input_payload->>'event_name',
               r.input_payload->>'title',
               a.title
            ),
            p.label,
            r.provider_model,
            r.status_id,
            r.tool_round_count,
            r.started_at,
            r.duration_seconds,
            greatest(
               r.conversation_character_count,
               coalesce(trace.max_payload_chars, 0)
            ) as max_payload_chars
         from ai_job_runs r
         join ai_jobs j on j.id = r.job_id
         join ai_providers p on p.id = r.provider_id
         left join activities a
            on a.id::text = r.correlation_id
               and r.job_id = @teaser_job_id
         left join lateral (
            select max((entry.value->>'payload_chars')::int)
               as max_payload_chars
            from jsonb_array_elements(
               coalesce(r.tool_trace, '[]'::jsonb)
            ) entry(value)
            where entry.value ? 'payload_chars'
               and entry.value->>'payload_chars' ~ '^[0-9]+$'
         ) trace on true
         where r.id = any(@ids)
            and r.status_id <> 'archived'
         order by r.started_at desc
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("ids", ids.ToArray());
      command.Parameters.AddWithValue(
         "teaser_job_id",
         AiJobIds.GenerateActivityTeaser
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var runs = new List<AiRunListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         runs.Add(
            new AiRunListItem(
               reader.GetGuid(0),
               ReadNullableString(reader, 1),
               reader.GetString(2),
               ReadNullableString(reader, 3),
               reader.GetString(4),
               ReadNullableString(reader, 5),
               reader.GetString(6),
               reader.GetInt32(7),
               reader.GetInt32(10),
               reader.GetFieldValue<DateTimeOffset>(8),
               ReadNullableDecimal(reader, 9)
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
            coalesce(r.prompt_version, pr.version, 0),
            coalesce(r.prompt_system_prompt, pr.system_prompt, ''),
            coalesce(
               r.prompt_user_prompt_template,
               pr.user_prompt_template,
               ''
            ),
            r.provider_id,
            p.label,
            r.provider_model,
            r.status_id,
            r.correlation_id,
            r.input_payload::text,
            r.rendered_prompt,
            r.raw_request::text,
            r.raw_response::text,
            r.tool_trace::text,
            r.tool_round_count,
            r.conversation_character_count,
            r.output_text,
            r.error_message,
            r.started_at,
            r.completed_at,
            r.duration_seconds,
            r.input_tokens,
            r.output_tokens,
            r.reasoning_tokens,
            r.execution_environment
         from ai_job_runs r
         join ai_jobs j on j.id = r.job_id
         join ai_providers p on p.id = r.provider_id
         left join ai_job_prompts pr on pr.id = r.prompt_id
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
         reader.GetString(8),
         ReadNullableString(reader, 9),
         reader.GetString(10),
         ReadNullableString(reader, 11),
         reader.GetString(12),
         reader.GetString(13),
         ReadNullableString(reader, 14),
         ReadNullableString(reader, 15),
         ReadNullableString(reader, 16),
         reader.GetInt32(17),
         reader.GetInt32(18),
         ReadNullableString(reader, 19),
         ReadNullableString(reader, 20),
         reader.GetFieldValue<DateTimeOffset>(21),
         ReadNullableDateTimeOffset(reader, 22),
         ReadNullableDecimal(reader, 23),
         ReadNullableInt32(reader, 24),
         ReadNullableInt32(reader, 25),
         ReadNullableInt32(reader, 26),
         ReadNullableString(reader, 27)
      );
   }

   public async Task<IReadOnlyList<CompletedActivityTeaserRun>>
      GetCompletedActivityTeaserRunsWithEmptyActivityTeasersAsync(
         int maxRuns,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            r.id,
            a.id,
            r.output_text
         from ai_job_runs r
         join activities a on a.id::text = r.correlation_id
         where r.job_id = @job_id
            and r.status_id = @status_id
            and coalesce(a.teaser, '') = ''
            and coalesce(r.output_text, '') <> ''
         order by r.completed_at desc, r.id desc
         limit @limit
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "job_id",
         AiJobIds.GenerateActivityTeaser
      );
      command.Parameters.AddWithValue(
         "status_id",
         AiJobRunStatusIds.Completed
      );
      command.Parameters.AddWithValue("limit", maxRuns);

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var runs = new List<CompletedActivityTeaserRun>();

      while(await reader.ReadAsync(cancellationToken))
      {
         runs.Add(
            new CompletedActivityTeaserRun(
               reader.GetGuid(0),
               reader.GetGuid(1),
               reader.GetString(2)
            )
         );
      }

      return runs;
   }

   public async Task<IReadOnlyDictionary<Guid, BroadcastParticipationCheck>>
      GetParticipationChecksAsync(
         IReadOnlyCollection<Guid> broadcastIds,
         CancellationToken cancellationToken
      )
   {
      var history = await GetParticipationCheckHistoryAsync(
         broadcastIds,
         cancellationToken
      );

      return history
         .Where(pair => pair.Value.Count > 0)
         .ToDictionary(
            pair => pair.Key,
            pair => pair.Value[0]
         );
   }

   public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<
      BroadcastParticipationCheck>>>
      GetParticipationCheckHistoryAsync(
         IReadOnlyCollection<Guid> broadcastIds,
         CancellationToken cancellationToken
      )
   {
      if(broadcastIds.Count == 0)
      {
         return new Dictionary<Guid, IReadOnlyList<
            BroadcastParticipationCheck>>();
      }

      const string sql = """
         select
            r.correlation_id,
            r.id,
            r.status_id,
            coalesce(r.tool_round_count, 0),
            r.output_text,
            r.raw_response::text,
            r.error_message
         from ai_job_runs r
         where r.job_id = @job_id
            and r.correlation_id = any(@correlation_ids)
            and r.status_id <> 'archived'
         order by r.correlation_id, r.started_at desc
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "job_id",
         AiJobIds.DecidePrimaryCountryParticipation
      );
      command.Parameters.AddWithValue(
         "correlation_ids",
         broadcastIds.Select(id => id.ToString()).ToArray()
      );

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var checks = new Dictionary<Guid, List<BroadcastParticipationCheck>>();

      while(await reader.ReadAsync(cancellationToken))
      {
         var correlationId = ReadNullableString(reader, 0);

         if(!Guid.TryParse(correlationId, out var broadcastId))
         {
            continue;
         }

         var runId = reader.GetGuid(1);
         var statusId = reader.GetString(2);
         var toolRoundCount = reader.GetInt32(3);
         var outputText = ReadNullableString(reader, 4);
         var rawResponseText = ReadNullableString(reader, 5);
         var errorMessage = ReadNullableString(reader, 6);

         if(!checks.TryGetValue(broadcastId, out var history))
         {
            history = [];
            checks[broadcastId] = history;
         }

         history.Add(
            ParseParticipationCheck(
               runId,
               statusId,
               toolRoundCount,
               outputText,
               rawResponseText,
               errorMessage
            )
         );
      }

      return checks.ToDictionary(
         pair => pair.Key,
         pair => (IReadOnlyList<BroadcastParticipationCheck>)pair.Value
      );
   }

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
            output_mode,
            tools_json::text,
            requires_web_search,
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
         ReadNullableString(reader, 2),
         reader.GetString(3),
         reader.GetString(4),
         ReadNullableString(reader, 5),
         reader.GetBoolean(6),
         reader.GetBoolean(8),
         ReadNullableGuid(reader, 7)
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
            enabled
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
         ReadNullableString(promptReader, 5),
         ReadNullableString(promptReader, 6) ?? "{}",
         ReadNullableDecimal(promptReader, 7),
         ReadNullableInt32(promptReader, 8),
         ReadNullableInt32(promptReader, 9),
         promptReader.GetBoolean(10)
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
            enabled
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
         ReadNullableString(reader, 5),
         ReadNullableString(reader, 6) ?? "{}",
         ReadNullableDecimal(reader, 7),
         ReadNullableInt32(reader, 8),
         ReadNullableInt32(reader, 9),
         reader.GetBoolean(10)
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
      ReadNullableString(reader, 6) ?? "{}",
      ReadNullableDecimal(reader, 7),
      ReadNullableInt32(reader, 8),
      ReadNullableInt32(reader, 9),
      reader.GetBoolean(10)
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
            id, job_id, prompt_id, prompt_version, prompt_system_prompt,
            prompt_user_prompt_template, provider_id, status_id,
            correlation_id, provider_model, input_payload, rendered_prompt,
            raw_request, raw_response, tool_trace, output_text, error_message,
            started_at, completed_at, duration_seconds, input_tokens,
            output_tokens, reasoning_tokens, tool_round_count,
            conversation_character_count, execution_environment
         )
         values (
            @id, @job_id, @prompt_id, @prompt_version, @prompt_system_prompt,
            @prompt_user_prompt_template, @provider_id, @status_id,
            @correlation_id, @provider_model, @input_payload,
            @rendered_prompt, @raw_request, @raw_response, @tool_trace,
            @output_text, @error_message, @started_at, @completed_at,
            @duration_seconds, @input_tokens, @output_tokens,
            @reasoning_tokens, @tool_round_count,
            @conversation_character_count, @execution_environment
         )
         """;

      await using var command = dataSource.CreateCommand(sql);
      AddRunParameters(command, run);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<bool> TryClaimRunAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update ai_job_runs
         set
            status_id = 'running',
            started_at = now()
         where id = @id
            and status_id = 'pending'
            and execution_environment = @execution_environment
         returning id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue(
         "execution_environment",
         ExecutionEnvironment.Current
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      return await reader.ReadAsync(cancellationToken);
   }

   public async Task<Guid?> ClaimNextRunAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         with next_run as (
            select id
            from ai_job_runs
            where status_id in ('pending', 'running')
               and execution_environment = @execution_environment
            order by
               status_id desc,
               started_at asc,
               created_at asc,
               id asc
            for update skip locked
            limit 1
         )
         update ai_job_runs r
         set
            status_id = 'running',
            started_at = now()
         from next_run
         where r.id = next_run.id
         returning r.id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "execution_environment",
         ExecutionEnvironment.Current
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return reader.GetGuid(0);
   }

   public async Task DeleteRunAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from ai_job_runs
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<bool> ArchiveRunAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update ai_job_runs
         set status_id = 'archived'
         where id = @id
         returning id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      return await reader.ReadAsync(cancellationToken);
   }

   public async Task FailRunAsync(
      Guid id,
      string errorMessage,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update ai_job_runs
         set
            status_id = 'failed',
            error_message = @error_message,
            completed_at = now(),
            duration_seconds = extract(
               epoch from now() - started_at
            )
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("error_message", errorMessage);
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
            prompt_version = @prompt_version,
            prompt_system_prompt = @prompt_system_prompt,
            prompt_user_prompt_template = @prompt_user_prompt_template,
            raw_request = @raw_request,
            raw_response = @raw_response,
            tool_trace = @tool_trace,
            tool_round_count = @tool_round_count,
            conversation_character_count = @conversation_character_count,
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

   public async Task UpdateRunExecutionEnvironmentAsync(
      Guid runId,
      string? executionEnvironment,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update ai_job_runs
         set
            execution_environment = @execution_environment
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", runId);
      command.Parameters.AddWithValue(
         "execution_environment",
         (object?)executionEnvironment ?? DBNull.Value
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<IReadOnlyList<string>> GetExecutionEnvironmentOptionsAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select distinct execution_environment
         from ai_job_runs
         where execution_environment is not null
            and btrim(execution_environment) <> ''
         order by execution_environment
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      var executionEnvironments = new List<string>();

      while(await reader.ReadAsync(cancellationToken))
      {
         executionEnvironments.Add(reader.GetString(0));
      }

      return executionEnvironments;
   }

   public async Task UpdateToolTraceAsync(
      Guid runId,
      string? toolTraceJson,
      int toolRoundCount,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update ai_job_runs
         set
            tool_trace = @tool_trace,
            tool_round_count = @tool_round_count
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", runId);
      AddJsonbParameter(command, "tool_trace", toolTraceJson);
      command.Parameters.AddWithValue("tool_round_count", toolRoundCount);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<int> FailStaleRunningRunsAsync(
      TimeSpan maxAge,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update ai_job_runs
         set
            status_id = 'failed',
            error_message = @error_message,
            completed_at = now(),
            duration_seconds = extract(
               epoch from now() - started_at
            )
         where status_id = 'running'
            and started_at < @cutoff
         returning id
         """;

      var cutoff = DateTimeOffset.UtcNow.Subtract(maxAge);

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("cutoff", cutoff);
      command.Parameters.AddWithValue(
         "error_message",
         "Run timed out after 1 hour."
      );

      var updatedCount = 0;

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      while(await reader.ReadAsync(cancellationToken))
      {
         updatedCount++;
      }

      return updatedCount;
   }

   private static void AddRunParameters(
      NpgsqlCommand command,
      AiJobRun run
   )
   {
      command.Parameters.AddWithValue("id", run.Id);
      command.Parameters.AddWithValue("job_id", run.JobId);
      command.Parameters.AddWithValue("prompt_id", run.PromptId);
      command.Parameters.AddWithValue("prompt_version", run.PromptVersion);
      command.Parameters.AddWithValue(
         "prompt_system_prompt",
         run.PromptSystemPrompt
      );
      command.Parameters.AddWithValue(
         "prompt_user_prompt_template",
         run.PromptUserPromptTemplate
      );
      command.Parameters.AddWithValue("provider_id", run.ProviderId);
      AddNullableStringParameter(command, "provider_model", run.ProviderModel);
      command.Parameters.AddWithValue("status_id", ToStatusId(run.Status));
      command.Parameters.AddWithValue(
         "correlation_id",
         (object?)run.CorrelationId ?? DBNull.Value
      );
      AddJsonbParameter(command, "input_payload", run.InputPayloadJson);
      command.Parameters.AddWithValue("rendered_prompt", run.RenderedPrompt);
      AddJsonbParameter(command, "raw_request", run.RawRequestJson);
      AddJsonbParameter(command, "raw_response", run.RawResponseJson);
      AddJsonbParameter(command, "tool_trace", run.ToolTraceJson);
      command.Parameters.AddWithValue("tool_round_count", run.ToolRoundCount);
      command.Parameters.AddWithValue(
         "conversation_character_count",
         run.ConversationCharacterCount
      );
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
      command.Parameters.AddWithValue(
         "execution_environment",
         run.ExecutionEnvironment
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
      command.Parameters.AddWithValue("prompt_version", run.PromptVersion);
      command.Parameters.AddWithValue(
         "prompt_system_prompt",
         run.PromptSystemPrompt
      );
      command.Parameters.AddWithValue(
         "prompt_user_prompt_template",
         run.PromptUserPromptTemplate
      );
      AddJsonbParameter(command, "raw_request", run.RawRequestJson);
      AddJsonbParameter(command, "raw_response", run.RawResponseJson);
      AddJsonbParameter(command, "tool_trace", run.ToolTraceJson);
      command.Parameters.AddWithValue("tool_round_count", run.ToolRoundCount);
      command.Parameters.AddWithValue(
         "conversation_character_count",
         run.ConversationCharacterCount
      );
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

   private static void AddNullableStringParameter(
      NpgsqlCommand command,
      string name,
      string? value
   )
   {
      command.Parameters.AddWithValue(
         name,
         (object?)value ?? DBNull.Value
      );
   }

   private static BroadcastParticipationCheck ParseParticipationCheck(
      Guid runId,
      string statusId,
      int toolRoundCount,
      string? outputText,
      string? rawResponseText,
      string? errorMessage
   )
   {
      var sourceUrls = ParticipationSourceUrlExtractor.ExtractFromOutput(
         outputText
      );
      var resolvedSourceUrls = sourceUrls.Count > 0
         ? sourceUrls
         : ParticipationSourceUrlExtractor.Extract(rawResponseText);

      if(string.IsNullOrWhiteSpace(outputText))
      {
         return new BroadcastParticipationCheck(
            runId,
            statusId,
            toolRoundCount,
            null,
            [],
            resolvedSourceUrls,
            errorMessage
         );
      }

      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(root.ValueKind != JsonValueKind.Object)
         {
            throw new JsonException("Expected a JSON object.");
         }

         if(!TryGetStringProperty(
            root,
            "Participation",
            PrimaryCountry.LanguageName + "Participation",
            out var participation
         ))
         {
            throw new JsonException(
               "Missing Participation property."
            );
         }

         var participants = new List<string>();

         if(TryGetArrayProperty(
            root,
            "Participants",
            PrimaryCountry.LanguageName + "Participants",
            out var participantsElement
         ))
         {
            foreach(var participant in participantsElement.EnumerateArray())
            {
               if(participant.ValueKind != JsonValueKind.String)
               {
                  continue;
               }

               var name = participant.GetString();

               if(!string.IsNullOrWhiteSpace(name))
               {
                  participants.Add(name);
               }
            }
         }

         return new BroadcastParticipationCheck(
            runId,
            statusId,
            toolRoundCount,
            participation,
            participants,
            resolvedSourceUrls,
            errorMessage
         );
      }
      catch(JsonException)
      {
         return new BroadcastParticipationCheck(
            runId,
            statusId,
            toolRoundCount,
            null,
            [],
            resolvedSourceUrls,
            errorMessage ?? "The model returned invalid JSON."
         );
      }
   }

   private static string ToStatusId(AiJobRunStatus status)
   {
      return status switch
      {
         AiJobRunStatus.Pending => "pending",
         AiJobRunStatus.Running => "running",
         AiJobRunStatus.Completed => "completed",
         AiJobRunStatus.Failed => "failed",
         AiJobRunStatus.Archived => "archived",
         _ => "pending"
      };
   }

   private static bool TryGetStringProperty(
      JsonElement root,
      string propertyName,
      string legacyPropertyName,
      out string? value
   )
   {
      if(root.TryGetProperty(propertyName, out var property) &&
         property.ValueKind == JsonValueKind.String)
      {
         value = property.GetString();
         return true;
      }

      if(root.TryGetProperty(legacyPropertyName, out var legacyProperty) &&
         legacyProperty.ValueKind == JsonValueKind.String)
      {
         value = legacyProperty.GetString();
         return true;
      }

      value = null;
      return false;
   }

   private static bool TryGetArrayProperty(
      JsonElement root,
      string propertyName,
      string legacyPropertyName,
      out JsonElement value
   )
   {
      if(root.TryGetProperty(propertyName, out value) &&
         value.ValueKind == JsonValueKind.Array)
      {
         return true;
      }

      if(root.TryGetProperty(legacyPropertyName, out value) &&
         value.ValueKind == JsonValueKind.Array)
      {
         return true;
      }

      value = default;
      return false;
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

public sealed record CompletedActivityTeaserRun(
   Guid RunId,
   Guid ActivityId,
   string OutputText
);
