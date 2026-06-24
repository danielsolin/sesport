using System.Reflection;
using System.Text.Json;

using Npgsql;

using SESport.AI.Interfaces;
using SESport.AI.Models;
using SESport.AI.Persistence;
using SESport.Core.Broadcast;
using SESport.Core.Configuration;
using SESport.Core.Domain;
using SESport.Data;
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
         null,
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
      Assert.Equal("  - Candidate", root.GetProperty("candidates").GetString());
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
         Assert.All(lines, line => Assert.StartsWith("  - Linked Person ", line));
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
            new BroadcastRepository(dataSource),
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
