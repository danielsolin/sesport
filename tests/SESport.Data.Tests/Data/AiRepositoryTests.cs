using Npgsql;
using SESport.Core.AI;
using SESport.Core.Formatting;
using SESport.Data.Repositories;
using System.Text.Json;

namespace SESport.Core.Tests.Data;

public sealed class AiRepositoryTests
{
   [Fact]
   public async Task GetActiveRunIdAsyncExcludesCompletedRuns()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var completedRunId = Guid.NewGuid();
      var runningRunId = Guid.NewGuid();
      var completedCorrelation = $"completed-{Guid.NewGuid():N}";
      var runningCorrelation = $"running-{Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      try
      {
         await InsertProviderAsync(dataSource, providerId);
         await InsertJobAsync(dataSource, jobId, providerId);
         await InsertPromptAsync(dataSource, promptId, jobId);
         await InsertRunAsync(
            dataSource,
            completedRunId,
            jobId,
            promptId,
            providerId,
            correlationId: completedCorrelation,
            statusId: AiJobRunStatusIds.Completed
         );
         await InsertRunAsync(
            dataSource,
            runningRunId,
            jobId,
            promptId,
            providerId,
            correlationId: runningCorrelation,
            statusId: AiJobRunStatusIds.Running
         );

         Assert.Null(
            await repository.GetActiveRunIdAsync(
               jobId,
               completedCorrelation,
               CancellationToken.None
            )
         );
         Assert.Equal(
            completedRunId,
            await repository.GetExistingRunIdAsync(
               jobId,
               completedCorrelation,
               CancellationToken.None
            )
         );
         Assert.Equal(
            runningRunId,
            await repository.GetActiveRunIdAsync(
               jobId,
               runningCorrelation,
               CancellationToken.None
            )
         );
      }
      finally
      {
         await DeleteRunAsync(dataSource, runningRunId);
         await DeleteRunAsync(dataSource, completedRunId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task RecordApplicationAsyncStoresApplicationOnce()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();
      var activityId = Guid.NewGuid().ToString();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId
      );

      try
      {
         await repository.RecordApplicationAsync(
            runId,
            AiJobRunApplicationTargetTypes.Activity,
            activityId,
            CancellationToken.None
         );
         await repository.RecordApplicationAsync(
            runId,
            AiJobRunApplicationTargetTypes.Activity,
            activityId,
            CancellationToken.None
         );

         await using var command = dataSource.CreateCommand(
            """
            select count(*)
            from ai_job_run_applications
            where run_id = @run_id
               and target_type = @target_type
               and target_id = @target_id
            """
         );
         command.Parameters.AddWithValue("run_id", runId);
         command.Parameters.AddWithValue(
            "target_type",
            AiJobRunApplicationTargetTypes.Activity
         );
         command.Parameters.AddWithValue("target_id", activityId);

         Assert.Equal(
            1L,
            (long)(await command.ExecuteScalarAsync())!
         );
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunsAsyncUsesLocalDateBoundaries()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();
      var localDate = new DateOnly(2026, 6, 15);
      var startedAt = TimeZoneHelper.ToUtc(
         localDate,
         new TimeOnly(0, 52, 1),
         SportDay.TimeZoneId
      );

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId, version: 999);
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId,
         startedAt
      );

      try
      {
         var previousDayRuns = await repository.GetRunsAsync(
            localDate.AddDays(-1),
            null,
            null,
            CancellationToken.None
         );
         var localDayRuns = await repository.GetRunsAsync(
            localDate,
            null,
            null,
            CancellationToken.None
         );

         Assert.DoesNotContain(previousDayRuns, run => run.Id == runId);
         Assert.Contains(localDayRuns, run => run.Id == runId);
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunsAsyncSkipsArchivedRunsByDefault()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var activeRunId = Guid.NewGuid();
      var archivedRunId = Guid.NewGuid();
      var localDate = new DateOnly(2026, 6, 15);
      var startedAt = TimeZoneHelper.ToUtc(
         localDate,
         new TimeOnly(8, 0),
         SportDay.TimeZoneId
      );

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         activeRunId,
         jobId,
         promptId,
         providerId,
         startedAt,
         statusId: AiJobRunStatusIds.Running
      );
      await InsertRunAsync(
         dataSource,
         archivedRunId,
         jobId,
         promptId,
         providerId,
         startedAt,
         statusId: "archived"
      );

