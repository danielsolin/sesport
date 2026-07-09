using Npgsql;

using SESport.Core.Configuration;
using SESport.Data;

namespace SESport.Core.Tests.Data;

public sealed class AdminRepositoryTests
{
   [Fact]
   public async Task GetPersonEntityNameOptionsAsyncReturnsInsertedPerson()
   {
      var entityId = Guid.NewGuid();
      var entityName = $"Test Person {entityId:N}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      await InsertEntityAsync(dataSource, entityId, entityName);

      try
      {
         var options = await repository.GetPersonEntityNameOptionsAsync(
            CancellationToken.None
         );

         Assert.Contains(
            options,
            option =>
               option.Id == entityId &&
               option.Name == entityName
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, entityId);
      }
   }

   [Fact]
   public async Task GetPersonEntityNameOptionsAsyncReturnsAliasName()
   {
      var entityId = Guid.NewGuid();
      var entityName = $"Test Person {entityId:N}";
      var aliasName = $"Alias Person {entityId:N}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      await InsertEntityAsync(dataSource, entityId, entityName, aliasName);

      try
      {
         var options = await repository.GetPersonEntityNameOptionsAsync(
            CancellationToken.None
         );

         Assert.Contains(
            options,
            option =>
               option.Id == entityId &&
               option.Name == aliasName
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, entityId);
      }
   }

   [Fact]
   public async Task GetPersonEntityNameOptionsAsyncReturnsInsertedPair()
   {
      var entityId = Guid.NewGuid();
      var entityName = $"Test Pair {entityId:N}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         entityId,
         entityName,
         TrackedEntityTypeIds.Pair,
         "football"
      );

