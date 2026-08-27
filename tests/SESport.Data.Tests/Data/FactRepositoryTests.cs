using Npgsql;

using SESport.Core.Formatting;

namespace SESport.Core.Tests.Data;

public sealed class FactRepositoryTests
{
   private static readonly DateOnly DistantActivityDate =
      new(2099, 12, 20);

   [Fact]
   public async Task StoresFactsForActivitiesAndEntities()
   {
      var activityId = Guid.NewGuid();
      var entityId = Guid.NewGuid();
      var activityFactText =
         "The event will feature a record 148 crews.";
      var entityFactText = "The athlete made their senior debut in 2024.";

      await using var dataSource = CreateDataSource();
      var repository = new FactRepository(dataSource);

      try
      {
         await InsertActivityAsync(dataSource, activityId);
         await InsertEntityAsync(dataSource, entityId);

         var activityFact = await repository.CreateForActivityAsync(
            activityId,
            $"  {activityFactText}  ",
            CancellationToken.None
         );
         var entityFact = await repository.CreateForEntityAsync(
            entityId,
            entityFactText,
            CancellationToken.None
         );

         Assert.Equal(FactSubjectTypes.Activity, activityFact.SubjectType);
         Assert.Equal(activityId, activityFact.SubjectId);
         Assert.Equal(activityFactText, activityFact.Text);
         Assert.Equal(FactSubjectTypes.Entity, entityFact.SubjectType);
         Assert.Equal(entityId, entityFact.SubjectId);

         var activityFacts = await repository.GetForActivityAsync(
            activityId,
            CancellationToken.None
         );
         var entityFacts = await repository.GetForEntityAsync(
            entityId,
            CancellationToken.None
         );

         Assert.Equal(activityFact, Assert.Single(activityFacts));
         Assert.Equal(entityFact, Assert.Single(entityFacts));
      }
      finally
      {
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteEntityAsync(dataSource, entityId);
      }
   }

   [Fact]
   public async Task SharesActivityGroupFactsWithAllActivitiesInGroup()
   {
      var activityGroupId = Guid.NewGuid();
      var firstActivityId = Guid.NewGuid();
      var secondActivityId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new FactRepository(dataSource);

      try
      {
         await InsertActivityGroupAsync(dataSource, activityGroupId);
         await InsertActivityAsync(dataSource, firstActivityId);
         await InsertActivityAsync(dataSource, secondActivityId);
         await AssignActivityGroupAsync(
            dataSource,
            firstActivityId,
            activityGroupId
         );
         await AssignActivityGroupAsync(
            dataSource,
            secondActivityId,
            activityGroupId
         );

         var facts = await repository.AddForActivityGroupAsync(
            activityGroupId,
            [
               new FactDraft(
                  "A shared group fact.",
                  [
                     new FactSourceDraft(
                        "https://example.test/group",
                        null,
                        null
                     )
                  ]
               )
            ],
            CancellationToken.None
         );

         var fact = Assert.Single(facts);
         Assert.Equal(FactSubjectTypes.ActivityGroup, fact.SubjectType);
         Assert.Equal(activityGroupId, fact.SubjectId);
         Assert.Single(
            await repository.GetForActivityAsync(
               firstActivityId,
               CancellationToken.None
            )
         );
         Assert.Single(
            await repository.GetForActivityAsync(
               secondActivityId,
               CancellationToken.None
            )
         );
         Assert.True(
            await repository.DeleteForActivityAsync(
               fact.Id,
               secondActivityId,
               CancellationToken.None
            )
         );
         Assert.Empty(
            await repository.GetForActivityGroupAsync(
               activityGroupId,
               CancellationToken.None
            )
         );
      }
      finally
      {
         await DeleteActivityAsync(dataSource, firstActivityId);
         await DeleteActivityAsync(dataSource, secondActivityId);
         await DeleteActivityGroupAsync(dataSource, activityGroupId);
      }
   }

