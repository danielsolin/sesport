using System.Reflection;
using System.Text.Json;

using Npgsql;

using SESport.AI.Interfaces;
using SESport.Core.Broadcast;
using SESport.Core.Configuration;
using SESport.Data;
using SESport.Data.AI;
using SESport.Web.Services;

namespace SESport.Core.Tests.Services;

public sealed class BroadcastParticipationServiceTests
{
   [Fact]
   public void CreateParticipationInputJsonUsesDateOnlyMarker()
   {
      var broadcast = new BroadcastActivitySource(
         Guid.NewGuid(),
         "Channel",
         "Event title",
         "Broadcast description",
         ["Tennis"],
         DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
         DateTimeOffset.Parse("2026-06-15T14:00:00Z")
      );

      var method = typeof(BroadcastParticipationService).GetMethod(
         "CreateParticipationInputJson",
         BindingFlags.NonPublic | BindingFlags.Static
      )!;

      var json = (string)method.Invoke(null, [broadcast, "  - Candidate"])!;
      using var document = JsonDocument.Parse(json);
      var root = document.RootElement;

      Assert.True(root.TryGetProperty("date", out var date));
      Assert.False(root.TryGetProperty("date_time", out _));
      Assert.Equal("2026-06-15", date.GetString());
      Assert.Equal("Event title", root.GetProperty("event_name").GetString());
      Assert.Equal(
         "Broadcast description",
         root.GetProperty("description").GetString()
      );
      Assert.Equal("  - Candidate", root.GetProperty("candidates").GetString());
   }

   [Fact]
   public void GetParticipantDisplayItemsLinksExistingEntities()
   {
      var entityId = Guid.NewGuid();
      var entityIdsByName = new Dictionary<string, Guid>
      {
         [BroadcastEntityFilter.NormalizeName("Christoffer Brunnhagen")] =
            entityId
      };

      var result = BroadcastParticipationService.GetParticipantDisplayItems(
         ["Christoffer BRUNNHAGEN", "New PERSON"],
         entityIdsByName
      );

      Assert.Equal("Christoffer Brunnhagen", result[0].Name);
      Assert.Equal($"/Admin/Entities/Edit/{entityId}", result[0].EditUrl);
      Assert.Null(result[0].TemplateEntityId);
      Assert.Equal("New Person", result[1].Name);
      Assert.Null(result[1].EditUrl);
      Assert.Equal(entityId, result[1].TemplateEntityId);
   }

