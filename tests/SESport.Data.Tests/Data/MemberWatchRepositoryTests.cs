using Npgsql;
using SESport.Core.Formatting;
using SESport.Data.Repositories;

namespace SESport.Core.Tests.Data;

public sealed class MemberWatchRepositoryTests
{
   [Fact]
   public async Task GetWatchedEntityCountCountsAllMemberWatches()
   {
      var memberId = Guid.NewGuid();
      var firstPersonId = Guid.NewGuid();
      var secondPersonId = Guid.NewGuid();
      await using var dataSource = CreateDataSource();
      var repository = new MemberWatchRepository(dataSource);

      try
      {
         await InsertMemberAsync(dataSource, memberId);
         await InsertPersonAsync(dataSource, firstPersonId);
         await InsertPersonAsync(dataSource, secondPersonId);
         Assert.True(
            await repository.TryAddEntityWatchAsync(
               memberId,
               firstPersonId,
               CancellationToken.None
            )
         );
         Assert.True(
            await repository.TryAddEntityWatchAsync(
               memberId,
               secondPersonId,
               CancellationToken.None
            )
         );

         var count = await repository.GetWatchedEntityCountAsync(
            memberId,
            CancellationToken.None
         );

         Assert.Equal(2, count);
      }
      finally
      {
         await DeleteTestDataAsync(
            dataSource,
            memberId,
            [firstPersonId, secondPersonId],
            [],
            Guid.Empty,
            []
         );
      }
   }

