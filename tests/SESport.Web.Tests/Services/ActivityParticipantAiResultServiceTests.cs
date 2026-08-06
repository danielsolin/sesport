using Microsoft.Extensions.Logging.Abstractions;

using Npgsql;

using SESport.Core.Configuration;
using SESport.Core.Sources;
using SESport.Data.Models;
using SESport.Data.Repositories;

namespace SESport.Web.Tests.Services;

public sealed class ActivityParticipantAiResultServiceTests
{
   private static readonly DateOnly DistantActivityDate =
      new(2199, 12, 1);

   [Fact]
   public async Task TryApplyRunAsyncStoresStructuredParticipantValues()
   {
      var organizationId = Guid.NewGuid();
      var firstPersonId = Guid.NewGuid();
      var secondPersonId = Guid.NewGuid();
      var thirdPersonId = Guid.NewGuid();
      var activityId = Guid.NewGuid();
      var runId = Guid.NewGuid();
      var firstPersonName = $"First {Guid.NewGuid():N}";
      var secondPersonName = $"Second {Guid.NewGuid():N}";
      var thirdPersonName = $"Third {Guid.NewGuid():N}";
      var sourceUrl = "https://example.test/event";

      await using var dataSource = CreateDataSource();
      var activityRepository = new ActivityRepository(dataSource);
      var aiRepository = new AiRepository(dataSource);
      var resultRepository = new ActivityParticipantAiResultRepository(
         dataSource
      );
      var service = new ActivityParticipantAiResultService(
         aiRepository,
         activityRepository,
         resultRepository,
         NullLogger<ActivityParticipantAiResultService>.Instance
      );
      var jobContext = await LoadJobContextAsync(
         dataSource,
         AiJobIds.FindParticipantsStart
      );

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         firstPersonId,
         firstPersonName,
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         secondPersonId,
         secondPersonName,
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         thirdPersonId,
         thirdPersonName,
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertEntityLinkAsync(dataSource, firstPersonId, organizationId);
      await InsertEntityLinkAsync(dataSource, secondPersonId, organizationId);
      await InsertEntityLinkAsync(dataSource, thirdPersonId, organizationId);
      activityId = await activityRepository.SaveAsync(
         new ActivityEditModel
         {
            Title = "Result test",
            ActivityType = ActivityType.Match.ToString(),
            SportId = "football",
            ActivityDate = DistantActivityDate,
            LinkedEntityIds = [firstPersonId, secondPersonId, thirdPersonId],
            OrganizationEntityId = organizationId
         },
         CancellationToken.None
      );
      await InsertRunAsync(
         dataSource,
         runId,
         AiJobIds.FindParticipantsStart,
         jobContext.PromptId,
         jobContext.ProviderId,
         activityId,
         firstPersonName,
         secondPersonName,
         thirdPersonName,
         sourceUrl,
         firstPersonId,
         secondPersonId,
         thirdPersonId
      );

      try
      {
         var applied = await service.TryApplyRunAsync(
            runId,
            CancellationToken.None
         );

         Assert.True(applied);

         await using var connection = await dataSource.OpenConnectionAsync();
         var loadedActivity = await activityRepository.GetForEditAsync(
            activityId,
            CancellationToken.None
         );
         Assert.NotNull(loadedActivity);
         Assert.Single(loadedActivity!.Sources);
         Assert.Equal(
            sourceUrl,
            loadedActivity.Sources[0].Url
         );
         Assert.Equal(
            SourceKinds.ParticipantStartEvidence,
            loadedActivity.Sources[0].Kind
         );

         await using(var sourceCountCommand = connection.CreateCommand())
         {
            sourceCountCommand.CommandText = """
               select count(*)
               from sources
               where correlation_type = @correlation_type
                  and correlation_id = @correlation_id
                  and kind = @kind
               """;
            sourceCountCommand.Parameters.AddWithValue(
               "correlation_type",
               SourceCorrelationTypes.Activity
            );
            sourceCountCommand.Parameters.AddWithValue(
               "correlation_id",
               activityId.ToString()
            );
            sourceCountCommand.Parameters.AddWithValue(
               "kind",
               SourceKinds.ParticipantStartEvidence
            );

            Assert.Equal(
               1L,
               (long)(await sourceCountCommand.ExecuteScalarAsync())!
            );
         }

         var resultSets = await resultRepository.GetForActivityAsync(
            activityId,
            CancellationToken.None
         );
         var resultSet = Assert.Single(resultSets);

         Assert.Equal(AiJobIds.FindParticipantsStart, resultSet.JobId);
         Assert.NotEmpty(resultSet.JobLabel);
         Assert.Equal(runId, resultSet.RunId);
         Assert.Equal("completed", resultSet.RunStatusId);
         var checkedSource = Assert.Single(resultSet.CheckedSources);
         Assert.Equal(sourceUrl, checkedSource.Url);
         Assert.Equal(2, resultSet.Values.Count);
         Assert.Contains(
            resultSet.Values,
            value =>
               value.EntityName == firstPersonName &&
               value.FieldKey == "start_time" &&
               value.ValueText == "12:30" &&
               value.ValueJson == "\"12:30\"" &&
               value.Sources.Count == 1 &&
               value.Sources[0].Url == sourceUrl
         );
         Assert.Contains(
            resultSet.Values,
            value =>
               value.EntityName == thirdPersonName &&
               value.FieldKey == "start_time" &&
               value.ValueText == "14:20" &&
               value.ValueJson == "\"14:20\"" &&
               value.Sources.Count == 1 &&
               value.Sources[0].Url == sourceUrl
         );

         await using(var countCommand = connection.CreateCommand())
         {
            countCommand.CommandText = """
               select count(*), count(distinct source_id)
               from activity_participant_ai_results
               where activity_id = @activity_id
                  and job_id = @job_id
               """;
            countCommand.Parameters.AddWithValue("activity_id", activityId);
            countCommand.Parameters.AddWithValue(
               "job_id",
               AiJobIds.FindParticipantsStart
            );

            await using var reader = await countCommand.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(2L, reader.GetInt64(0));
            Assert.Equal(1L, reader.GetInt64(1));
         }

         await using(var valueCommand = connection.CreateCommand())
         {
            valueCommand.CommandText = """
               select e.canonical_name, r.field_key, r.value_text,
                  r.value_json::text, src.url, src.kind, r.sort_order
               from activity_participant_ai_results r
               join entities e on e.id = r.entity_id
               join sources src on src.id = r.source_id
               where r.activity_id = @activity_id
                  and r.job_id = @job_id
               order by e.canonical_name, r.field_key, r.sort_order, r.id
               """;
            valueCommand.Parameters.AddWithValue("activity_id", activityId);
            valueCommand.Parameters.AddWithValue(
               "job_id",
               AiJobIds.FindParticipantsStart
            );

            await using var reader = await valueCommand.ExecuteReaderAsync();
            var rows = new List<(string Name, string FieldKey, string? Text,
               string Json, string Url, string Kind, int SortOrder)>();

            while(await reader.ReadAsync())
            {
               rows.Add(
                  (
                     reader.GetString(0),
                     reader.GetString(1),
                     reader.IsDBNull(2) ? null : reader.GetString(2),
                     reader.GetString(3),
                     reader.GetString(4),
                     reader.GetString(5),
                     reader.GetInt32(6)
                  )
               );
            }

            Assert.Equal(2, rows.Count);
            Assert.All(
               rows,
               row =>
               {
                  Assert.Equal(
                     sourceUrl,
                     row.Url
                  );
                  Assert.Equal(
                     SourceKinds.ParticipantStartEvidence,
                     row.Kind
                  );
               }
            );
            Assert.Contains(
               rows,
               row =>
                  row.Name == firstPersonName &&
                  row.FieldKey == "start_time" &&
                  row.Text == "12:30" &&
                  row.Json == "\"12:30\"" &&
                  row.Url == sourceUrl &&
                  row.Kind == SourceKinds.ParticipantStartEvidence &&
                  row.SortOrder == 0
            );
            Assert.Contains(
               rows,
               row =>
                  row.Name == thirdPersonName &&
                  row.FieldKey == "start_time" &&
                  row.Text == "14:20" &&
                  row.Json == "\"14:20\"" &&
                  row.Url == sourceUrl &&
                  row.Kind == SourceKinds.ParticipantStartEvidence &&
                  row.SortOrder == 1
            );
         }

         await using(var applicationCommand = connection.CreateCommand())
         {
            applicationCommand.CommandText = """
               select count(*)
               from ai_job_run_applications
               where run_id = @run_id
                  and target_type = @target_type
                  and target_id = @target_id
               """;
            applicationCommand.Parameters.AddWithValue("run_id", runId);
            applicationCommand.Parameters.AddWithValue(
               "target_type",
               AiJobRunApplicationTargetTypes.Activity
            );
            applicationCommand.Parameters.AddWithValue(
               "target_id",
               activityId.ToString()
            );

            Assert.Equal(
               1L,
               (long)(await applicationCommand.ExecuteScalarAsync())!
            );
         }

         var firstValue = Assert.Single(
            resultSet.Values,
            value => value.EntityId == firstPersonId
         );
         Assert.NotEqual(Guid.Empty, firstValue.Id);
         Assert.True(
            await resultRepository.UpdateValueAsync(
               firstValue.Id,
               "14:20",
               CancellationToken.None
            )
         );
         var updatedResultSet = Assert.Single(
            await resultRepository.GetForActivityAsync(
               activityId,
               CancellationToken.None
            )
         );
         var updatedValue = Assert.Single(
            updatedResultSet.Values,
            value => value.EntityId == firstPersonId
         );
         Assert.Equal("14:20", updatedValue.ValueText);
         Assert.Equal("\"14:20\"", updatedValue.ValueJson);
      }
      finally
      {
         await activityRepository.DeleteAsync(
            activityId,
            CancellationToken.None
         );
         await DeleteRunAsync(dataSource, runId);
         await DeleteLinksAsync(dataSource, firstPersonId);
         await DeleteLinksAsync(dataSource, secondPersonId);
         await DeleteLinksAsync(dataSource, thirdPersonId);
         await DeleteEntityAsync(dataSource, firstPersonId);
         await DeleteEntityAsync(dataSource, secondPersonId);
         await DeleteEntityAsync(dataSource, thirdPersonId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   private static async Task InsertRunAsync(
      NpgsqlDataSource dataSource,
      Guid runId,
      string jobId,
      Guid promptId,
      string providerId,
      Guid activityId,
      string firstPersonName,
      string secondPersonName,
      string thirdPersonName,
      string sourceUrl,
      Guid firstPersonId,
      Guid secondPersonId,
      Guid thirdPersonId
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
            'completed',
            @correlation_id,
            'gpt',
            @input_payload::jsonb,
            'Rendered',
            null,
            null,
            null,
            @output_text,
            null,
            now(),
            now(),
            null,
            null,
            null,
            null,
            0,
            0,
            null,
            @max_output_tokens
         )
         """;
      command.Parameters.AddWithValue("id", runId);
      command.Parameters.AddWithValue("job_id", jobId);
      command.Parameters.AddWithValue("prompt_id", promptId);
      command.Parameters.AddWithValue("provider_id", providerId);
      command.Parameters.AddWithValue("correlation_id", activityId.ToString());
      command.Parameters.AddWithValue(
         "max_output_tokens",
         AiDefaults.DefaultMaxOutputTokens
      );
      command.Parameters.AddWithValue(
         "input_payload",
         $$"""
         {
            "participants": [
               "{{firstPersonName}}",
               "{{secondPersonName}}",
               "{{thirdPersonName}}"
            ],
            "participant_entities": [
               {
                  "id": "{{firstPersonId}}",
                  "name": "{{firstPersonName}}"
               },
               {
                  "id": "{{secondPersonId}}",
                  "name": "{{secondPersonName}}"
               },
               {
                  "id": "{{thirdPersonId}}",
                  "name": "{{thirdPersonName}}"
               }
            ]
         }
         """
      );
      command.Parameters.AddWithValue(
         "output_text",
         $$"""
         {
            "participants": [
               {
                  "name": "{{firstPersonName}}",
                  "start_time": "12:30",
                  "source_url": "{{sourceUrl}}"
               },
               {
                  "name": "{{thirdPersonName}}",
                  "start_time": "14:20",
                  "source_url": "{{sourceUrl}}"
               },
               {
                  "name": "{{secondPersonName}}",
                  "start_time": null,
                  "source_url": null
               }
            ]
         }
         """
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

   private static async Task InsertRelatedEntityAsync(
      NpgsqlDataSource dataSource,
      Guid entityId,
      string entityName,
      string entityTypeId,
      string sportId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into entities (
            id,
            canonical_name,
            entity_type_id,
            sport_id,
            country_id,
            country_relevance_kind_id,
            country_relevance_reason,
            watch_priority_id,
            expected_stability_id
         )
         values (
            @id,
            @canonical_name,
            @entity_type_id,
            @sport_id,
            @country_id,
            'NationalityOrSportingIdentity',
            'Test coverage',
            'tier_3',
            'short_term'
         )
         """;
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("canonical_name", entityName);
      command.Parameters.AddWithValue("country_id", PrimaryCountry.Id);
      command.Parameters.AddWithValue("entity_type_id", entityTypeId);
      command.Parameters.AddWithValue("sport_id", sportId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertEntityLinkAsync(
      NpgsqlDataSource dataSource,
      Guid sourceEntityId,
      Guid targetEntityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into entity_to_entity_links (
            id,
            source_entity_id,
            target_entity_id
         )
         values (
            @id,
            @source_entity_id,
            @target_entity_id
         )
         """;
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("source_entity_id", sourceEntityId);
      command.Parameters.AddWithValue("target_entity_id", targetEntityId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteLinksAsync(
      NpgsqlDataSource dataSource,
      Guid entityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from entity_to_entity_links
         where source_entity_id = @id
            or target_entity_id = @id
         """;
      command.Parameters.AddWithValue("id", entityId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteEntityAsync(
      NpgsqlDataSource dataSource,
      Guid entityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from entities
         where id = @id
         """;
      command.Parameters.AddWithValue("id", entityId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task<JobContext> LoadJobContextAsync(
      NpgsqlDataSource dataSource,
      string jobId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         select
            j.provider_id,
            coalesce(j.active_prompt_id, p.id)
         from ai_jobs j
         left join ai_job_prompts p
            on p.job_id = j.id
               and p.enabled = true
         where j.id = @job_id
         order by p.version desc nulls last
         limit 1
         """;
      command.Parameters.AddWithValue("job_id", jobId);

      await using var reader = await command.ExecuteReaderAsync();

      if(!await reader.ReadAsync())
      {
         throw new InvalidOperationException(
            $"Missing AI job definition: {jobId}"
         );
      }

      return new JobContext(
         reader.GetString(0),
         reader.GetGuid(1)
      );
   }

   private sealed record JobContext(
      string ProviderId,
      Guid PromptId
   );
}
