using Npgsql;
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
         Assert.Equal([nationalTeamId], template.LinkedEntityIds);
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
      var connectionString =
         Environment.GetEnvironmentVariable("ConnectionStrings__Default") ??
         "Host=localhost;Port=5432;Database=sesport;" +
         "Username=sesport;Password=sesport";

      return new NpgsqlDataSourceBuilder(connectionString).Build();
   }

   private static async Task InsertEntityAsync(
      NpgsqlDataSource dataSource,
      Guid entityId,
      string entityName
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
            'Person',
            'football',
            'se',
            'NationalityOrSportingIdentity',
            'Test coverage',
            'review',
            'short_term'
         )
         """;
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("canonical_name", entityName);

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
