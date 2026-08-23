using SESport.Data.Repositories;

namespace SESport.Core.Tests.Data;

public sealed class MemberPushRepositoryTests
{
   [Fact]
   public async Task ClaimDueNotificationsCanQueryThePushSchema()
   {
      await using var dataSource = CreateDataSource();
      var repository = new MemberPushRepository(dataSource);
      var now = DateTimeOffset.UtcNow;

      var notifications = await repository.ClaimDueNotificationsAsync(
         now,
         now.AddMinutes(-5),
         10,
         10,
         CancellationToken.None
      );

      Assert.Empty(notifications);
   }

   [Fact]
   public async Task ClaimDueNotificationsSkipsMembersWithoutOptIn()
   {
      var memberId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var activityId = Guid.NewGuid();
      var subscriptionId = Guid.NewGuid();
      var now = DateTimeOffset.UtcNow;
      var startsAt = now.AddMinutes(5);

      await using var dataSource = CreateDataSource();
      var repository = new MemberPushRepository(dataSource);

      try
      {
         await using(var command = dataSource.CreateCommand(
            """
            insert into members (
               id,
               email,
               email_normalized
            )
            values (
               @member_id,
               @email,
               @email_normalized
            );

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
               @person_id,
               'Push Test Person',
               @person_entity_type_id,
               'football',
               @country_id,
               'NationalityOrSportingIdentity',
               'Push opt-in coverage',
               'tier_3',
               'short_term'
            );

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
               published_at
            )
            values (
               @activity_id,
               'Push opt-in activity',
               'Match',
               'football',
               @activity_date,
               @local_start_time,
               @starts_at,
               @published_status,
               @slug,
               @published_at
            );

            insert into activity_entity_links (
               id,
               activity_id,
               entity_id,
               is_active
            )
            values (
               @activity_link_id,
               @activity_id,
               @person_id,
               true
            );

            insert into member_entity_watches (
               member_id,
               entity_id
            )
            values (
               @member_id,
               @person_id
            );

            insert into member_push_subscriptions (
               id,
               member_id,
               endpoint,
               p256dh,
               auth
            )
            values (
               @subscription_id,
               @member_id,
               @endpoint,
               'test-p256dh',
               'test-auth'
            );
            """
         ))
         {
            command.Parameters.AddWithValue("member_id", memberId);
            command.Parameters.AddWithValue(
               "email",
               $"push-test-{memberId:N}@example.test"
            );
            command.Parameters.AddWithValue(
               "email_normalized",
               $"push-test-{memberId:N}@example.test"
            );
            command.Parameters.AddWithValue("person_id", personId);
            command.Parameters.AddWithValue(
               "person_entity_type_id",
               TrackedEntityTypeIds.Person
            );
            command.Parameters.AddWithValue(
               "country_id",
               PrimaryCountry.Id
            );
            command.Parameters.AddWithValue("activity_id", activityId);
            command.Parameters.AddWithValue(
               "activity_date",
               DateOnly.FromDateTime(startsAt.UtcDateTime)
            );
            command.Parameters.AddWithValue(
               "local_start_time",
               startsAt.TimeOfDay
            );
            command.Parameters.AddWithValue("starts_at", startsAt);
            command.Parameters.AddWithValue(
               "published_status",
               ActivityPublicationStatusIds.Published
            );
            command.Parameters.AddWithValue(
               "slug",
               $"push-test-{activityId:N}"
            );
            command.Parameters.AddWithValue("published_at", now);
            command.Parameters.AddWithValue(
               "activity_link_id",
               Guid.NewGuid()
            );
            command.Parameters.AddWithValue(
               "subscription_id",
               subscriptionId
            );
            command.Parameters.AddWithValue(
               "endpoint",
               $"https://example.test/push/{subscriptionId:N}"
            );
            await command.ExecuteNonQueryAsync();
         }

         Assert.Equal(
            0,
            (await repository.GetNotificationLeadTimeMinutesAsync(
               memberId,
               CancellationToken.None
            )).GetValueOrDefault()
         );

         var disabledNotifications =
            await repository.ClaimDueNotificationsAsync(
               now,
               now.AddMinutes(-5),
               60,
               10,
               CancellationToken.None
            );
         Assert.Empty(disabledNotifications);

         await using(var enableCommand = dataSource.CreateCommand(
            """
            update members
            set push_notification_lead_time_minutes = 10
            where id = @member_id
            """
         ))
         {
            enableCommand.Parameters.AddWithValue("member_id", memberId);
            await enableCommand.ExecuteNonQueryAsync();
         }

         var enabledNotifications =
            await repository.ClaimDueNotificationsAsync(
               now,
               now.AddMinutes(-5),
               60,
               10,
               CancellationToken.None
            );
         var notification = Assert.Single(enabledNotifications);
         Assert.Equal(activityId, notification.ActivityId);
         Assert.Equal(10, notification.LeadTimeMinutes);
         Assert.Single(notification.Subscriptions);
      }
      finally
      {
         await using var cleanup = dataSource.CreateCommand(
            """
            delete from activity_entity_links
            where activity_id = @activity_id;
            delete from activities
            where id = @activity_id;
            delete from entities
            where id = @person_id;
            delete from members
            where id = @member_id;
            """
         );
         cleanup.Parameters.AddWithValue("activity_id", activityId);
         cleanup.Parameters.AddWithValue("person_id", personId);
         cleanup.Parameters.AddWithValue("member_id", memberId);
         await cleanup.ExecuteNonQueryAsync();
      }
   }
}
