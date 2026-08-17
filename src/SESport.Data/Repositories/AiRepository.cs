using Npgsql;
using NpgsqlTypes;
using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Configuration;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using System.Text;

namespace SESport.Data.Repositories;

public sealed class AiRepository(NpgsqlDataSource dataSource)
   : IAiJobDefinitionRepository, IAiJobRunRepository
{
   private static readonly string[] ActivityJobIds =
   [
      AiJobIds.GenerateActivityTeaser,
      AiJobIds.FindParticipantsStart,
      AiJobIds.FindParticipantsResult
   ];

   private const string ActivityGroupJobId =
      AiJobIds.FindActivityGroupFacts;

   private const string BroadcastJobId =
      AiJobIds.DecidePrimaryCountryParticipation;

   private static readonly string[] PersonJobIds =
   [
      AiJobIds.FindPersonData,
      AiJobIds.TranslateText
   ];

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
         .AppendLine("   r.job_id,")
         .AppendLine("   r.execution_environment,")
         .AppendLine(
            "   coalesce(r.job_label, j.label, '') as job_label,"
         )
         .AppendLine("   coalesce(")
         .AppendLine("      r.input_payload->>'event_name',")
         .AppendLine("      r.input_payload->>'title',")
         .AppendLine("      a.title,")
         .AppendLine("      ag.title,")
         .AppendLine("      b.title,")
         .AppendLine("      person.canonical_name")
         .AppendLine("   ) as event_name,")
         .AppendLine("   coalesce(")
         .AppendLine("      a.activity_date,")
         .AppendLine("      ag.start_date,")
         .AppendLine("      (b.starts_at at time zone @time_zone)::date")
         .AppendLine("   ) as event_date,")
         .AppendLine(
            "   coalesce(r.provider_label, p.label, '') as provider_label,"
         )
         .AppendLine("   r.provider_model,")
         .AppendLine("   r.status_id,")
         .AppendLine("   j.queue_priority,")
         .AppendLine("   r.tool_round_count,")
         .AppendLine("   r.started_at,")
         .AppendLine("   r.created_at,")
         .AppendLine("   r.duration_seconds,")
         .AppendLine("   r.conversation_character_count,")
         .AppendLine("   r.tool_trace,")
         .AppendLine("   r.output_text,")
         .AppendLine(
            "   coalesce(r.prompt_output_schema_json, " +
            "pr.output_schema)::text as output_schema,"
         )
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
         .AppendLine("left join ai_jobs j on j.id = r.job_id")
         .AppendLine("left join ai_providers p on p.id = r.provider_id")
         .AppendLine("left join ai_job_prompts pr on pr.id = r.prompt_id")
         .AppendLine("left join activities a")
         .AppendLine("   on a.id::text = r.correlation_id")
         .AppendLine("      and r.job_id = any(@activity_job_ids)")
         .AppendLine("left join activity_groups ag")
         .AppendLine("   on ag.id::text = r.correlation_id")
         .AppendLine("      and r.job_id = @activity_group_job_id")
         .AppendLine("left join broadcasts b")
         .AppendLine("   on b.id::text = r.correlation_id")
         .AppendLine("      and r.job_id = @broadcast_job_id")
         .AppendLine("left join entities person")
         .AppendLine("   on person.id::text = r.correlation_id")
         .AppendLine("      and r.job_id = any(@person_job_ids)");

      if(where.Count > 0)
      {
         sql.AppendLine("where " + string.Join(" and ", where));
      }

      sql.AppendLine(")")
         .AppendLine("select")
         .AppendLine("   sr.id,")
         .AppendLine("   sr.job_id,")
         .AppendLine("   sr.execution_environment,")
         .AppendLine("   sr.job_label,")
         .AppendLine("   sr.event_name,")
         .AppendLine("   sr.event_date,")
         .AppendLine("   sr.provider_label,")
         .AppendLine("   sr.provider_model,")
         .AppendLine("   sr.status_id,")
         .AppendLine("   sr.tool_round_count,")
         .AppendLine("   sr.started_at,")
         .AppendLine("   sr.duration_seconds,")
         .AppendLine("   greatest(")
         .AppendLine("      sr.conversation_character_count,")
         .AppendLine("      coalesce(trace.max_payload_chars, 0)")
         .AppendLine("   ) as max_payload_chars,")
         .AppendLine("   sr.output_text,")
         .AppendLine("   sr.output_schema")
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
         .AppendLine("   case when sr.status_sort_order < 2")
         .AppendLine("      then sr.queue_priority else 0 end desc,")
         .AppendLine("   case when sr.status_sort_order < 2")
         .AppendLine("      then sr.started_at end asc nulls last,")
         .AppendLine("   case when sr.status_sort_order < 2")
         .AppendLine("      then sr.created_at end asc nulls last,")
         .AppendLine("   case when sr.status_sort_order < 2")
         .AppendLine("      then sr.id end asc nulls last,")
         .AppendLine("   sr.started_at desc,")
         .AppendLine("   sr.id desc")
         .AppendLine("limit 500");

      await using var command = dataSource.CreateCommand(sql.ToString());
      command.Parameters.AddWithValue("activity_job_ids", ActivityJobIds);
      command.Parameters.AddWithValue(
         "activity_group_job_id",
         ActivityGroupJobId
      );
      command.Parameters.AddWithValue("broadcast_job_id", BroadcastJobId);
      command.Parameters.AddWithValue("person_job_ids", PersonJobIds);
      command.Parameters.AddWithValue("time_zone", SportDay.TimeZoneId);

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
               reader.GetString(1),
               ReadNullableString(reader, 2),
               reader.GetString(3),
               ReadNullableString(reader, 4),
               ReadNullableDateOnly(reader, 5),
               reader.GetString(6),
               ReadNullableString(reader, 7),
               reader.GetString(8),
               reader.GetInt32(9),
               reader.GetInt32(12),
               AiRunSummaryFormatter.Format(
                  ReadNullableString(reader, 13),
                  reader.GetString(1),
                  ReadNullableString(reader, 14)
               ),
               reader.GetFieldValue<DateTimeOffset>(10),
               ReadNullableDecimal(reader, 11)
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
            r.job_id,
            r.execution_environment,
            coalesce(r.job_label, j.label, ''),
            coalesce(
               r.input_payload->>'event_name',
               r.input_payload->>'title',
               a.title,
               ag.title,
               b.title,
               person.canonical_name
            ),
            coalesce(
               a.activity_date,
               ag.start_date,
               (b.starts_at at time zone @time_zone)::date
            ),
            coalesce(r.provider_label, p.label, ''),
            r.provider_model,
            r.status_id,
            r.tool_round_count,
            r.started_at,
            r.duration_seconds,
            greatest(
               r.conversation_character_count,
               coalesce(trace.max_payload_chars, 0)
            ) as max_payload_chars,
            r.output_text,
            coalesce(
               r.prompt_output_schema_json,
               pr.output_schema
            )::text
         from ai_job_runs r
         left join ai_jobs j on j.id = r.job_id
         left join ai_providers p on p.id = r.provider_id
         left join ai_job_prompts pr on pr.id = r.prompt_id
         left join activities a
            on a.id::text = r.correlation_id
               and r.job_id = any(@activity_job_ids)
         left join activity_groups ag
            on ag.id::text = r.correlation_id
               and r.job_id = @activity_group_job_id
         left join broadcasts b
            on b.id::text = r.correlation_id
               and r.job_id = @broadcast_job_id
         left join entities person
            on person.id::text = r.correlation_id
               and r.job_id = any(@person_job_ids)
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
      command.Parameters.AddWithValue("activity_job_ids", ActivityJobIds);
      command.Parameters.AddWithValue(
         "activity_group_job_id",
         ActivityGroupJobId
      );
      command.Parameters.AddWithValue("broadcast_job_id", BroadcastJobId);
      command.Parameters.AddWithValue("person_job_ids", PersonJobIds);
      command.Parameters.AddWithValue("time_zone", SportDay.TimeZoneId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var runs = new List<AiRunListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         runs.Add(
            new AiRunListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               ReadNullableString(reader, 2),
               reader.GetString(3),
               ReadNullableString(reader, 4),
               ReadNullableDateOnly(reader, 5),
               reader.GetString(6),
               ReadNullableString(reader, 7),
               reader.GetString(8),
               reader.GetInt32(9),
               reader.GetInt32(12),
               AiRunSummaryFormatter.Format(
                  ReadNullableString(reader, 13),
                  reader.GetString(1),
                  ReadNullableString(reader, 14)
               ),
               reader.GetFieldValue<DateTimeOffset>(10),
               ReadNullableDecimal(reader, 11)
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
            coalesce(r.job_label, j.label, ''),
            r.prompt_id,
            coalesce(r.prompt_version, pr.version, 0),
            coalesce(r.prompt_system_prompt, pr.system_prompt, ''),
            coalesce(
               r.prompt_user_prompt_template,
               pr.user_prompt_template,
               ''
            ),
            coalesce(r.prompt_temperature, pr.temperature),
            coalesce(r.prompt_max_output_tokens, pr.max_output_tokens),
            coalesce(r.prompt_max_tool_rounds, pr.max_tool_rounds),
            coalesce(r.prompt_min_tool_rounds, pr.min_tool_rounds),
            r.max_output_tokens,
            coalesce(r.prompt_output_schema_json, pr.output_schema)::text,
            coalesce(
               r.prompt_request_options_json,
               pr.request_options
            )::text,
            r.provider_id,
            coalesce(r.provider_label, p.label, ''),
            coalesce(r.provider_kind, p.kind, ''),
            coalesce(r.provider_base_address, p.base_address),
            r.provider_model,
            coalesce(r.provider_api_key_source, p.api_key_source),
            coalesce(
               r.provider_request_options_json,
               p.request_options
            )::text,
            r.status_id,
            r.correlation_id,
            r.input_payload::text,
            r.rendered_system_prompt,
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
            r.execution_environment,
            coalesce(r.job_output_mode, j.output_mode, ''),
            coalesce(r.job_requires_web_search, j.requires_web_search),
            coalesce(
               r.job_include_social_media,
               j.include_social_media,
               false
            ),
            coalesce(r.job_tools_json, j.tools_json)::text,
            coalesce(
               r.job_conditional_tools_json,
               j.conditional_tools_json
            )::text,
            coalesce(r.job_tool_call_max_tokens, j.tool_call_max_tokens),
            r.diagnostic_payload_purged_at
         from ai_job_runs r
         left join ai_jobs j on j.id = r.job_id
         left join ai_providers p on p.id = r.provider_id
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
         Id: reader.GetGuid(0),
         JobId: reader.GetString(1),
         JobLabel: reader.GetString(2),
         PromptId: reader.GetGuid(3),
         PromptVersion: reader.GetInt32(4),
         SystemPrompt: reader.GetString(5),
         UserPromptTemplate: reader.GetString(6),
         PromptTemperature: ReadNullableDecimal(reader, 7),
         PromptMaxOutputTokens: ReadNullableInt32(reader, 8),
         PromptMaxToolRounds: ReadNullableInt32(reader, 9),
         MaxOutputTokens: reader.GetInt32(11),
         PromptOutputSchemaJson: ReadNullableString(reader, 12),
         PromptRequestOptionsJson: reader.GetString(13),
         ProviderId: reader.GetString(14),
         ProviderLabel: reader.GetString(15),
         ProviderKind: reader.GetString(16),
         ProviderBaseAddress: ReadNullableString(reader, 17),
         ProviderModel: ReadNullableString(reader, 18),
         ProviderApiKeySource: ReadNullableString(reader, 19),
         ProviderRequestOptionsJson: reader.GetString(20),
         StatusId: reader.GetString(21),
         CorrelationId: ReadNullableString(reader, 22),
         InputPayloadJson: reader.GetString(23),
         RenderedSystemPrompt: ReadNullableString(reader, 24),
         RenderedPrompt: reader.GetString(25),
         RawRequestJson: ReadNullableString(reader, 26),
         RawResponseJson: ReadNullableString(reader, 27),
         ToolTraceJson: ReadNullableString(reader, 28),
         ToolRoundCount: reader.GetInt32(29),
         ConversationCharacterCount: reader.GetInt32(30),
         OutputText: ReadNullableString(reader, 31),
         ErrorMessage: ReadNullableString(reader, 32),
         StartedAt: reader.GetFieldValue<DateTimeOffset>(33),
         CompletedAt: ReadNullableDateTimeOffset(reader, 34),
         DurationSeconds: ReadNullableDecimal(reader, 35),
         InputTokens: ReadNullableInt32(reader, 36),
         OutputTokens: ReadNullableInt32(reader, 37),
         ReasoningTokens: ReadNullableInt32(reader, 38),
         ExecutionEnvironment: ReadNullableString(reader, 39),
         JobOutputMode: reader.GetString(40),
         JobRequiresWebSearch: reader.GetBoolean(41),
         JobIncludeSocialMedia: reader.GetBoolean(42),
         JobToolsJson: ReadNullableString(reader, 43),
         JobConditionalToolsJson: ReadNullableString(reader, 44),
         JobToolCallMaxTokens: ReadNullableInt32(reader, 45),
         PromptMinToolRounds: ReadNullableInt32(reader, 10),
         DiagnosticPayloadPurgedAt: ReadNullableDateTimeOffset(reader, 46)
      );
   }

   public Task<Guid?> GetExistingRunIdAsync(
      string jobId,
      string correlationId,
      CancellationToken cancellationToken
   )
   {
      return GetRunIdAsync(
         jobId,
         correlationId,
         [
            AiJobRunStatusIds.Pending,
            AiJobRunStatusIds.Running,
            AiJobRunStatusIds.Completed
         ],
         cancellationToken
      );
   }

   public Task<Guid?> GetActiveRunIdAsync(
      string jobId,
      string correlationId,
      CancellationToken cancellationToken
   )
   {
      return GetRunIdAsync(
         jobId,
         correlationId,
         [
            AiJobRunStatusIds.Pending,
            AiJobRunStatusIds.Running
         ],
         cancellationToken
      );
   }

   private async Task<Guid?> GetRunIdAsync(
      string jobId,
      string correlationId,
      string[] statusIds,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select id
         from ai_job_runs
         where job_id = @job_id
            and correlation_id = @correlation_id
            and status_id = any(@status_ids)
         order by created_at desc, id desc
         limit 1
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("job_id", jobId);
      command.Parameters.AddWithValue("correlation_id", correlationId);
      command.Parameters.AddWithValue(
         "status_ids",
         statusIds
      );

      var result = await command.ExecuteScalarAsync(cancellationToken);
      return result is null || result is DBNull ? null : (Guid)result;
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

   public async Task<IReadOnlyList<CompletedActivityGroupFactsRun>>
      GetUnappliedCompletedActivityGroupFactsRunsAsync(
         int maxRuns,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            r.id,
            ag.id,
            r.output_text
         from ai_job_runs r
         join activity_groups ag on ag.id::text = r.correlation_id
         left join ai_job_run_applications app
            on app.run_id = r.id
            and app.target_type = @target_type
            and app.target_id = ag.id::text
         where r.job_id = @job_id
            and r.status_id = @status_id
            and r.prompt_version >= 3
            and app.run_id is null
            and coalesce(r.output_text, '') <> ''
         order by r.completed_at desc, r.id desc
         limit @limit
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "job_id",
         AiJobIds.FindActivityGroupFacts
      );
      command.Parameters.AddWithValue(
         "status_id",
         AiJobRunStatusIds.Completed
      );
      command.Parameters.AddWithValue(
         "target_type",
         AiJobRunApplicationTargetTypes.ActivityGroup
      );
      command.Parameters.AddWithValue("limit", maxRuns);

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var runs = new List<CompletedActivityGroupFactsRun>();

      while(await reader.ReadAsync(cancellationToken))
      {
         runs.Add(
            new CompletedActivityGroupFactsRun(
               reader.GetGuid(0),
               reader.GetGuid(1),
               reader.GetString(2)
            )
         );
      }

      return runs;
   }

   public async Task<IReadOnlyList<Guid>>
      GetUnappliedCompletedActivityParticipantResultRunIdsAsync(
         int maxRuns,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            r.id
         from ai_job_runs r
         join activities a on a.id::text = r.correlation_id
         left join ai_job_run_applications app
            on app.run_id = r.id
            and app.target_type = @target_type
            and app.target_id = a.id::text
         where r.job_id = @job_id
            and r.status_id = @status_id
            and app.run_id is null
            and coalesce(r.output_text, '') <> ''
         order by r.completed_at desc, r.id desc
         limit @limit
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "job_id",
         AiJobIds.FindParticipantsStart
      );
      command.Parameters.AddWithValue(
         "status_id",
         AiJobRunStatusIds.Completed
      );
      command.Parameters.AddWithValue(
         "target_type",
         AiJobRunApplicationTargetTypes.Activity
      );
      command.Parameters.AddWithValue("limit", maxRuns);

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var runs = new List<Guid>();

      while(await reader.ReadAsync(cancellationToken))
      {
         runs.Add(reader.GetGuid(0));
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
            BroadcastParticipationCheckParser.Parse(
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
         ReadNullableString(reader, 2),
         reader.GetString(3),
         reader.GetString(6),
         ReadNullableString(reader, 7),
         ReadNullableString(reader, 8),
         ReadNullableInt32(reader, 9),
         reader.GetBoolean(10),
         reader.GetBoolean(13),
         ReadNullableGuid(reader, 12),
         ReadNullableString(reader, 4),
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
         promptReader.GetBoolean(11),
         ReadNullableInt32(promptReader, 10)
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
         reader.GetBoolean(11),
         ReadNullableInt32(reader, 10)
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
      reader.GetBoolean(11),
      ReadNullableInt32(reader, 10)
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
            id, job_id, job_label, job_output_mode,
            job_requires_web_search, job_tools_json,
            job_include_social_media, job_conditional_tools_json,
            job_tool_call_max_tokens,
            prompt_id, prompt_version, prompt_system_prompt,
            prompt_user_prompt_template, prompt_output_schema_json,
            prompt_request_options_json, prompt_temperature,
            prompt_max_output_tokens, prompt_max_tool_rounds,
            prompt_min_tool_rounds,
            max_output_tokens,
            provider_id, provider_label, provider_kind,
            provider_base_address, provider_model, provider_api_key_source,
            provider_request_options_json, status_id, correlation_id,
            input_payload, rendered_prompt, rendered_system_prompt,
            raw_request, raw_response, tool_trace, output_text, error_message,
            started_at, completed_at, duration_seconds, input_tokens,
            output_tokens, reasoning_tokens, tool_round_count,
            conversation_character_count, execution_environment
         )
         values (
            @id, @job_id, @job_label, @job_output_mode,
            @job_requires_web_search, @job_tools_json,
            @job_include_social_media, @job_conditional_tools_json,
            @job_tool_call_max_tokens,
            @prompt_id, @prompt_version, @prompt_system_prompt,
            @prompt_user_prompt_template, @prompt_output_schema_json,
            @prompt_request_options_json, @prompt_temperature,
            @prompt_max_output_tokens, @prompt_max_tool_rounds,
            @prompt_min_tool_rounds,
            @max_output_tokens,
            @provider_id, @provider_label, @provider_kind,
            @provider_base_address, @provider_model, @provider_api_key_source,
            @provider_request_options_json, @status_id, @correlation_id,
            @input_payload, @rendered_prompt, @rendered_system_prompt,
            @raw_request, @raw_response, @tool_trace, @output_text,
            @error_message, @started_at, @completed_at, @duration_seconds,
            @input_tokens, @output_tokens, @reasoning_tokens,
            @tool_round_count, @conversation_character_count,
            @execution_environment
         )
         """;

      await using var command = dataSource.CreateCommand(sql);
      AddRunParameters(command, run);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task RecordApplicationAsync(
      Guid runId,
      string targetType,
      string targetId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into ai_job_run_applications (
            run_id,
            target_type,
            target_id
         )
         values (
            @run_id,
            @target_type,
            @target_id
         )
         on conflict (run_id, target_type, target_id) do nothing
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("run_id", runId);
      command.Parameters.AddWithValue("target_type", targetType);
      command.Parameters.AddWithValue("target_id", targetId);
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
            select ai_job_runs.id
            from ai_job_runs
            join ai_jobs j on j.id = ai_job_runs.job_id
            where ai_job_runs.status_id in ('pending', 'running')
               and ai_job_runs.execution_environment =
                  @execution_environment
            order by
               ai_job_runs.status_id desc,
               j.queue_priority desc,
               ai_job_runs.started_at asc,
               ai_job_runs.created_at asc,
               ai_job_runs.id asc
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
      AddNullableStringParameter(command, "job_label", run.JobLabel);
      command.Parameters.AddWithValue("job_output_mode", run.JobOutputMode);
      command.Parameters.AddWithValue(
         "job_requires_web_search",
         run.JobRequiresWebSearch
      );
      command.Parameters.AddWithValue(
         "job_include_social_media",
         run.JobIncludeSocialMedia
      );
      AddJsonbParameter(command, "job_tools_json", run.JobToolsJson);
      AddJsonbParameter(
         command,
         "job_conditional_tools_json",
         run.JobConditionalToolsJson
      );
      command.Parameters.AddWithValue(
         "job_tool_call_max_tokens",
         (object?)run.JobToolCallMaxTokens ?? DBNull.Value
      );
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
      AddJsonbParameter(
         command,
         "prompt_output_schema_json",
         run.PromptOutputSchemaJson
      );
      AddJsonbParameter(
         command,
         "prompt_request_options_json",
         run.PromptRequestOptionsJson
      );
      command.Parameters.AddWithValue(
         "prompt_temperature",
         (object?)run.PromptTemperature ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "prompt_max_output_tokens",
         (object?)run.PromptMaxOutputTokens ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "prompt_max_tool_rounds",
         (object?)run.PromptMaxToolRounds ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "prompt_min_tool_rounds",
         (object?)run.PromptMinToolRounds ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "max_output_tokens",
         run.MaxOutputTokens
      );
      command.Parameters.AddWithValue("provider_id", run.ProviderId);
      AddNullableStringParameter(command, "provider_label", run.ProviderLabel);
      command.Parameters.AddWithValue("provider_kind", run.ProviderKind);
      AddNullableStringParameter(
         command,
         "provider_base_address",
         run.ProviderBaseAddress
      );
      AddNullableStringParameter(command, "provider_model", run.ProviderModel);
      AddNullableStringParameter(
         command,
         "provider_api_key_source",
         run.ProviderApiKeySource
      );
      AddJsonbParameter(
         command,
         "provider_request_options_json",
         run.ProviderRequestOptionsJson
      );
      command.Parameters.AddWithValue("status_id", ToStatusId(run.Status));
      command.Parameters.AddWithValue(
         "correlation_id",
         (object?)run.CorrelationId ?? DBNull.Value
      );
      AddJsonbParameter(command, "input_payload", run.InputPayloadJson);
      command.Parameters.AddWithValue("rendered_prompt", run.RenderedPrompt);
      AddNullableStringParameter(
         command,
         "rendered_system_prompt",
         run.RenderedSystemPrompt
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
      var normalizedValue = PostgreSqlJson.Normalize(value);
      var jsonValue = (object?)normalizedValue ?? DBNull.Value;

      command.Parameters.Add(
         new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
         {
            Value = jsonValue
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

   private static DateOnly? ReadNullableDateOnly(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetFieldValue<DateOnly>(ordinal);
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

public sealed record CompletedActivityGroupFactsRun(
   Guid RunId,
   Guid ActivityGroupId,
   string OutputText
);
