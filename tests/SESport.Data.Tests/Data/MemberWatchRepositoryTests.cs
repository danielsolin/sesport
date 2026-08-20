using Npgsql;
using SESport.Core.Formatting;
using SESport.Data.Repositories;

namespace SESport.Core.Tests.Data;

public sealed class MemberWatchRepositoryTests
{
   [Fact]
   public async Task GetWatchedEntitiesIncludesNextPublishedActiveActivity()
   {
      var memberId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var activityGroupId = Guid.NewGuid();
      var inactiveActivityId = Guid.NewGuid();
      var draftActivityId = Guid.NewGuid();
      var nextActivityId = Guid.NewGuid();
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
         await InsertOrganizationAsync(dataSource, organizationId);
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
         Assert.True(
            await repository.TryAddEntityWatchAsync(
               memberId,
               personId,
               CancellationToken.None
            )
         );

         var watches = await repository.GetWatchedEntitiesAsync(
            memberId,
            now,
            CancellationToken.None
         );

         var watch = Assert.Single(watches);
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
            personId,
            organizationId,
            activityGroupId,
            [inactiveActivityId, draftActivityId, nextActivityId]
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
      Guid personId
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
            'Watch Test Person',
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
      command.Parameters.AddWithValue(
         "entity_type_id",
         TrackedEntityTypeIds.Person
      );
      command.Parameters.AddWithValue("country_id", PrimaryCountry.Id);
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
      Guid organizationId
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
            'Liverpool Football Club',
            @entity_type_id,
            'football',
            @country_id,
            'NationalityOrSportingIdentity',
            'Test coverage',
            'tier_3',
            'short_term',
            'Liverpool F.C.'
         )
         """
      );
      command.Parameters.AddWithValue("id", organizationId);
      command.Parameters.AddWithValue(
         "entity_type_id",
         TrackedEntityTypeIds.Club
      );
      command.Parameters.AddWithValue("country_id", PrimaryCountry.Id);
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
            : (object)DBNull.Value
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

   private static async Task DeleteTestDataAsync(
      NpgsqlDataSource dataSource,
      Guid memberId,
      Guid personId,
      Guid organizationId,
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
         delete from activities
         where id = any(@activity_ids);
         delete from activity_groups
         where id = @activity_group_id;
         delete from entities
         where id in (@person_id, @organization_id);
         delete from members
         where id = @member_id;
         """
      );
      command.Parameters.AddWithValue("member_id", memberId);
      command.Parameters.AddWithValue("person_id", personId);
      command.Parameters.AddWithValue(
         "organization_id",
         organizationId
      );
      command.Parameters.AddWithValue(
         "activity_group_id",
         activityGroupId
      );
      command.Parameters.AddWithValue("activity_ids", activityIds.ToArray());
      await command.ExecuteNonQueryAsync();
   }
}
