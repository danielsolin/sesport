using Npgsql;

using SESport.AI;
using SESport.AI.Models;
using SESport.AI.Persistence;
using SESport.Core.Configuration;
using SESport.Core.Formatting;

namespace SESport.Core.Tests.Data;

public sealed class AiRepositoryTests
{
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
   public async Task GetRunsAsyncLimitsResultsToFiftyRows()
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

      for(var index = 0; index < 51; index++)
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

         Assert.Equal(50, runs.Count);
         Assert.Equal(runIds[0], runs.First().Id);
         Assert.DoesNotContain(runIds[^1], runs.Select(run => run.Id));
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

   private static NpgsqlDataSource CreateDataSource()
   {
      var connectionString = PostgresConnectionStrings.ResolveDefault();

      return new NpgsqlDataSourceBuilder(connectionString).Build();
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
      string providerId
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
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertPromptAsync(
      NpgsqlDataSource dataSource,
      Guid promptId,
      string jobId,
      int version = 1
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
            null,
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
      decimal? durationSeconds = null,
      string? toolTraceJson = null
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
            execution_environment
         )
         values (
            @id,
            @job_id,
            @prompt_id,
            @provider_id,
            @status_id,
            @correlation_id,
            'gpt',
            '{}'::jsonb,
            'Rendered',
            null,
            null,
            @tool_trace::jsonb,
            null,
            null,
            @started_at,
            null,
            @duration_seconds,
            null,
            null,
            null,
            @tool_round_count,
            0,
            @execution_environment
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
         "duration_seconds",
         (object?)durationSeconds ?? DBNull.Value
      );
      command.Parameters.AddWithValue("tool_round_count", toolRoundCount);
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