      try
      {
         var runs = await repository.GetRunsAsync(
            localDate,
            null,
            null,
            CancellationToken.None
         );

         Assert.Contains(runs, run => run.Id == activeRunId);
         Assert.DoesNotContain(runs, run => run.Id == archivedRunId);

         var archivedRuns = await repository.GetRunsAsync(
            localDate,
            null,
            [AiJobRunStatusIds.Archived],
            CancellationToken.None
         );

         Assert.Equal([archivedRunId], archivedRuns.Select(run => run.Id));
      }
      finally
      {
         await DeleteRunAsync(dataSource, archivedRunId);
         await DeleteRunAsync(dataSource, activeRunId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunsAsyncOrdersRunningBeforePendingByDefault()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runningRunId = Guid.NewGuid();
      var pendingRunId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         runningRunId,
         jobId,
         promptId,
         providerId,
         DateTimeOffset.UtcNow.AddMinutes(-30),
         statusId: AiJobRunStatusIds.Running
      );
      await InsertRunAsync(
         dataSource,
         pendingRunId,
         jobId,
         promptId,
         providerId,
         DateTimeOffset.UtcNow.AddMinutes(-5),
         statusId: AiJobRunStatusIds.Pending
      );

      try
      {
         var runs = await repository.GetRunsAsync(
            null,
            jobId,
            null,
            CancellationToken.None
         );

         Assert.Equal(
            [runningRunId, pendingRunId],
            runs.Select(run => run.Id)
         );
      }
      finally
      {
         await DeleteRunAsync(dataSource, pendingRunId);
         await DeleteRunAsync(dataSource, runningRunId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunsAsyncOrdersPendingRunsByQueuePriority()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var highPriorityJobId = $"test-job-{Guid.NewGuid():N}";
      var lowPriorityJobId = $"test-job-{Guid.NewGuid():N}";
      var highPriorityPromptId = Guid.NewGuid();
      var lowPriorityPromptId = Guid.NewGuid();
      var highPriorityRunId = Guid.NewGuid();
      var lowPriorityRunId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(
         dataSource,
         highPriorityJobId,
         providerId,
         queuePriority: 100
      );
      await InsertJobAsync(
         dataSource,
         lowPriorityJobId,
         providerId,
         queuePriority: 10
      );
      await InsertPromptAsync(
         dataSource,
         highPriorityPromptId,
         highPriorityJobId
      );
      await InsertPromptAsync(
         dataSource,
         lowPriorityPromptId,
         lowPriorityJobId
      );
      await InsertRunAsync(
         dataSource,
         highPriorityRunId,
         highPriorityJobId,
         highPriorityPromptId,
         providerId,
         statusId: AiJobRunStatusIds.Pending
      );
      await InsertRunAsync(
         dataSource,
         lowPriorityRunId,
         lowPriorityJobId,
         lowPriorityPromptId,
         providerId,
         statusId: AiJobRunStatusIds.Pending
      );

      try
      {
         var runs = await repository.GetRunsAsync(
            null,
            null,
            [AiJobRunStatusIds.Pending],
            CancellationToken.None
         );
         var runIds = runs.Select(run => run.Id).ToList();

         Assert.True(
            runIds.IndexOf(highPriorityRunId)
               < runIds.IndexOf(lowPriorityRunId)
         );
      }
      finally
      {
         await DeleteRunAsync(dataSource, lowPriorityRunId);
         await DeleteRunAsync(dataSource, highPriorityRunId);
         await DeletePromptAsync(dataSource, lowPriorityPromptId);
         await DeletePromptAsync(dataSource, highPriorityPromptId);
         await DeleteJobAsync(dataSource, lowPriorityJobId);
         await DeleteJobAsync(dataSource, highPriorityJobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunsAsyncUsesPayloadTitleWhenEventNameIsMissing()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();
      const string eventTitle = "Activity title";

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId,
         inputPayloadJson: $$"""
            {
              "title": "{{eventTitle}}"
            }
            """
      );

      try
      {
         var runs = await repository.GetRunsAsync(
            null,
            jobId,
            null,
            CancellationToken.None
         );

         Assert.Equal(eventTitle, Assert.Single(runs).EventName);
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunsAsyncUsesShortSummaryForJsonOutput()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId,
         outputText: """
            {
              "Participants": [
                {
                  "Name": "Alice"
                },
                {
                  "Name": "Bob"
                }
              ],
              "CheckedSources": [],
              "Participation": "Yes"
            }
            """
      );

      try
      {
         var runs = await repository.GetRunsAsync(
            null,
            jobId,
            null,
            CancellationToken.None
         );

         Assert.Equal("2 participants", Assert.Single(runs).ResultSummary);
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunsAsyncReturnsMaxPayloadCharacterCount()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId,
         statusId: AiJobRunStatusIds.Running,
         conversationCharacterCount: 12345,
         toolTraceJson: """
            [
              {
                "kind": "budget",
                "turn": 2,
                "payload_chars": 45678
              }
            ]
            """
      );

      try
      {
         var runs = await repository.GetRunsAsync(
            null,
            jobId,
            null,
            CancellationToken.None
         );

         var run = Assert.Single(runs, item => item.Id == runId);
         Assert.Equal(45678, run.MaxPayloadCharacterCount);
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunsAsyncUsesConversationCharacterCountAsPayloadFloor()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId,
         statusId: AiJobRunStatusIds.Running,
         conversationCharacterCount: 50000,
         toolTraceJson: """
            [
              {
                "kind": "budget",
                "turn": 1,
                "payload_chars": 20000
              }
            ]
            """
      );

      try
      {
         var runs = await repository.GetRunsAsync(
            null,
            jobId,
            null,
            CancellationToken.None
         );

         var run = Assert.Single(runs, item => item.Id == runId);
         Assert.Equal(50000, run.MaxPayloadCharacterCount);
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunsAsyncReturnsFirst500MatchingRows()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runIds = new List<Guid>();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);

      for(var index = 0; index < 501; index++)
      {
         var runId = Guid.NewGuid();
         runIds.Add(runId);

         await InsertRunAsync(
            dataSource,
            runId,
            jobId,
            promptId,
            providerId,
            DateTimeOffset.UtcNow.AddMinutes(-index),
            statusId: AiJobRunStatusIds.Running
         );
      }

      try
      {
         var runs = await repository.GetRunsAsync(
            null,
            jobId,
            null,
            CancellationToken.None
         );

         Assert.Equal(500, runs.Count);
         Assert.Equal(runIds[^1], runs.First().Id);
         Assert.DoesNotContain(runIds[0], runs.Select(run => run.Id));
      }
      finally
      {
         foreach(var runId in runIds)
         {
            await DeleteRunAsync(dataSource, runId);
         }

         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunsByIdsAsyncReturnsMatchingRuns()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var firstRunId = Guid.NewGuid();
      var secondRunId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         firstRunId,
         jobId,
         promptId,
         providerId,
         statusId: "running",
         toolRoundCount: 2,
         durationSeconds: null,
         toolTraceJson: """
            [{"kind":"tool_call","turn":1},{"kind":"tool_call","turn":2}]
            """
      );
      await InsertRunAsync(
         dataSource,
         secondRunId,
         jobId,
         promptId,
         providerId,
         statusId: "completed",
         toolRoundCount: 1,
         durationSeconds: 12m
      );

      try
      {
         var runs = await repository.GetRunsByIdsAsync(
            [firstRunId, secondRunId],
            CancellationToken.None
         );

         Assert.Equal([secondRunId, firstRunId], runs.Select(run => run.Id));

         var firstRun = runs.Single(run => run.Id == firstRunId);
         var secondRun = runs.Single(run => run.Id == secondRunId);

         Assert.Equal("running", firstRun.StatusId);
         Assert.Equal(2, firstRun.ToolRoundCount);
         Assert.Equal("completed", secondRun.StatusId);
         Assert.Equal(1, secondRun.ToolRoundCount);
         Assert.Equal(12m, secondRun.DurationSeconds);
      }
      finally
      {
         await DeleteRunAsync(dataSource, secondRunId);
         await DeleteRunAsync(dataSource, firstRunId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunsByIdsAsyncSkipsArchivedRuns()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var archivedRunId = Guid.NewGuid();
      var activeRunId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         archivedRunId,
         jobId,
         promptId,
         providerId,
         statusId: "archived"
      );
      await InsertRunAsync(
         dataSource,
         activeRunId,
         jobId,
         promptId,
         providerId,
         statusId: "running"
      );

      try
      {
         var runs = await repository.GetRunsByIdsAsync(
            [archivedRunId, activeRunId],
            CancellationToken.None
         );

         Assert.Equal([activeRunId], runs.Select(run => run.Id));
      }
      finally
      {
         await DeleteRunAsync(dataSource, activeRunId);
         await DeleteRunAsync(dataSource, archivedRunId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task ArchiveRunAsyncMarksRunArchived()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId,
         statusId: "running"
      );

      try
      {
         var archived = await repository.ArchiveRunAsync(
            runId,
            CancellationToken.None
         );

         Assert.True(archived);

         await using var connection = await dataSource.OpenConnectionAsync();
         await using var command = connection.CreateCommand();
         command.CommandText = """
            select status_id
            from ai_job_runs
            where id = @id
            """;
         command.Parameters.AddWithValue("id", runId);

         await using var reader = await command.ExecuteReaderAsync();
         Assert.True(await reader.ReadAsync());
         Assert.Equal("archived", reader.GetString(0));
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task FailStaleRunningRunsAsyncMarksOldRunsAsFailed()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId
      );

      try
      {
         var updatedCount = await repository.FailStaleRunningRunsAsync(
            TimeSpan.FromHours(1),
            CancellationToken.None
         );

         Assert.True(updatedCount >= 1);

         await using var connection = await dataSource.OpenConnectionAsync();
         await using var command = connection.CreateCommand();
         command.CommandText = """
            select status_id, error_message, completed_at, duration_seconds
            from ai_job_runs
            where id = @id
            """;
         command.Parameters.AddWithValue("id", runId);

         await using var reader = await command.ExecuteReaderAsync();
         Assert.True(await reader.ReadAsync());
         Assert.Equal("failed", reader.GetString(0));
         Assert.Equal("Run timed out after 1 hour.", reader.GetString(1));
         _ = reader.GetFieldValue<DateTimeOffset>(2);
         Assert.True(reader.GetDecimal(3) >= 3600m);
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task ClaimNextRunAsyncSkipsOtherExecutionEnvironments()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var otherRunId = Guid.NewGuid();
      var matchingRunId = Guid.NewGuid();
      var otherExecutionEnvironment = $"other-worker-{Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         otherRunId,
         jobId,
         promptId,
         providerId,
         DateTimeOffset.UtcNow.AddHours(-3),
         otherExecutionEnvironment
      );
      await InsertRunAsync(
         dataSource,
         matchingRunId,
         jobId,
         promptId,
         providerId,
         DateTimeOffset.UtcNow.AddHours(-2),
         ExecutionEnvironment.Current
      );

      try
      {
         var claimedRunId = await repository.ClaimNextRunAsync(
            CancellationToken.None
         );

         Assert.Equal(matchingRunId, claimedRunId);

         await using var connection = await dataSource.OpenConnectionAsync();
         await using var command = connection.CreateCommand();
         command.CommandText = """
            select id, status_id, started_at
            from ai_job_runs
            where id in (@other_run_id, @matching_run_id)
            """;
         command.Parameters.AddWithValue("other_run_id", otherRunId);
         command.Parameters.AddWithValue("matching_run_id", matchingRunId);

         await using var reader = await command.ExecuteReaderAsync();
         var runs = new Dictionary<Guid,
            (string statusId, DateTimeOffset startedAt)>();

         while(await reader.ReadAsync())
         {
            runs[reader.GetGuid(0)] = (
               reader.GetString(1),
               reader.GetFieldValue<DateTimeOffset>(2)
            );
         }

         Assert.Equal("running", runs[otherRunId].statusId);
         Assert.Equal("running", runs[matchingRunId].statusId);
         Assert.True(
            runs[otherRunId].startedAt < runs[matchingRunId].startedAt
         );
      }
      finally
      {
         await DeleteRunAsync(dataSource, matchingRunId);
         await DeleteRunAsync(dataSource, otherRunId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunAsyncReturnsExecutionEnvironment()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();
      var executionEnvironment = $"worker-{Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId,
         executionEnvironment: executionEnvironment
      );

      try
      {
         var run = await repository.GetRunAsync(
            runId,
            CancellationToken.None
         );

         Assert.NotNull(run);
         Assert.Equal(executionEnvironment, run!.ExecutionEnvironment);
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task UpdateToolTraceAsyncSanitizesUnsupportedUnicode()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();
      const string toolTraceJson =
         """
         [{
            "result":"Rally\u0000 Polen \uD800",
            "emoji":"😀",
            "literal":"\\u0000",
            "ok\u0000key":"värde"
         }]
         """;

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId
      );

      try
      {
         await repository.UpdateToolTraceAsync(
            runId,
            toolTraceJson,
            1,
            CancellationToken.None
         );

         var run = await repository.GetRunAsync(
            runId,
            CancellationToken.None
         );
         using var document = JsonDocument.Parse(run!.ToolTraceJson!);
         var trace = document.RootElement[0];

         Assert.Equal("Rally Polen �", trace.GetProperty("result").GetString());
         Assert.Equal("😀", trace.GetProperty("emoji").GetString());
         Assert.Equal(
            @"\u0000",
            trace.GetProperty("literal").GetString()
         );
         Assert.Equal("värde", trace.GetProperty("okkey").GetString());
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetRunAsyncReturnsStoredMaxOutputTokens()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(
         dataSource,
         promptId,
         jobId,
         maxOutputTokens: null
      );
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId
      );

      try
      {
         var run = await repository.GetRunAsync(
            runId,
            CancellationToken.None
         );

         Assert.NotNull(run);
         Assert.Null(run!.PromptMaxOutputTokens);
         Assert.Equal(
            AiDefaults.DefaultMaxOutputTokens,
            run.MaxOutputTokens
         );
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task GetJobAsyncReturnsToolCallMaxTokens()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(
         dataSource,
         jobId,
         providerId,
         toolCallMaxTokens: 2048
      );

      try
      {
         var job = await repository.GetJobAsync(
            jobId,
            CancellationToken.None
         );

         Assert.NotNull(job);
         Assert.Equal(2048, job!.ToolCallMaxTokens);
      }
      finally
      {
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   [Fact]
   public async Task UpdateRunExecutionEnvironmentAsyncUpdatesStoredRun()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();
      var newExecutionEnvironment = $"worker-{Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(dataSource, runId, jobId, promptId, providerId);

      try
      {
         await repository.UpdateRunExecutionEnvironmentAsync(
            runId,
            newExecutionEnvironment,
            CancellationToken.None
         );

         await using var connection = await dataSource.OpenConnectionAsync();
         await using var command = connection.CreateCommand();
         command.CommandText = """
            select execution_environment
            from ai_job_runs
            where id = @id
            """;
         command.Parameters.AddWithValue("id", runId);

         await using var reader = await command.ExecuteReaderAsync();
         Assert.True(await reader.ReadAsync());
         Assert.Equal(
            newExecutionEnvironment,
            reader.GetString(0)
         );
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   private static async Task InsertProviderAsync(
      NpgsqlDataSource dataSource,
      string providerId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into ai_providers (
            id,
            label,
            kind,
            base_address,
            model,
            api_key_source,
            request_options,
            enabled,
            created_at,
            updated_at
         )
         values (
            @id,
            @label,
            'llama-server',
            'http://127.0.0.1:8080/v1/',
            'gpt',
            'key:secret',
            '{}'::jsonb,
            true,
            now(),
            now()
         )
         """;
      command.Parameters.AddWithValue("id", providerId);
      command.Parameters.AddWithValue("label", "Test provider");
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertJobAsync(
      NpgsqlDataSource dataSource,
      string jobId,
      string providerId,
      int? toolCallMaxTokens = null,
      int queuePriority = 0
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into ai_jobs (
            id,
            label,
            provider_id,
            output_mode,
            tool_call_max_tokens,
            queue_priority,
            enabled,
            created_at,
            updated_at,
            requires_web_search
         )
         values (
            @id,
            @label,
            @provider_id,
            'json_object',
            @tool_call_max_tokens,
            @queue_priority,
            true,
            now(),
            now(),
            false
         )
         on conflict (id) do nothing
         """;
      command.Parameters.AddWithValue("id", jobId);
      command.Parameters.AddWithValue("label", "Test job");
      command.Parameters.AddWithValue("provider_id", providerId);
      command.Parameters.AddWithValue(
         "tool_call_max_tokens",
         (object?)toolCallMaxTokens ?? DBNull.Value
      );
      command.Parameters.AddWithValue("queue_priority", queuePriority);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertPromptAsync(
      NpgsqlDataSource dataSource,
      Guid promptId,
      string jobId,
      int version = 1,
      int? maxOutputTokens = null
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into ai_job_prompts (
            id,
            job_id,
            version,
            system_prompt,
            user_prompt_template,
            output_schema,
            temperature,
            max_output_tokens,
            enabled,
            created_at,
            updated_at,
            request_options,
            max_tool_rounds
         )
         values (
            @id,
            @job_id,
            @version,
            'System',
            'User',
            null,
            null,
            @max_output_tokens,
            true,
            now(),
            now(),
            '{}'::jsonb,
            null
         )
         """;
      command.Parameters.AddWithValue("id", promptId);
      command.Parameters.AddWithValue("job_id", jobId);
      command.Parameters.AddWithValue("version", version);
      command.Parameters.AddWithValue(
         "max_output_tokens",
         (object?)maxOutputTokens ?? DBNull.Value
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertRunAsync(
      NpgsqlDataSource dataSource,
      Guid runId,
      string jobId,
      Guid promptId,
      string providerId,
      DateTimeOffset? startedAt = null,
      string? executionEnvironment = null,
      string? correlationId = null,
      string statusId = "running",
      int toolRoundCount = 0,
      int conversationCharacterCount = 0,
      decimal? durationSeconds = null,
      string? toolTraceJson = null,
      string inputPayloadJson = "{}",
      string? outputText = null,
      int maxOutputTokens = AiDefaults.DefaultMaxOutputTokens
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into ai_job_runs (
            id,
            job_id,
            prompt_id,
            provider_id,
            status_id,
            correlation_id,
            provider_model,
            input_payload,
            rendered_prompt,
            raw_request,
            raw_response,
            tool_trace,
            output_text,
            error_message,
            started_at,
            completed_at,
            duration_seconds,
            input_tokens,
            output_tokens,
            reasoning_tokens,
            tool_round_count,
            conversation_character_count,
            execution_environment,
            max_output_tokens
         )
         values (
            @id,
            @job_id,
            @prompt_id,
            @provider_id,
            @status_id,
            @correlation_id,
            'gpt',
            @input_payload::jsonb,
            'Rendered',
            null,
            null,
            @tool_trace::jsonb,
            @output_text,
            null,
            @started_at,
            null,
            @duration_seconds,
            null,
            null,
            null,
            @tool_round_count,
            @conversation_character_count,
            @execution_environment,
            @max_output_tokens
         )
         """;
      command.Parameters.AddWithValue("id", runId);
      command.Parameters.AddWithValue("job_id", jobId);
      command.Parameters.AddWithValue("prompt_id", promptId);
      command.Parameters.AddWithValue("provider_id", providerId);
      command.Parameters.AddWithValue(
         "correlation_id",
         (object?)correlationId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "execution_environment",
         executionEnvironment ?? ExecutionEnvironment.Current
      );
      command.Parameters.AddWithValue(
         "started_at",
         startedAt ?? DateTimeOffset.UtcNow.AddHours(-2)
      );
      command.Parameters.AddWithValue("status_id", statusId);
      command.Parameters.AddWithValue(
         "tool_trace",
         (object?)toolTraceJson ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "output_text",
         (object?)outputText ?? DBNull.Value
      );
      command.Parameters.AddWithValue("input_payload", inputPayloadJson);
      command.Parameters.AddWithValue("max_output_tokens", maxOutputTokens);
      command.Parameters.AddWithValue(
         "duration_seconds",
         (object?)durationSeconds ?? DBNull.Value
      );
      command.Parameters.AddWithValue("tool_round_count", toolRoundCount);
      command.Parameters.AddWithValue(
         "conversation_character_count",
         conversationCharacterCount
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteRunAsync(
      NpgsqlDataSource dataSource,
      Guid runId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from ai_job_runs
         where id = @id
         """;
      command.Parameters.AddWithValue("id", runId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeletePromptAsync(
      NpgsqlDataSource dataSource,
      Guid promptId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from ai_job_prompts
         where id = @id
         """;
      command.Parameters.AddWithValue("id", promptId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteJobAsync(
      NpgsqlDataSource dataSource,
      string jobId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from ai_jobs
         where id = @id
         """;
      command.Parameters.AddWithValue("id", jobId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteProviderAsync(
      NpgsqlDataSource dataSource,
      string providerId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from ai_providers
         where id = @id
         """;
      command.Parameters.AddWithValue("id", providerId);
      await command.ExecuteNonQueryAsync();
   }
}