   [Fact]
   public async Task GetParticipationCheckResultsAsyncLinksPairEntities()
   {
      var organizationId = Guid.NewGuid();
      var pairId = Guid.NewGuid();
      var broadcastId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var pairName = $"Pair {Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var fixture = CreateService(dataSource);
      var jobId = "decide-swedish-participation";
      var context = await LoadParticipationJobContextAsync(
         dataSource,
         jobId
      );
      var runId = Guid.NewGuid();

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         pairId,
         pairName,
         TrackedEntityTypeIds.Pair,
         "football"
      );
      await InsertEntityLinkAsync(dataSource, pairId, organizationId);
      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         organizationId,
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
         context.PromptId,
         context.ProviderId,
         broadcastId.ToString(),
         pairName
      );

      try
      {
         var results = await fixture.Service.GetParticipationCheckResultsAsync(
            [broadcastId],
            CancellationToken.None
         );

         var result = Assert.Single(results);
         var check = Assert.Single(result.Checks);

         Assert.Equal(
            $"/Admin/Entities/Edit/{pairId}",
            check.Participants[0].EditUrl
         );
      }
      finally
      {
         await DeleteParticipationRunAsync(dataSource, runId);
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteLinksAsync(dataSource, pairId);
         await DeleteEntityAsync(dataSource, pairId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task QueueParticipationAsyncUsesEmptyCandidatesWithoutOrg()
   {
      var broadcastId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var fixture = CreateService(dataSource);

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

      try
      {
         await fixture.Service.QueueParticipationAsync(
            [broadcastId],
            CancellationToken.None
         );

         var request = Assert.Single(fixture.JobRunner.Requests);
         using var document = JsonDocument.Parse(request.InputPayloadJson);

         Assert.Equal(
            string.Empty,
            document.RootElement.GetProperty("candidates").GetString()
         );
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
      }
   }

   [Fact]
   public async Task QueueParticipationAsyncUsesLinkedOrgCandidates()
   {
      var broadcastId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var linkedPersonIds = Enumerable.Range(1, 6)
         .Select(_ => Guid.NewGuid())
         .ToList();
      var unrelatedPersonId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var fixture = CreateService(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      foreach(var linkedPersonId in linkedPersonIds)
      {
         await InsertRelatedEntityAsync(
            dataSource,
            linkedPersonId,
            $"Linked Person {linkedPersonId:N}",
            TrackedEntityTypeIds.Person,
            "football"
         );
         await InsertEntityLinkAsync(
            dataSource,
            linkedPersonId,
            organizationId
         );
      }
      await InsertRelatedEntityAsync(
         dataSource,
         unrelatedPersonId,
         $"Unrelated Person {unrelatedPersonId:N}",
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         organizationId,
         $"external-{Guid.NewGuid():N}",
         $"fingerprint-{Guid.NewGuid():N}",
         "channel-1",
         "Viaplay",
         "Broadcast title",
         ["Old", "Categories"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2)
      );

      try
      {
         await fixture.Service.QueueParticipationAsync(
            [broadcastId],
            CancellationToken.None
         );

         var request = Assert.Single(fixture.JobRunner.Requests);
         using var document = JsonDocument.Parse(request.InputPayloadJson);
         var candidates = document.RootElement.GetProperty("candidates")
            .GetString();

         var lines = candidates!.Split(
            Environment.NewLine,
            StringSplitOptions.RemoveEmptyEntries
         );

         Assert.Equal(5, lines.Length);
         Assert.DoesNotContain(
            $"Unrelated Person {unrelatedPersonId:N}",
            candidates
         );
         Assert.All(
            lines,
            line => Assert.StartsWith("  - Linked Person ", line)
         );
         Assert.DoesNotContain(
            $"Unrelated Person {unrelatedPersonId:N}",
            candidates
         );
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         foreach(var linkedPersonId in linkedPersonIds)
         {
            await DeleteLinksAsync(dataSource, linkedPersonId);
         }
         await DeleteEntityAsync(dataSource, unrelatedPersonId);
         foreach(var linkedPersonId in linkedPersonIds)
         {
            await DeleteEntityAsync(dataSource, linkedPersonId);
         }
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task GetParticipationCheckResultsAsyncUsesBroadcastOrg()
   {
      var organizationAId = Guid.NewGuid();
      var organizationBId = Guid.NewGuid();
      var personAId = Guid.NewGuid();
      var personBId = Guid.NewGuid();
      var broadcastAId = Guid.NewGuid();
      var broadcastBId = Guid.NewGuid();
      var sourceKeyA = $"test-source-{Guid.NewGuid():N}";
      var sourceKeyB = $"test-source-{Guid.NewGuid():N}";
      var sharedName = "Shared Participant";

      await using var dataSource = CreateDataSource();
      var fixture = CreateService(dataSource);
      var jobId = "decide-swedish-participation";
      var context = await LoadParticipationJobContextAsync(
         dataSource,
         jobId
      );
      var runAId = Guid.NewGuid();
      var runBId = Guid.NewGuid();

      await InsertRelatedEntityAsync(
         dataSource,
         organizationAId,
         $"Organization A {organizationAId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         organizationBId,
         $"Organization B {organizationBId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personAId,
         sharedName,
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personBId,
         sharedName,
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertEntityLinkAsync(dataSource, personAId, organizationAId);
      await InsertEntityLinkAsync(dataSource, personBId, organizationBId);
      await InsertBroadcastAsync(
         dataSource,
         broadcastAId,
         sourceKeyA,
         organizationAId,
         $"external-{Guid.NewGuid():N}",
         $"fingerprint-{Guid.NewGuid():N}",
         "channel-1",
         "Viaplay",
         "Broadcast A",
         ["Old", "Categories"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2)
      );
      await InsertBroadcastAsync(
         dataSource,
         broadcastBId,
         sourceKeyB,
         organizationBId,
         $"external-{Guid.NewGuid():N}",
         $"fingerprint-{Guid.NewGuid():N}",
         "channel-2",
         "Viaplay",
         "Broadcast B",
         ["Old", "Categories"],
         DateTimeOffset.UtcNow.AddMinutes(1),
         DateTimeOffset.UtcNow.AddHours(2).AddMinutes(1)
      );
      await InsertRunAsync(
         dataSource,
         runAId,
         jobId,
         context.PromptId,
         context.ProviderId,
         broadcastAId.ToString(),
         sharedName
      );
      await InsertRunAsync(
         dataSource,
         runBId,
         jobId,
         context.PromptId,
         context.ProviderId,
         broadcastBId.ToString(),
         sharedName
      );

      try
      {
         var results = await fixture.Service.GetParticipationCheckResultsAsync(
            [broadcastAId, broadcastBId],
            CancellationToken.None
         );

         var resultsById = results.ToDictionary(result => result.Id);
         var broadcastAResult = Assert.Single(
            resultsById[broadcastAId].Checks
         );
         var broadcastBResult = Assert.Single(
            resultsById[broadcastBId].Checks
         );

         Assert.Equal(
            $"/Admin/Entities/Edit/{personAId}",
            broadcastAResult.Participants[0].EditUrl
         );
         Assert.Equal(
            $"/Admin/Entities/Edit/{personBId}",
            broadcastBResult.Participants[0].EditUrl
         );
      }
      finally
      {
         await DeleteParticipationRunsAsync(dataSource, runAId, runBId);
         await DeleteBroadcastAsync(dataSource, broadcastAId);
         await DeleteBroadcastAsync(dataSource, broadcastBId);
         await DeleteLinksAsync(dataSource, personAId);
         await DeleteLinksAsync(dataSource, personBId);
         await DeleteEntityAsync(dataSource, personAId);
         await DeleteEntityAsync(dataSource, personBId);
         await DeleteEntityAsync(dataSource, organizationAId);
         await DeleteEntityAsync(dataSource, organizationBId);
      }
   }

   private static NpgsqlDataSource CreateDataSource()
   {
      var connectionString = PostgresConnectionStrings.ResolveDefault();

      return new NpgsqlDataSourceBuilder(connectionString).Build();
   }

   private static ServiceFixture CreateService(NpgsqlDataSource dataSource)
   {
      var jobRunner = new CapturingAiJobRunner();

      return new ServiceFixture(
         new BroadcastParticipationService(
            new ActivityRepository(dataSource),
            new AiRepository(dataSource),
            new AdminRepository(dataSource),
            new AdminBroadcastRepository(dataSource),
            jobRunner
         ),
         jobRunner
      );
   }

   private sealed record ServiceFixture(
      BroadcastParticipationService Service,
      CapturingAiJobRunner JobRunner
   );

   private sealed class CapturingAiJobRunner : IAiJobRunner
   {
      public List<AiJobRequest> Requests { get; } = [];

      public Task<Guid> QueueAsync(
         AiJobRequest request,
         CancellationToken cancellationToken
      )
      {
         Requests.Add(request);
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
            'se',
            'NationalityOrSportingIdentity',
            'Test coverage',
            'review',
            'short_term'
         )
         """;
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("canonical_name", entityName);
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

   private static async Task InsertRunAsync(
      NpgsqlDataSource dataSource,
      Guid runId,
      string jobId,
      Guid promptId,
      string providerId,
      string correlationId,
      string participantName
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
            'completed',
            @correlation_id,
            'gpt',
            '{}'::jsonb,
            'Rendered',
            null,
            null,
            null,
            @output_text::jsonb,
            null,
            now(),
            now(),
            null,
            null,
            null,
            null,
            0,
            0,
            null
         )
         """;
      command.Parameters.AddWithValue("id", runId);
      command.Parameters.AddWithValue("job_id", jobId);
      command.Parameters.AddWithValue("prompt_id", promptId);
      command.Parameters.AddWithValue("provider_id", providerId);
      command.Parameters.AddWithValue("correlation_id", correlationId);
      command.Parameters.AddWithValue(
         "output_text",
         $$"""
         {
            "Participation": "Yes",
            "Participants": [
               {
                  "Name": "{{participantName}}",
                  "Sources": [
                     {
                        "Url": "https://example.test/participant",
                        "EvidenceType": "ParticipantMention"
                     }
                  ]
               }
            ],
            "CheckedSources": []
         }
         """
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteParticipationRunsAsync(
      NpgsqlDataSource dataSource,
      Guid runAId,
      Guid runBId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from ai_job_runs
         where id = @run_a_id
            or id = @run_b_id
         """;
      command.Parameters.AddWithValue("run_a_id", runAId);
      command.Parameters.AddWithValue("run_b_id", runBId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteParticipationRunAsync(
      NpgsqlDataSource dataSource,
      Guid runId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from ai_job_runs
         where id = @run_id
         """;
      command.Parameters.AddWithValue("run_id", runId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task<JobContext> LoadParticipationJobContextAsync(
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

   private sealed record JobContext(
      string ProviderId,
      Guid PromptId
   );

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

}
