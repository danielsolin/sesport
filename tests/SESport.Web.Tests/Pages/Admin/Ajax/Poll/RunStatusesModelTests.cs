using Microsoft.AspNetCore.Mvc;

using Npgsql;

using SESport.Data.Repositories;
using SESport.Web.Pages.Admin.Ajax.Poll;

namespace SESport.Core.Tests.Pages.Admin.Ajax.Poll;

public sealed class RunStatusesModelTests
{
   [Fact]
   public async Task OnPostAsyncIncludesResultSummary()
   {
      var providerId = $"test-provider-{Guid.NewGuid():N}";
      var jobId = $"test-job-{Guid.NewGuid():N}";
      var promptId = Guid.NewGuid();
      var runId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AiRepository(dataSource);
      var model = new RunStatusesModel(repository);

      await InsertProviderAsync(dataSource, providerId);
      await InsertJobAsync(dataSource, jobId, providerId);
      await InsertPromptAsync(dataSource, promptId, jobId);
      await InsertRunAsync(
         dataSource,
         runId,
         jobId,
         promptId,
         providerId,
         statusId: "completed",
         outputText: """
            {
              "Participation": "Yes",
              "Participants": [
                { "Name": "Alice" },
                { "Name": "Bob" }
              ],
              "CheckedSources": []
            }
            """
      );

      try
      {
         var result = await model.OnPostAsync(
            [runId],
            CancellationToken.None
         );

         var jsonResult = Assert.IsType<JsonResult>(result);
         var payload = jsonResult.Value;
         Assert.NotNull(payload);
         var results = GetRequiredPropertyValue(payload, "results");
         var resultItem = Assert.Single(
            Assert.IsAssignableFrom<IEnumerable<object>>(results)
         );

         Assert.Equal(
            "2 participants",
            GetRequiredProperty<string>(resultItem, "resultSummary")
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
      string jobId
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
            1,
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
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertRunAsync(
      NpgsqlDataSource dataSource,
      Guid runId,
      string jobId,
      Guid promptId,
      string providerId,
      string statusId = "running",
      string? outputText = null
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
            'gpt',
            '{}'::jsonb,
            'Rendered',
            null,
            null,
            null,
            @output_text,
            null,
            now(),
            null,
            3.5,
            null,
            null,
            null,
            2,
            1234,
            'Worker-A'
         )
         """;
      command.Parameters.AddWithValue("id", runId);
      command.Parameters.AddWithValue("job_id", jobId);
      command.Parameters.AddWithValue("prompt_id", promptId);
      command.Parameters.AddWithValue("provider_id", providerId);
      command.Parameters.AddWithValue("status_id", statusId);
      command.Parameters.AddWithValue(
         "output_text",
         (object?)outputText ?? DBNull.Value
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
