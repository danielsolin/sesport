using Npgsql;
using SESport.Data;

namespace SESport.Web.Services;

public sealed class ActivityTeaserCatchUpWorker(
   IServiceScopeFactory scopeFactory,
   ILogger<ActivityTeaserCatchUpWorker> logger
) : BackgroundService
{
   private const string TeaserJobId = "generate-activity-teaser";
   private const string CompletedStatusId = "completed";
   private const int MaxRuns = 50;

   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      try
      {
         using var scope = scopeFactory.CreateScope();
         var dataSource = scope.ServiceProvider
            .GetRequiredService<NpgsqlDataSource>();
         var activityRepository = scope.ServiceProvider
            .GetRequiredService<ActivityRepository>();

         var runs = await GetCompletedRunsWithEmptyActivityTeasersAsync(
            dataSource,
            stoppingToken
         );

         foreach(var run in runs)
         {
            var teaser = ActivityTeaserJobProcessor.ExtractGeneratedTeaser(
               run.OutputText
            );

            if(string.IsNullOrWhiteSpace(teaser))
            {
               continue;
            }

            var updated = await activityRepository.UpdateEmptyTeaserAsync(
               run.ActivityId,
               teaser,
               stoppingToken
            );

            if(updated)
            {
               logger.LogInformation(
                  "Saved missed activity teaser from AI run {RunId}.",
                  run.RunId
               );
            }
         }
      }
      catch(OperationCanceledException)
         when(stoppingToken.IsCancellationRequested)
      {
      }
      catch(Exception exception)
      {
         logger.LogError(
            exception,
            "Activity teaser catch-up failed."
         );
      }
   }

   private static async Task<IReadOnlyList<CompletedTeaserRun>>
      GetCompletedRunsWithEmptyActivityTeasersAsync(
         NpgsqlDataSource dataSource,
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
      command.Parameters.AddWithValue("job_id", TeaserJobId);
      command.Parameters.AddWithValue("status_id", CompletedStatusId);
      command.Parameters.AddWithValue("limit", MaxRuns);

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var runs = new List<CompletedTeaserRun>();

      while(await reader.ReadAsync(cancellationToken))
      {
         runs.Add(
            new CompletedTeaserRun(
               reader.GetGuid(0),
               reader.GetGuid(1),
               reader.GetString(2)
            )
         );
      }

      return runs;
   }

   private sealed record CompletedTeaserRun(
      Guid RunId,
      Guid ActivityId,
      string OutputText
   );
}
