using Microsoft.AspNetCore.Mvc;

using Npgsql;

using SESport.AI.Interfaces;
using SESport.Data.Configuration;
using SESport.Data;
using SESport.Data.AI;
using SESport.Web.Pages.Admin.Ajax.Update;
using SESport.Web.Services;

namespace SESport.Core.Tests.Pages.Admin.Ajax.Update;

public sealed class RunFieldModelTests
{
   [Fact]
   public async Task OnPostAsyncArchivesRunAndReturnsJson()
   {
      var broadcastId = Guid.NewGuid();
      var runId = Guid.NewGuid();
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);
      var model = new RunFieldModel(repository, CreateService(dataSource));

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         null,
         $"external-{Guid.NewGuid():N}",
         $"fingerprint-{Guid.NewGuid():N}",
         "channel-1",
         "Viaplay",
         "Broadcast title",
         ["Old", "Categories"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2)
      );
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId,
         correlationId: broadcastId.ToString(),
         statusId: "completed",
         toolRoundCount: 3,
         durationSeconds: 12.5m
      );

      try
      {
         var result = await model.OnPostAsync(
            runId,
            "archive",
            null,
            CancellationToken.None
         );

         var jsonResult = Assert.IsType<JsonResult>(result);
         var payload = jsonResult.Value;

         Assert.NotNull(payload);

         Assert.True(GetRequiredProperty<bool>(payload, "updated"));
         Assert.Equal("archive", GetRequiredProperty<string>(payload, "field"));

         var archivedRun = await repository.GetRunAsync(
            runId,
            CancellationToken.None
         );

         Assert.NotNull(archivedRun);
         Assert.Equal("archived", archivedRun!.StatusId);

         var archivedResult = GetRequiredPropertyValue(payload, "result");
         Assert.Equal(
            broadcastId.ToString(),
            GetRequiredProperty<string>(archivedResult, "id")
         );
         Assert.Empty(
            Assert.IsAssignableFrom<IEnumerable<object>>(
               GetRequiredPropertyValue(archivedResult, "checks")
            )
         );
      }
      finally
      {
         await DeleteRunAsync(dataSource, runId);
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeletePromptAsync(dataSource, promptId);
         await DeleteJobAsync(dataSource, jobId);
         await DeleteProviderAsync(dataSource, providerId);
      }
   }

   private static T GetRequiredProperty<T>(object value, string name)
   {
      var property = value.GetType().GetProperty(name);

      Assert.NotNull(property);

      return Assert.IsType<T>(property!.GetValue(value));
   }

   private static object GetRequiredPropertyValue(object value, string name)
   {
      var property = value.GetType().GetProperty(name);

      Assert.NotNull(property);

      var propertyValue = property!.GetValue(value);

      Assert.NotNull(propertyValue);

      return propertyValue;
   }

   private static BroadcastParticipationService CreateService(
      NpgsqlDataSource dataSource
   )
   {
      return new BroadcastParticipationService(
         new ActivityRepository(dataSource),
         new AiRepository(dataSource),
         new AdminRepository(dataSource),
         new AdminBroadcastRepository(dataSource),
         new CapturingAiJobRunner()
      );
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
      decimal? durationSeconds = null
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
            null,
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
      command.Parameters.AddWithValue("status_id", statusId);
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
      command.Parameters.AddWithValue(
         "duration_seconds",
         (object?)durationSeconds ?? DBNull.Value
      );
      command.Parameters.AddWithValue("tool_round_count", toolRoundCount);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertBroadcastAsync(
      NpgsqlDataSource dataSource,
      Guid broadcastId,
      string sourceKey,
      Guid? entityId,
      string externalId,
      string fingerprint,
      string channelId,
      string channelName,
      string title,
      string[] categories,
      DateTimeOffset startsAt,
      DateTimeOffset endsAt
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into broadcasts (
            id,
            source_key,
            external_id,
            fingerprint,
            entity_id,
            channel_id,
            channel_name,
            title,
            description,
            categories,
            is_replay,
            original_air_date,
            starts_at,
            ends_at,
            time_zone_id,
            raw_programme_xml
         )
         values (
            @id,
            @source_key,
            @external_id,
            @fingerprint,
            @entity_id,
            @channel_id,
            @channel_name,
            @title,
            null,
            @categories,
            false,
            null,
            @starts_at,
            @ends_at,
            'Europe/Stockholm',
            null
         )
         """;
      command.Parameters.AddWithValue("id", broadcastId);
      command.Parameters.AddWithValue("source_key", sourceKey);
      command.Parameters.AddWithValue("external_id", externalId);
      command.Parameters.AddWithValue("fingerprint", fingerprint);
      command.Parameters.AddWithValue(
         "entity_id",
         (object?)entityId ?? DBNull.Value
      );
      command.Parameters.AddWithValue("channel_id", channelId);
      command.Parameters.AddWithValue("channel_name", channelName);
      command.Parameters.AddWithValue("title", title);
      command.Parameters.AddWithValue("categories", categories);
      command.Parameters.AddWithValue("starts_at", startsAt);
      command.Parameters.AddWithValue("ends_at", endsAt);

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

   private static async Task DeleteBroadcastAsync(
      NpgsqlDataSource dataSource,
      Guid broadcastId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from broadcasts
         where id = @id
         """;
      command.Parameters.AddWithValue("id", broadcastId);
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

   private sealed class CapturingAiJobRunner : IAiJobRunner
   {
      public Task<Guid> QueueAsync(
         AiJobRequest request,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult(Guid.NewGuid());
      }

      public Task<AiJobResult> RunAsync(
         AiJobRequest request,
         CancellationToken cancellationToken
      )
      {
         throw new NotSupportedException();
      }
   }
}