   [Fact]
   public async Task AppendsActivityGroupFactsAcrossRuns()
   {
      var activityGroupId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new FactRepository(dataSource);

      try
      {
         await InsertActivityGroupAsync(dataSource, activityGroupId);

         var firstFacts = await repository.AddForActivityGroupAsync(
            activityGroupId,
            [
               new FactDraft(
                  "First group fact.",
                  [
                     new FactSourceDraft(
                        "https://example.test/first",
                        null,
                        null
                     )
                  ]
               )
            ],
            CancellationToken.None
         );
         var secondFacts = await repository.AddForActivityGroupAsync(
            activityGroupId,
            [
               new FactDraft(
                  "Second group fact.",
                  [
                     new FactSourceDraft(
                        "https://example.test/second",
                        null,
                        null
                     )
                  ]
               )
            ],
            CancellationToken.None
         );

         Assert.Single(firstFacts);
         Assert.Single(secondFacts);

         var facts = await repository.GetForActivityGroupAsync(
            activityGroupId,
            CancellationToken.None
         );

         Assert.Equal(
            ["First group fact.", "Second group fact."],
            facts.Select(fact => fact.Text).OrderBy(text => text).ToArray()
         );
      }
      finally
      {
         await DeleteActivityGroupAsync(dataSource, activityGroupId);
      }
   }

   [Fact]
   public async Task UpdatesAndDeletesFact()
   {
      var activityId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new FactRepository(dataSource);

      try
      {
         await InsertActivityAsync(dataSource, activityId);

         var fact = await repository.CreateForActivityAsync(
            activityId,
            "Original fact.",
            CancellationToken.None
         );
         var updated = await repository.UpdateAsync(
            fact.Id,
            "Updated fact.",
            CancellationToken.None
         );

         Assert.NotNull(updated);
         Assert.Equal("Updated fact.", updated!.Text);
         Assert.True(updated.UpdatedAt >= fact.UpdatedAt);
         Assert.False(
            await repository.DeleteForActivityAsync(
               fact.Id,
               Guid.NewGuid(),
               CancellationToken.None
            )
         );
         Assert.True(
            await repository.DeleteForActivityAsync(
               fact.Id,
               activityId,
               CancellationToken.None
            )
         );
         Assert.Null(
            await repository.GetAsync(
               fact.Id,
               CancellationToken.None
            )
         );
      }
      finally
      {
         await DeleteActivityAsync(dataSource, activityId);
      }
   }

   [Fact]
   public async Task AddsActivityFactsWithLinkedSources()
   {
      var activityId = Guid.NewGuid();
      var sharedSource = new FactSourceDraft(
         "https://example.test/rally",
         "Rally page",
         "Supporting text"
      );

      await using var dataSource = CreateDataSource();
      var repository = new FactRepository(dataSource);

      try
      {
         await InsertActivityAsync(dataSource, activityId);
         var original = await repository.AddForActivityAsync(
            activityId,
            [
               new FactDraft("First fact.", [sharedSource]),
               new FactDraft("Second fact.", [sharedSource])
            ],
            CancellationToken.None
         );

         Assert.Equal(2, original.Count);
         var listedFacts = await repository.GetForActivityAsync(
            activityId,
            CancellationToken.None
         );
         Assert.Equal(
            sharedSource.Url,
            Assert.Single(listedFacts[0].SourceUrls)
         );
         var firstSources =
            await repository.GetSourcesAsync(
               original[0].Id,
               CancellationToken.None
            );
         var secondSources =
            await repository.GetSourcesAsync(
               original[1].Id,
               CancellationToken.None
            );
         Assert.Equal(
            Assert.Single(firstSources).Id,
            Assert.Single(secondSources).Id
         );
         Assert.Equal(
            1,
            await CountActivityEvidenceSourcesAsync(
               dataSource,
               activityId
            )
         );

         var added = await repository.AddForActivityAsync(
            activityId,
            [
               new FactDraft(
                  "Replacement fact.",
                  [
                     sharedSource,
                     new FactSourceDraft(
                        "https://example.test/new",
                        null,
                        null
                     )
                  ]
               )
            ],
            CancellationToken.None
         );

         Assert.Single(added);
         Assert.NotNull(
            await repository.GetAsync(original[0].Id, CancellationToken.None)
         );
         var addedSources = await repository.GetSourcesAsync(
            added[0].Id,
            CancellationToken.None
         );
         Assert.Equal(2, addedSources.Count);
         Assert.Equal(
            3,
            (await repository.GetForActivityAsync(
               activityId,
               CancellationToken.None
            )).Count
         );
         Assert.Equal(
            2,
            await CountActivityEvidenceSourcesAsync(
               dataSource,
               activityId
            )
         );
      }
      finally
      {
         await DeleteActivityAsync(dataSource, activityId);
      }
   }

   [Fact]
   public async Task DeletingSubjectCascadesToFacts()
   {
      var entityId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();
      var repository = new FactRepository(dataSource);

      try
      {
         await InsertEntityAsync(dataSource, entityId);
         var fact = await repository.CreateForEntityAsync(
            entityId,
            "Fact deleted with its entity.",
            CancellationToken.None
         );

         await DeleteEntityAsync(dataSource, entityId);

         Assert.Null(
            await repository.GetAsync(
               fact.Id,
               CancellationToken.None
            )
         );
      }
      finally
      {
         await DeleteEntityAsync(dataSource, entityId);
      }
   }