   [Fact]
   public async Task GetWatchedEntitiesIncludesNextPublishedActiveActivity()
   {
      var memberId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var nationalTeamId = Guid.NewGuid();
      var activityGroupId = Guid.NewGuid();
      var inactiveActivityId = Guid.NewGuid();
      var draftActivityId = Guid.NewGuid();
      var nextActivityId = Guid.NewGuid();
      var imageId = Guid.NewGuid();
      var now = new DateTimeOffset(
         2026,
         1,
         1,
         0,
         0,
         0,
         TimeSpan.Zero
      );
      await using var dataSource = CreateDataSource();
      var repository = new MemberWatchRepository(dataSource);

      try
      {
         await InsertMemberAsync(dataSource, memberId);
         await InsertPersonAsync(dataSource, personId);
         await InsertPrimaryImageAsync(dataSource, imageId, personId);
         await InsertOrganizationAsync(dataSource, organizationId);
         await InsertOrganizationAsync(
            dataSource,
            nationalTeamId,
            TrackedEntityTypeIds.NationalTeam,
            "Sweden National Team",
            "Sweden"
         );
         await InsertEntityLinkAsync(
            dataSource,
            personId,
            organizationId
         );
         await InsertEntityLinkAsync(
            dataSource,
            personId,
            nationalTeamId
         );
         await InsertActivityGroupAsync(dataSource, activityGroupId);
         await InsertActivityAsync(
            dataSource,
            inactiveActivityId,
            "Inactive watch activity",
            ToUtc(new TimeOnly(17, 0)),
            ActivityPublicationStatusIds.Published,
            now,
            activityGroupId,
            organizationId
         );
         await InsertActivityAsync(
            dataSource,
            draftActivityId,
            "Draft watch activity",
            ToUtc(new TimeOnly(17, 5)),
            ActivityPublicationStatusIds.Draft,
            now,
            activityGroupId,
            organizationId
         );
         await InsertActivityAsync(
            dataSource,
            nextActivityId,
            "Newcastle - Liverpool",
            ToUtc(new TimeOnly(17, 15)),
            ActivityPublicationStatusIds.Published,
            now,
            activityGroupId,
            organizationId
         );
         await InsertActivityLinkAsync(
            dataSource,
            inactiveActivityId,
            personId,
            isActive: false
         );
         await InsertActivityLinkAsync(
            dataSource,
            draftActivityId,
            personId
         );
         await InsertActivityLinkAsync(
            dataSource,
            nextActivityId,
            personId
         );
         var searchImage = await repository.GetPersonPrimaryImageAsync(
            personId,
            CancellationToken.None
         );
         Assert.NotNull(searchImage);
         Assert.Equal("image/jpeg", searchImage.MimeType);
         Assert.Equal(new byte[] { 9, 8 }, searchImage.Data);
         Assert.True(
            await repository.TryAddEntityWatchAsync(
               memberId,
               personId,
               CancellationToken.None
            )
         );
         var searchResults = await repository.SearchPeopleAsync(
            "Watch Test",
            memberId,
            5,
            CancellationToken.None
         );
         var searchResult = Assert.Single(searchResults);
         Assert.Equal(personId, searchResult.Id);
         Assert.True(searchResult.IsWatched);
         var partialNameResults = await repository.SearchPeopleAsync(
            "Watch Per",
            memberId,
            100,
            CancellationToken.None
         );
         Assert.Contains(
            partialNameResults,
            result => result.Id == personId
         );
         var sportAndNameResults = await repository.SearchPeopleAsync(
            $"Watch {searchResult.SportName}",
            memberId,
            100,
            CancellationToken.None
         );
         Assert.Contains(
            sportAndNameResults,
            result => result.Id == personId
         );
         var teamAndNameResults = await repository.SearchPeopleAsync(
            "Watch Liverpool",
            memberId,
            100,
            CancellationToken.None
         );
         Assert.Contains(
            teamAndNameResults,
            result => result.Id == personId
         );
         var nationalTeamAndNameResults =
            await repository.SearchPeopleAsync(
               "Watch Sweden",
               memberId,
               100,
               CancellationToken.None
            );
         Assert.Contains(
            nationalTeamAndNameResults,
            result => result.Id == personId
         );
         var canonicalTeamAndNameResults =
            await repository.SearchPeopleAsync(
               "Watch Football Club",
               memberId,
               100,
               CancellationToken.None
            );
         Assert.Contains(
            canonicalTeamAndNameResults,
            result => result.Id == personId
         );
         var teamResults = await repository.SearchPeopleAsync(
            "liverpool",
            memberId,
            100,
            CancellationToken.None
         );
         Assert.Contains(teamResults, result => result.Id == personId);

         var watches = await repository.GetWatchedEntitiesAsync(
            memberId,
            now,
            CancellationToken.None
         );

         var watch = Assert.Single(watches);
         Assert.True(watch.HasPrimaryImage);
         Assert.NotNull(watch.PrimaryImageSource);
         Assert.Equal(
            "https://example.test/watch-image",
            watch.PrimaryImageSource.SourceUrl
         );
         Assert.Equal(
            "Test license",
            watch.PrimaryImageSource.LicenseName
         );
         Assert.NotNull(watch.NextActivity);
         Assert.Equal(
            "Newcastle - Liverpool",
            watch.NextActivity.Title
         );
         Assert.Equal(
            "Liverpool F.C.",
            watch.NextActivity.RelatedOrganizationName
         );
         Assert.Equal(
            ToUtc(new TimeOnly(17, 15)),
            watch.NextActivity.StartsAt
         );
      }
      finally
      {
         await DeleteTestDataAsync(
            dataSource,
            memberId,
            [personId],
            [organizationId, nationalTeamId],
            activityGroupId,
            [inactiveActivityId, draftActivityId, nextActivityId]
         );
      }
   }

