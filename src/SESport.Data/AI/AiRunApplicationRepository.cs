using Npgsql;

using SESport.Core.AI;
using SESport.Core.Broadcast;

namespace SESport.Data.AI;

public sealed class AiRunApplicationRepository(NpgsqlDataSource dataSource)
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
         var correlationId = PostgresHelpers.ReadNullableString(reader, 0);

         if(!Guid.TryParse(correlationId, out var broadcastId))
         {
            continue;
         }

         var runId = reader.GetGuid(1);
         var statusId = reader.GetString(2);
         var toolRoundCount = reader.GetInt32(3);
         var outputText = PostgresHelpers.ReadNullableString(reader, 4);
         var rawResponseText = PostgresHelpers.ReadNullableString(reader, 5);
         var errorMessage = PostgresHelpers.ReadNullableString(reader, 6);

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