      try
      {
         var options = await repository.GetPersonEntityNameOptionsAsync(
            CancellationToken.None
         );

         Assert.Contains(
            options,
            option =>
               option.Id == entityId &&
               option.Name == entityName
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, entityId);
      }
   }

   [Fact]
   public async Task SearchEntitiesAsyncFiltersByEntityType()
   {
      var personId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var token = Guid.NewGuid().ToString("N");
      var personName = $"Type Filter Person {token}";
      var organizationName = $"Type Filter Organization {token}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         personName,
         TrackedEntityTypeIds.Person,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         organizationName,
         TrackedEntityTypeIds.Organization,
         "football"
      );

      try
      {
         var results = await repository.SearchEntitiesAsync(
            token,
            CancellationToken.None,
            false,
            [TrackedEntityTypeIds.Person]
         );

         Assert.Contains(results, entity => entity.Id == personId);
         Assert.DoesNotContain(
            results,
            entity => entity.Id == organizationId
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task GetEntityActivitiesAsyncReturnsLinkedActivities()
   {
      var entityId = Guid.NewGuid();
      var otherEntityId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var olderActivityId = Guid.NewGuid();
      var newerActivityId = Guid.NewGuid();
      var otherActivityId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      await InsertEntityAsync(
         dataSource,
         entityId,
         $"Activity Entity {entityId:N}"
      );
      await InsertEntityAsync(
         dataSource,
         otherEntityId,
         $"Other Activity Entity {otherEntityId:N}"
      );
      await InsertEntityAsync(
         dataSource,
         organizationId,
         "Canonical Activity Org",
         "Alias Activity Org"
      );
      await InsertActivityAsync(
         dataSource,
         olderActivityId,
         "Older Linked Activity",
         new DateOnly(2026, 7, 1),
         new TimeOnly(12, 0),
         "Draft"
      );
      await InsertActivityAsync(
         dataSource,
         newerActivityId,
         "Newer Linked Activity",
         new DateOnly(2026, 7, 2),
         new TimeOnly(13, 30),
         "Published"
      );
      await InsertActivityAsync(
         dataSource,
         otherActivityId,
         "Other Linked Activity",
         new DateOnly(2026, 7, 3),
         new TimeOnly(14, 0),
         "Published"
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         olderActivityId,
         entityId
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         newerActivityId,
         entityId,
         organizationId
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         otherActivityId,
         otherEntityId
      );

      try
      {
         var activities = await repository.GetEntityActivitiesAsync(
            entityId,
            CancellationToken.None
         );

         Assert.Equal([newerActivityId, olderActivityId], activities
            .Select(activity => activity.Id)
            .ToArray());
         Assert.Equal("2026-07-02 13:30", activities[0].DateTimeText);
         Assert.Equal("Football", activities[0].Sport);
         Assert.Equal("Match", activities[0].ActivityType);
         Assert.Equal("Published", activities[0].PublicationStatus);
         Assert.Equal("Canonical Activity Org", activities[0].Organization);
      }
      finally
      {
         await DeleteActivityEntityLinksAsync(dataSource, olderActivityId);
         await DeleteActivityEntityLinksAsync(dataSource, newerActivityId);
         await DeleteActivityEntityLinksAsync(dataSource, otherActivityId);
         await DeleteActivityAsync(dataSource, olderActivityId);
         await DeleteActivityAsync(dataSource, newerActivityId);
         await DeleteActivityAsync(dataSource, otherActivityId);
         await DeleteEntityAsync(dataSource, entityId);
         await DeleteEntityAsync(dataSource, otherEntityId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task GetPersonEntityNameOptionsAsyncUsesOrganizationScope()
   {
      var organizationAId = Guid.NewGuid();
      var organizationBId = Guid.NewGuid();
      var personAId = Guid.NewGuid();
      var personBId = Guid.NewGuid();
      var personName = $"Shared Person {Guid.NewGuid():N}";
      var aliasName = $"Alias Person {Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         organizationAId,
         "Organization A",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         organizationBId,
         "Organization B",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertEntityAsync(
         dataSource,
         personAId,
         personName,
         aliasName
      );
      await InsertEntityAsync(
         dataSource,
         personBId,
         personName
      );
      var pairId = Guid.NewGuid();
      var pairName = $"Shared Pair {Guid.NewGuid():N}";
      await InsertRelatedEntityAsync(
         dataSource,
         pairId,
         pairName,
         TrackedEntityTypeIds.Pair,
         "football"
      );
      await InsertLinkAsync(dataSource, personAId, organizationAId);
      await InsertLinkAsync(dataSource, personBId, organizationBId);
      await InsertLinkAsync(dataSource, pairId, organizationAId);

      try
      {
         var options = await repository.GetPersonEntityNameOptionsAsync(
            organizationAId,
            CancellationToken.None
         );

         Assert.Contains(
            options,
            option =>
               option.Id == personAId &&
               option.Name == personName
         );
         Assert.Contains(
            options,
            option =>
               option.Id == personAId &&
               option.Name == aliasName
         );
         Assert.Contains(
            options,
            option =>
               option.Id == pairId &&
               option.Name == pairName
         );
         Assert.DoesNotContain(
            options,
            option => option.Id == personBId
         );
      }
      finally
      {
         await DeleteLinksAsync(dataSource, pairId);
         await DeleteLinksAsync(dataSource, personAId);
         await DeleteLinksAsync(dataSource, personBId);
         await DeleteEntityAsync(dataSource, personAId);
         await DeleteEntityAsync(dataSource, personBId);
         await DeleteEntityAsync(dataSource, pairId);
         await DeleteEntityAsync(dataSource, organizationAId);
         await DeleteEntityAsync(dataSource, organizationBId);
      }
   }

   [Fact]
   public async Task GetEntityCloneTemplateAsyncKeepsOnlyNationalTeams()
   {
      var templateId = Guid.NewGuid();
      var nationalTeamId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var templateName = $"Template Person {templateId:N}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      await InsertEntityAsync(dataSource, templateId, templateName);
      await InsertRelatedEntityAsync(
         dataSource,
         nationalTeamId,
         "National Team",
         TrackedEntityTypeIds.NationalTeam,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         "Organization",
         "Organization",
         "football"
      );
      await InsertLinkAsync(dataSource, templateId, nationalTeamId);
      await InsertLinkAsync(dataSource, templateId, organizationId);

      try
      {
         var template = await repository.GetEntityCloneTemplateAsync(
            templateId,
            CancellationToken.None
         );

         Assert.NotNull(template);
         Assert.Equal(TrackedEntityTypeIds.Person, template!.EntityTypeId);
         Assert.Contains(nationalTeamId, template.LinkedEntityIds);
         Assert.Contains(organizationId, template.LinkedEntityIds);
         Assert.Equal(2, template.LinkedEntityIds.Count);
      }
      finally
      {
         await DeleteLinksAsync(dataSource, templateId);
         await DeleteEntityAsync(dataSource, organizationId);
         await DeleteEntityAsync(dataSource, nationalTeamId);
         await DeleteEntityAsync(dataSource, templateId);
      }
   }

   [Fact]
   public async Task SearchBroadcastOrganizationLinkOptionsAsyncReturnsMatches()
   {
      var organizationId = Guid.NewGuid();
      var nationalTeamId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var queryToken = Guid.NewGuid().ToString("N")[..8];

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {queryToken}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         nationalTeamId,
         $"National Team {queryToken}",
         TrackedEntityTypeIds.NationalTeam,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         $"Person {queryToken}",
         TrackedEntityTypeIds.Person,
         "football"
      );

      try
      {
         var options = await repository
            .SearchBroadcastOrganizationLinkOptionsAsync(
               queryToken,
               CancellationToken.None
            );

         Assert.Contains(
            options,
            option =>
               option.Id == organizationId &&
               option.Name == $"Organization {queryToken}"
         );
         Assert.Contains(
            options,
            option =>
               option.Id == nationalTeamId &&
               option.Name == $"National Team {queryToken}"
         );
         Assert.DoesNotContain(
            options,
            option => option.Id == personId
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, nationalTeamId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public void GetConfigNavigationGroupsPlacesActivityProposalsInLegacy()
   {
      using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      var groups = repository.GetConfigNavigationGroups();
      var legacyGroup = Assert.Single(
         groups,
         group => string.Equals(
            group.Title,
            "Legacy",
            StringComparison.OrdinalIgnoreCase
         )
      );

      var legacyItem = Assert.Single(legacyGroup.Items);
      Assert.Equal("Activity Proposals", legacyItem.Title);
      Assert.Equal("/Admin/Activities/Proposals", legacyItem.Href);
      Assert.Equal("Legacy", groups[^1].Title);
   }

   [Fact]
   public async Task SaveEntityAsyncPersistsAliasName()
   {
      var entityKey = Guid.NewGuid();
      var entityName = $"Alias Entity {entityKey:N}";
      var aliasName = $"Alias {entityKey:N}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      var model = new EntityEditModel
      {
         Id = null,
         CanonicalName = entityName,
         AliasName = aliasName,
         EntityTypeId = TrackedEntityTypeIds.Person,
         SportId = "football",
         CountryId = "se",
         CountryRelevanceKindId =
            "NationalityOrSportingIdentity",
         CountryRelevanceReason = "Test coverage",
         WatchPriorityId = "review",
         ExpectedStabilityId = "short_term",
         PersonGenderId = PersonGenderIds.Female
      };

      try
      {
         await repository.SaveEntityAsync(model, CancellationToken.None);

         var loaded = await repository.GetEntityForEditAsync(
            model.Id!.Value,
            CancellationToken.None
         );

         Assert.NotNull(loaded);
         Assert.Equal(aliasName, loaded!.AliasName);
      }
      finally
      {
         if(model.Id is not null)
         {
            await DeleteEntityAsync(dataSource, model.Id.Value);
         }
      }
   }

   [Fact]
   public async Task SaveBroadcastIgnoreRuleAsyncPersistsRule()
   {
      var ruleKind = "channel_name";
      var ruleValue = $"Test Ignore {Guid.NewGuid():N}";
      var sourceKey = "iptv-epg-se";

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      var model = new BroadcastIgnoreRuleEditModel
      {
         Kind = ruleKind,
         Value = ruleValue,
         SourceKey = sourceKey,
         Reason = "Test coverage",
         IsActive = true
      };

      try
      {
         await repository.SaveBroadcastIgnoreRuleAsync(
            model,
            CancellationToken.None
         );

         var rules = await repository.GetBroadcastIgnoreRulesAsync(
            CancellationToken.None
         );

         Assert.Contains(
            rules,
            rule =>
               rule.Kind == ruleKind &&
               rule.Value == ruleValue &&
               rule.SourceKey == sourceKey &&
               rule.Reason == "Test coverage" &&
               rule.IsActive
         );

         var loaded = await repository.GetBroadcastIgnoreRuleForEditAsync(
            ruleKind,
            ruleValue,
            sourceKey,
            CancellationToken.None
         );

         Assert.NotNull(loaded);
         Assert.Equal(ruleKind, loaded!.Kind);
         Assert.Equal(ruleValue, loaded.Value);
         Assert.Equal(sourceKey, loaded.SourceKey);
         Assert.Equal("Test coverage", loaded.Reason);
         Assert.True(loaded.IsActive);
      }
      finally
      {
         await repository.DeleteBroadcastIgnoreRuleAsync(
            ruleKind,
            ruleValue,
            sourceKey,
            CancellationToken.None
         );
      }
   }

   [Fact]
   public async Task GetEntityMergePreviewReturnsCountsAndLinkActions()
   {
      var sourceId = Guid.NewGuid();
      var targetId = Guid.NewGuid();
      var linkedId = Guid.NewGuid();
      var sourceActivityId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      await InsertEntityAsync(dataSource, sourceId, $"Merge Source {sourceId}");
      await InsertEntityAsync(dataSource, targetId, $"Merge Target {targetId}");
      await InsertRelatedEntityAsync(
         dataSource,
         linkedId,
         $"Merge Linked {linkedId}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertActivityAsync(
         dataSource,
         sourceActivityId,
         "Merge Preview Activity",
         new DateOnly(2026, 7, 8),
         new TimeOnly(12, 0),
         "Draft"
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         sourceActivityId,
         sourceId
      );
      await InsertLinkAsync(dataSource, sourceId, linkedId);
      await InsertLinkAsync(dataSource, linkedId, targetId);

      try
      {
         var preview = await repository.GetEntityMergePreviewAsync(
            sourceId,
            targetId,
            CancellationToken.None
         );

         Assert.NotNull(preview);
         Assert.Equal(sourceId, preview.Source.Id);
         Assert.Equal(targetId, preview.Target.Id);
         Assert.Contains(
            preview.ReferenceCounts,
            count => count.Label == "Activity participants" &&
               count.Count == 1
         );
         Assert.Contains(
            preview.LinkPreviews,
            link => link.RelatedEntityName.StartsWith(
               "Merge Linked",
               StringComparison.Ordinal
            ) && link.Action == "Drop duplicate"
         );
      }
      finally
      {
         await DeleteActivityEntityLinksAsync(dataSource, sourceActivityId);
         await DeleteActivityAsync(dataSource, sourceActivityId);
         await DeleteLinksAsync(dataSource, sourceId);
         await DeleteLinksAsync(dataSource, targetId);
         await DeleteLinksAsync(dataSource, linkedId);
         await DeleteEntityAsync(dataSource, sourceId);
         await DeleteEntityAsync(dataSource, targetId);
         await DeleteEntityAsync(dataSource, linkedId);
      }
   }

   [Fact]
   public async Task MergeEntityMovesReferencesAndDeletesSource()
   {
      var sourceId = Guid.NewGuid();
      var targetId = Guid.NewGuid();
      var linkedId = Guid.NewGuid();
      var sourceActivityId = Guid.NewGuid();
      var targetActivityId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);

      await InsertEntityAsync(dataSource, sourceId, $"Merge Source {sourceId}");
      await InsertEntityAsync(dataSource, targetId, $"Merge Target {targetId}");
      await InsertRelatedEntityAsync(
         dataSource,
         linkedId,
         $"Merge Linked {linkedId}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertActivityAsync(
         dataSource,
         sourceActivityId,
         "Merge Source Activity",
         new DateOnly(2026, 7, 8),
         new TimeOnly(12, 0),
         "Draft"
      );
      await InsertActivityAsync(
         dataSource,
         targetActivityId,
         "Merge Target Activity",
         new DateOnly(2026, 7, 9),
         new TimeOnly(12, 0),
         "Draft"
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         sourceActivityId,
         sourceId
      );
      await InsertActivityEntityLinkAsync(
         dataSource,
         targetActivityId,
         targetId
      );
      await InsertLinkAsync(dataSource, sourceId, linkedId);
      await InsertLinkAsync(dataSource, linkedId, targetId);

      try
      {
         var result = await repository.MergeEntityAsync(
            sourceId,
            targetId,
            CancellationToken.None
         );

         Assert.Equal(1, result.ActivityEntityLinksMoved);
         Assert.Equal(1, result.DuplicateEntityLinksDeleted);
         Assert.Equal(0, result.EntityLinksMoved);
         Assert.False(await EntityExistsAsync(dataSource, sourceId));
         Assert.True(await EntityExistsAsync(dataSource, targetId));
         Assert.Equal(
            2,
            await CountActivityEntityLinksAsync(dataSource, targetId)
         );
         Assert.Equal(
            1,
            await CountEntityLinksAsync(dataSource, targetId, linkedId)
         );
      }
      finally
      {
         await DeleteActivityEntityLinksAsync(dataSource, sourceActivityId);
         await DeleteActivityEntityLinksAsync(dataSource, targetActivityId);
         await DeleteActivityAsync(dataSource, sourceActivityId);
         await DeleteActivityAsync(dataSource, targetActivityId);
         await DeleteLinksAsync(dataSource, sourceId);
         await DeleteLinksAsync(dataSource, targetId);
         await DeleteLinksAsync(dataSource, linkedId);
         await DeleteEntityAsync(dataSource, sourceId);
         await DeleteEntityAsync(dataSource, targetId);
         await DeleteEntityAsync(dataSource, linkedId);
      }
   }

   private static NpgsqlDataSource CreateDataSource()
   {
      var connectionString = PostgresConnectionStrings.ResolveDefault();

      return new NpgsqlDataSourceBuilder(connectionString).Build();
   }

   private static async Task InsertEntityAsync(
      NpgsqlDataSource dataSource,
      Guid entityId,
      string entityName,
      string? aliasName = null
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
            expected_stability_id,
            alias_name
         )
         values (
            @id,
            @canonical_name,
            'Person',
            'football',
            'se',
            'NationalityOrSportingIdentity',
            'Test coverage',
            'review',
            'short_term',
            @alias_name
         )
         """;
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("canonical_name", entityName);
      command.Parameters.AddWithValue(
         "alias_name",
         (object?)aliasName ?? DBNull.Value
      );

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

   private static async Task InsertLinkAsync(
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

   private static async Task InsertActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityId,
      string title,
      DateOnly activityDate,
      TimeOnly localStartTime,
      string publicationStatusId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into activities (
            id,
            title,
            description,
            teaser,
            activity_type_id,
            sport_id,
            activity_date,
            local_start_time,
            starts_at,
            time_zone_id,
            publication_status_id,
            tv_channel_name,
            slug
         )
         values (
            @id,
            @title,
            null,
            null,
            'Match',
            'football',
            @activity_date,
            @local_start_time,
            @starts_at,
            'Europe/Stockholm',
            @publication_status_id,
            null,
            @slug
         )
         """;
      command.Parameters.AddWithValue("id", activityId);
      command.Parameters.AddWithValue("title", title);
      command.Parameters.AddWithValue("activity_date", activityDate);
      command.Parameters.AddWithValue("local_start_time", localStartTime);
      command.Parameters.AddWithValue(
         "starts_at",
         activityDate.ToDateTime(localStartTime)
      );
      command.Parameters.AddWithValue(
         "publication_status_id",
         publicationStatusId
      );
      command.Parameters.AddWithValue(
         "slug",
         $"test-entity-activity-{activityId:N}"
      );

      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertActivityEntityLinkAsync(
      NpgsqlDataSource dataSource,
      Guid activityId,
      Guid entityId,
      Guid? organizationEntityId = null
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into activity_entity_links (
            id,
            activity_id,
            entity_id,
            organization_entity_id
         )
         values (
            @id,
            @activity_id,
            @entity_id,
            @organization_entity_id
         )
         """;
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", entityId);
      command.Parameters.AddWithValue(
         "organization_entity_id",
         (object?)organizationEntityId ?? DBNull.Value
      );

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

   private static async Task DeleteActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from activities
         where id = @id
         """;
      command.Parameters.AddWithValue("id", activityId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteActivityEntityLinksAsync(
      NpgsqlDataSource dataSource,
      Guid activityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from activity_entity_links
         where activity_id = @id
         """;
      command.Parameters.AddWithValue("id", activityId);

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

   private static async Task<bool> EntityExistsAsync(
      NpgsqlDataSource dataSource,
      Guid entityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         select exists (
            select 1
            from entities
            where id = @id
         )
         """;
      command.Parameters.AddWithValue("id", entityId);

      return (bool)(await command.ExecuteScalarAsync())!;
   }

   private static async Task<int> CountActivityEntityLinksAsync(
      NpgsqlDataSource dataSource,
      Guid entityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         select count(*)::int
         from activity_entity_links
         where entity_id = @id
         """;
      command.Parameters.AddWithValue("id", entityId);

      return (int)(await command.ExecuteScalarAsync())!;
   }

   private static async Task<int> CountEntityLinksAsync(
      NpgsqlDataSource dataSource,
      Guid firstEntityId,
      Guid secondEntityId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         select count(*)::int
         from entity_to_entity_links
         where (
               source_entity_id = @first_entity_id
               and target_entity_id = @second_entity_id
            )
            or (
               source_entity_id = @second_entity_id
               and target_entity_id = @first_entity_id
            )
         """;
      command.Parameters.AddWithValue("first_entity_id", firstEntityId);
      command.Parameters.AddWithValue("second_entity_id", secondEntityId);

      return (int)(await command.ExecuteScalarAsync())!;
   }
}