   [Fact]
   public async Task SearchPeoplePrioritizesNamesStartingWithQuery()
   {
      var memberId = Guid.NewGuid();
      var prefixedPersonId = Guid.NewGuid();
      var embeddedPersonId = Guid.NewGuid();
      var prefixedName = $"Fredrik Prefix {memberId:N}";
      var embeddedName = $"Alice Fredriksson {memberId:N}";
      await using var dataSource = CreateDataSource();
      var repository = new MemberWatchRepository(dataSource);

      try
      {
         await InsertMemberAsync(dataSource, memberId);
         await InsertPersonAsync(
            dataSource,
            prefixedPersonId,
            prefixedName
         );
         await InsertPersonAsync(
            dataSource,
            embeddedPersonId,
            embeddedName
         );

         var results = await repository.SearchPeopleAsync(
            "fredrik",
            memberId,
            1000,
            CancellationToken.None
         );
         var resultIds = results.Select(result => result.Id).ToArray();
         var prefixedIndex = Array.IndexOf(
            resultIds,
            prefixedPersonId
         );
         var embeddedIndex = Array.IndexOf(
            resultIds,
            embeddedPersonId
         );

         Assert.True(prefixedIndex >= 0);
         Assert.True(embeddedIndex >= 0);
         Assert.True(prefixedIndex < embeddedIndex);
      }
      finally
      {
         await DeleteTestDataAsync(
            dataSource,
            memberId,
            [prefixedPersonId, embeddedPersonId],
            [],
            Guid.Empty,
            []
         );
      }
   }

   [Fact]
   public async Task SearchPeoplePrioritizesFirstAndLastNamePrefixes()
   {
      var memberId = Guid.NewGuid();
      var matchingPersonId = Guid.NewGuid();
      var otherPersonId = Guid.NewGuid();
      var uniqueFirstName = $"Niklas{memberId:N}";
      await using var dataSource = CreateDataSource();
      var repository = new MemberWatchRepository(dataSource);

      try
      {
         await InsertMemberAsync(dataSource, memberId);
         await InsertPersonAsync(
            dataSource,
            matchingPersonId,
            $"{uniqueFirstName} Lemke"
         );
         await InsertPersonAsync(
            dataSource,
            otherPersonId,
            $"{uniqueFirstName} Aldén"
         );

         var results = await repository.SearchPeopleAsync(
            $"{uniqueFirstName} l",
            memberId,
            100,
            CancellationToken.None
         );

         Assert.Equal(matchingPersonId, results[0].Id);
         Assert.Contains(results, result => result.Id == otherPersonId);
      }
      finally
      {
         await DeleteTestDataAsync(
            dataSource,
            memberId,
            [matchingPersonId, otherPersonId],
            [],
            Guid.Empty,
            []
         );
      }
   }

   [Fact]
   public async Task SearchPeopleIgnoresAccentsButKeepsSwedishLettersDistinct()
   {
      var memberId = Guid.NewGuid();
      var accentedPersonId = Guid.NewGuid();
      var swedishPersonId = Guid.NewGuid();
      await using var dataSource = CreateDataSource();
      var repository = new MemberWatchRepository(dataSource);

      try
      {
         await InsertMemberAsync(dataSource, memberId);
         await InsertPersonAsync(
            dataSource,
            accentedPersonId,
            "Franzén"
         );
         await InsertPersonAsync(
            dataSource,
            swedishPersonId,
            "Åke"
         );

         var accentInsensitiveResults =
            await repository.SearchPeopleAsync(
               "franzen",
               memberId,
               100,
               CancellationToken.None
            );
         var swedishLetterResults = await repository.SearchPeopleAsync(
            "ake",
            memberId,
            100,
            CancellationToken.None
         );

         Assert.Contains(
            accentInsensitiveResults,
            result => result.Id == accentedPersonId
         );
         Assert.DoesNotContain(
            swedishLetterResults,
            result => result.Id == swedishPersonId
         );
      }
      finally
      {
         await DeleteTestDataAsync(
            dataSource,
            memberId,
            [accentedPersonId, swedishPersonId],
            [],
            Guid.Empty,
            []
         );
      }
   }

   private static DateTimeOffset ToUtc(TimeOnly time)
   {
      return TimeZoneHelper.ToUtc(
         DistantActivityDate,
         time,
         SportDay.TimeZoneId
      );
   }

