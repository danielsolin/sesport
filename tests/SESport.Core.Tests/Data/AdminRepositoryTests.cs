using Npgsql;

using SESport.Core.Configuration;
using SESport.Core.Domain;
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