   [Fact]
   public async Task RejectsBlankFactText()
   {
      await using var dataSource = CreateDataSource();
      var repository = new FactRepository(dataSource);

      await Assert.ThrowsAsync<ArgumentException>(
         () => repository.CreateForActivityAsync(
            Guid.NewGuid(),
            " ",
            CancellationToken.None
         )
      );
   }

   private static async Task InsertActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityId
   )
   {
      var startsAt = TimeZoneHelper.ToUtc(
         DistantActivityDate,
         new TimeOnly(12, 0),
         SportDay.TimeZoneId
      );
      const string sql = """
         insert into activities (
            id,
            title,
            activity_type_id,
            sport_id,
            activity_date,
            local_start_time,
            starts_at,
            time_zone_id,
            publication_status_id,
            slug
         )
         values (
            @id,
            'Fact test activity',
            'Match',
            'football',
            @activity_date,
            '12:00',
            @starts_at,
            'Europe/Stockholm',
            'Draft',
            @slug
         )
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", activityId);
      command.Parameters.AddWithValue(
         "activity_date",
         DistantActivityDate
      );
      command.Parameters.AddWithValue("starts_at", startsAt);
      command.Parameters.AddWithValue(
         "slug",
         $"fact-test-{activityId:N}"
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertActivityGroupAsync(
      NpgsqlDataSource dataSource,
      Guid activityGroupId
   )
   {
      const string sql = """
         insert into activity_groups (
            id,
            title,
            sport_id,
            start_date,
            end_date
         )
         values (
            @id,
            'Fact test group',
            'football',
            @date,
            @date
         )
         """;
      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", activityGroupId);
      command.Parameters.AddWithValue("date", DistantActivityDate);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task AssignActivityGroupAsync(
      NpgsqlDataSource dataSource,
      Guid activityId,
      Guid activityGroupId
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         update activities
         set activity_group_id = @activity_group_id
         where id = @activity_id
         """
      );
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("activity_group_id", activityGroupId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertEntityAsync(
      NpgsqlDataSource dataSource,
      Guid entityId
   )
   {
      const string sql = """
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
            'Fact test entity',
            'Person',
            'football',
            @country_id,
            'NationalityOrSportingIdentity',
            'Test coverage',
            'tier_3',
            'short_term'
         )
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("country_id", PrimaryCountry.Id);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityId
   )
   {
      await using(var sourceCommand = dataSource.CreateCommand(
         """
         delete from sources
         where correlation_type = 'Activity'
            and correlation_id = @correlation_id
         """
      ))
      {
         sourceCommand.Parameters.AddWithValue(
            "correlation_id",
            activityId.ToString()
         );
         await sourceCommand.ExecuteNonQueryAsync();
      }

      await using var command = dataSource.CreateCommand(
         "delete from activities where id = @id"
      );
      command.Parameters.AddWithValue("id", activityId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task<int> CountActivityEvidenceSourcesAsync(
      NpgsqlDataSource dataSource,
      Guid activityId
   )
   {
      const string sql = """
         select count(*)
         from sources
         where correlation_type = 'Activity'
            and correlation_id = @correlation_id
            and kind = 'ActivityEvidence'
         """;
      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "correlation_id",
         activityId.ToString()
      );

      return Convert.ToInt32(await command.ExecuteScalarAsync());
   }

   private static async Task DeleteEntityAsync(
      NpgsqlDataSource dataSource,
      Guid entityId
   )
   {
      await using var command = dataSource.CreateCommand(
         "delete from entities where id = @id"
      );
      command.Parameters.AddWithValue("id", entityId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteActivityGroupAsync(
      NpgsqlDataSource dataSource,
      Guid activityGroupId
   )
   {
      await using(var sourceCommand = dataSource.CreateCommand(
         """
         delete from sources
         where correlation_type = 'ActivityGroup'
            and correlation_id = @correlation_id
         """
      ))
      {
         sourceCommand.Parameters.AddWithValue(
            "correlation_id",
            activityGroupId.ToString()
         );
         await sourceCommand.ExecuteNonQueryAsync();
      }

      await using var command = dataSource.CreateCommand(
         "delete from activity_groups where id = @id"
      );
      command.Parameters.AddWithValue("id", activityGroupId);
      await command.ExecuteNonQueryAsync();
   }

}