   private static async Task InsertMemberAsync(
      NpgsqlDataSource dataSource,
      Guid memberId
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         insert into members (
            id,
            email,
            email_normalized
         )
         values (
            @id,
            @email,
            @email_normalized
         )
         """
      );
      var email = $"watch-test-{memberId:N}@example.test";
      command.Parameters.AddWithValue("id", memberId);
      command.Parameters.AddWithValue("email", email);
      command.Parameters.AddWithValue("email_normalized", email);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertPersonAsync(
      NpgsqlDataSource dataSource,
      Guid personId,
      string name = "Watch Test Person"
   )
   {
      await using var command = dataSource.CreateCommand(
         """
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
            'football',
            @country_id,
            'NationalityOrSportingIdentity',
            'Test coverage',
            'tier_3',
            'short_term'
         )
         """
      );
      command.Parameters.AddWithValue("id", personId);
      command.Parameters.AddWithValue("canonical_name", name);
      command.Parameters.AddWithValue(
         "entity_type_id",
         TrackedEntityTypeIds.Person
      );
      command.Parameters.AddWithValue("country_id", PrimaryCountry.Id);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertPrimaryImageAsync(
      NpgsqlDataSource dataSource,
      Guid imageId,
      Guid entityId
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         insert into entity_images (
            id,
            entity_id,
            image_data,
            mime_type,
            pixel_width,
            pixel_height,
            thumbnail_data,
            thumbnail_mime_type,
            thumbnail_pixel_width,
            thumbnail_pixel_height,
            source_kind,
            source_url,
            license_name,
            review_status,
            reviewed_at,
            is_primary
         )
         values (
            @id,
            @entity_id,
            @image_data,
            'image/jpeg',
            1,
            1,
            @thumbnail_data,
            'image/jpeg',
            2,
            2,
            'test',
            'https://example.test/watch-image',
            'Test license',
            @review_status,
            @reviewed_at,
            true
         )
         """
      );
      command.Parameters.AddWithValue("id", imageId);
      command.Parameters.AddWithValue("entity_id", entityId);
      command.Parameters.AddWithValue(
         "image_data",
         new byte[] { 1, 2, 3 }
      );
      command.Parameters.AddWithValue(
         "thumbnail_data",
         new byte[] { 9, 8 }
      );
      command.Parameters.AddWithValue(
         "review_status",
         EntityImageReviewStatusIds.Approved
      );
      command.Parameters.AddWithValue(
         "reviewed_at",
         new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertActivityGroupAsync(
      NpgsqlDataSource dataSource,
      Guid activityGroupId
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         insert into activity_groups (
            id,
            title,
            sport_id,
            start_date,
            end_date
         )
         values (
            @id,
            'Premier League',
            'football',
            @start_date,
            @end_date
         )
         """
      );
      command.Parameters.AddWithValue("id", activityGroupId);
      command.Parameters.AddWithValue(
         "start_date",
         DistantActivityDate
      );
      command.Parameters.AddWithValue(
         "end_date",
         DistantActivityDate.AddDays(1)
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertOrganizationAsync(
      NpgsqlDataSource dataSource,
      Guid organizationId,
      string entityTypeId = TrackedEntityTypeIds.Club,
      string canonicalName = "Liverpool Football Club",
      string aliasName = "Liverpool F.C."
   )
   {
      await using var command = dataSource.CreateCommand(
         """
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
            @entity_type_id,
            'football',
            @country_id,
            'NationalityOrSportingIdentity',
            'Test coverage',
            'tier_3',
            'short_term',
            @alias_name
         )
         """
      );
      command.Parameters.AddWithValue("id", organizationId);
      command.Parameters.AddWithValue("canonical_name", canonicalName);
      command.Parameters.AddWithValue(
         "entity_type_id",
         entityTypeId
      );
      command.Parameters.AddWithValue("country_id", PrimaryCountry.Id);
      command.Parameters.AddWithValue("alias_name", aliasName);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityId,
      string title,
      DateTimeOffset startsAt,
      string publicationStatus,
      DateTimeOffset publishedAt,
      Guid activityGroupId,
      Guid organizationId
   )
   {
      var localStart = TimeZoneHelper.ToLocal(
         startsAt,
         SportDay.TimeZoneId
      );
      await using var command = dataSource.CreateCommand(
         """
         insert into activities (
            id,
            title,
            activity_type_id,
            sport_id,
            activity_date,
            local_start_time,
            starts_at,
            publication_status_id,
            slug,
            published_at,
            activity_group_id,
            organization_entity_id
         )
         values (
            @id,
            @title,
            'Match',
            'football',
            @activity_date,
            @local_start_time,
            @starts_at,
            @publication_status_id,
            @slug,
            @published_at,
            @activity_group_id,
            @organization_entity_id
         )
         """
      );
      command.Parameters.AddWithValue("id", activityId);
      command.Parameters.AddWithValue("title", title);
      command.Parameters.AddWithValue(
         "activity_date",
         DateOnly.FromDateTime(localStart.DateTime)
      );
      command.Parameters.AddWithValue(
         "local_start_time",
         localStart.TimeOfDay
      );
      command.Parameters.AddWithValue("starts_at", startsAt);
      command.Parameters.AddWithValue(
         "publication_status_id",
         publicationStatus
      );
      command.Parameters.AddWithValue(
         "slug",
         $"watch-test-{activityId:N}"
      );
      command.Parameters.AddWithValue(
         "published_at",
         publicationStatus == ActivityPublicationStatusIds.Published
            ? publishedAt
            : DBNull.Value
      );
      command.Parameters.AddWithValue(
         "activity_group_id",
         activityGroupId
      );
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationId
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertActivityLinkAsync(
      NpgsqlDataSource dataSource,
      Guid activityId,
      Guid personId,
      bool isActive = true
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         insert into activity_entity_links (
            id,
            activity_id,
            entity_id,
            is_active
         )
         values (
            @id,
            @activity_id,
            @entity_id,
            @is_active
         )
         """
      );
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", personId);
      command.Parameters.AddWithValue("is_active", isActive);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertEntityLinkAsync(
      NpgsqlDataSource dataSource,
      Guid personId,
      Guid organizationId
   )
   {
      await using var command = dataSource.CreateCommand(
         """
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
         """
      );
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("source_entity_id", personId);
      command.Parameters.AddWithValue("target_entity_id", organizationId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteTestDataAsync(
      NpgsqlDataSource dataSource,
      Guid memberId,
      IReadOnlyCollection<Guid> personIds,
      IReadOnlyCollection<Guid> organizationIds,
      Guid activityGroupId,
      IReadOnlyCollection<Guid> activityIds
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         delete from member_entity_watches
         where member_id = @member_id;
         delete from activity_entity_links
         where activity_id = any(@activity_ids);
         delete from entity_to_entity_links
         where source_entity_id = any(@person_ids)
            or target_entity_id = any(@person_ids)
            or source_entity_id = any(@organization_ids)
            or target_entity_id = any(@organization_ids);
         delete from activities
         where id = any(@activity_ids);
         delete from activity_groups
         where id = @activity_group_id;
         delete from entities
         where id = any(@person_ids)
            or id = any(@organization_ids);
         delete from members
         where id = @member_id;
         """
      );
      command.Parameters.AddWithValue("member_id", memberId);
      command.Parameters.AddWithValue("person_ids", personIds.ToArray());
      command.Parameters.AddWithValue(
         "organization_ids",
         organizationIds.ToArray()
      );
      command.Parameters.AddWithValue(
         "activity_group_id",
         activityGroupId
      );
      command.Parameters.AddWithValue("activity_ids", activityIds.ToArray());
      await command.ExecuteNonQueryAsync();
   }
}
