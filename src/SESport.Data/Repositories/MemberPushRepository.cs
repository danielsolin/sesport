using Npgsql;

using SESport.Core.Domain;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class MemberPushRepository(NpgsqlDataSource dataSource)
{
   public async Task<int?> GetNotificationLeadTimeMinutesAsync(
      Guid memberId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select push_notification_lead_time_minutes
         from members
         where id = @member_id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("member_id", memberId);
      var value = await command.ExecuteScalarAsync(cancellationToken);

      return value is null || value is DBNull
         ? null
         : (int)value;
   }

   public async Task<bool> SetNotificationLeadTimeMinutesAsync(
      Guid memberId,
      int minutes,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update members
         set
            push_notification_lead_time_minutes = @minutes,
            updated_at = now()
         where id = @member_id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("member_id", memberId);
      command.Parameters.AddWithValue("minutes", minutes);
      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }

   public async Task UpsertSubscriptionAsync(
      Guid memberId,
      MemberPushSubscriptionInput subscription,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into member_push_subscriptions (
            id,
            member_id,
            endpoint,
            p256dh,
            auth,
            expiration_at
         )
         values (
            @id,
            @member_id,
            @endpoint,
            @p256dh,
            @auth,
            @expiration_at
         )
         on conflict (endpoint)
         do update set
            member_id = excluded.member_id,
            p256dh = excluded.p256dh,
            auth = excluded.auth,
            expiration_at = excluded.expiration_at,
            updated_at = now()
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("member_id", memberId);
      command.Parameters.AddWithValue(
         "endpoint",
         subscription.Endpoint
      );
      command.Parameters.AddWithValue("p256dh", subscription.P256dh);
      command.Parameters.AddWithValue("auth", subscription.Auth);
      command.Parameters.AddWithValue(
         "expiration_at",
         subscription.ExpirationAt ?? (object)DBNull.Value
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task DeleteSubscriptionAsync(
      Guid memberId,
      Guid subscriptionId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from member_push_subscriptions
         where id = @subscription_id
            and member_id = @member_id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("subscription_id", subscriptionId);
      command.Parameters.AddWithValue("member_id", memberId);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<IReadOnlyList<MemberActivityPushNotification>>
      ClaimDueNotificationsAsync(
      DateTimeOffset now,
      DateTimeOffset claimBefore,
      int defaultLeadTimeMinutes,
      int batchSize,
      CancellationToken cancellationToken
   )
   {
      var sql = $$"""
         with eligible as (
            select
               member.id as member_id,
               activity.id as activity_id,
               activity.title,
               string_agg(
                  distinct person.canonical_name,
                  ', ' order by person.canonical_name
               ) as person_names,
               activity.starts_at,
               coalesce(
                  activity_group.public_date_mode,
                  '{{ActivityGroupPublicDateModeIds.SportDay}}'
               ) as public_date_mode,
               coalesce(
                  member.push_notification_lead_time_minutes,
                  @default_lead_time_minutes
               ) as lead_time_minutes,
               activity.tv_channel_name
            from members member
            join member_entity_watches watch
               on watch.member_id = member.id
            join activity_entity_links link
               on link.entity_id = watch.entity_id
               and link.is_active
            join entities person
               on person.id = link.entity_id
            join activities activity
               on activity.id = link.activity_id
            left join activity_groups activity_group
               on activity_group.id = activity.activity_group_id
            left join member_activity_push_notifications notification
               on notification.member_id = member.id
               and notification.activity_id = activity.id
            where activity.publication_status_id =
               '{{ActivityPublicationStatusIds.Published}}'
               and activity.starts_at is not null
               and activity.starts_at > @now
               and person.entity_type_id =
                  '{{TrackedEntityTypeIds.Person}}'
               and member.push_notification_lead_time_minutes > 0
               and notification.sent_at is null
               and (
                  notification.claimed_at is null
                  or notification.claimed_at < @claim_before
               )
               and exists (
                  select 1
                  from member_push_subscriptions subscription
                  where subscription.member_id = member.id
                     and (
                        subscription.expiration_at is null
                        or subscription.expiration_at > @now
                     )
               )
            group by
               member.id,
               activity.id,
               activity.title,
               activity.tv_channel_name,
               activity.starts_at,
               activity_group.public_date_mode,
               member.push_notification_lead_time_minutes
         ), candidates as (
            select
               eligible.*,
               eligible.starts_at - (
                  eligible.lead_time_minutes::double precision *
                  interval '1 minute'
               ) as scheduled_at
            from eligible
            where eligible.starts_at - (
                  eligible.lead_time_minutes::double precision *
                  interval '1 minute'
               ) <= @now
            order by
               scheduled_at,
               eligible.member_id,
               eligible.activity_id
            limit @batch_size
         ), claimed as (
            insert into member_activity_push_notifications (
               member_id,
               activity_id,
               scheduled_at,
               claimed_at,
               updated_at
            )
            select
               member_id,
               activity_id,
               scheduled_at,
               @now,
               @now
            from candidates
            on conflict (member_id, activity_id)
            do update set
               scheduled_at = excluded.scheduled_at,
               claimed_at = excluded.claimed_at,
               updated_at = excluded.updated_at
            where member_activity_push_notifications.sent_at is null
               and (
                  member_activity_push_notifications.claimed_at is null
                  or member_activity_push_notifications.claimed_at <
                     @claim_before
               )
            returning member_id, activity_id
         )
         select
            claimed.member_id,
            claimed.activity_id,
            candidates.title,
            candidates.person_names,
            candidates.starts_at,
            candidates.public_date_mode,
            candidates.lead_time_minutes,
            candidates.tv_channel_name,
            subscription.id,
            subscription.endpoint,
            subscription.p256dh,
            subscription.auth
         from claimed
         join candidates
            on candidates.member_id = claimed.member_id
            and candidates.activity_id = claimed.activity_id
         join member_push_subscriptions subscription
            on subscription.member_id = claimed.member_id
            and (
               subscription.expiration_at is null
               or subscription.expiration_at > @now
            )
         order by
            candidates.scheduled_at,
            claimed.member_id,
            claimed.activity_id,
            subscription.id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("now", now);
      command.Parameters.AddWithValue("claim_before", claimBefore);
      command.Parameters.AddWithValue(
         "default_lead_time_minutes",
         defaultLeadTimeMinutes
      );
      command.Parameters.AddWithValue("batch_size", batchSize);

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var notifications = new Dictionary<
         (Guid MemberId, Guid ActivityId),
         (MemberActivityPushNotification Notification,
            List<MemberPushSubscription> Subscriptions)
      >();

      while(await reader.ReadAsync(cancellationToken))
      {
         var key = (
            MemberId: reader.GetGuid(0),
            ActivityId: reader.GetGuid(1)
         );
         if(!notifications.TryGetValue(key, out var notificationData))
         {
            notificationData = (
               new MemberActivityPushNotification(
                  key.MemberId,
                  key.ActivityId,
                  reader.GetString(2),
                  reader.GetString(3),
                  reader.GetFieldValue<DateTimeOffset>(4),
                  reader.GetString(5),
                  reader.GetInt32(6),
                  reader.IsDBNull(7) ? null : reader.GetString(7),
                  []
               ),
               []
            );
            notifications[key] = notificationData;
         }

         notificationData.Subscriptions.Add(
            new MemberPushSubscription(
               reader.GetGuid(8),
               reader.GetString(9),
               reader.GetString(10),
               reader.GetString(11)
            )
         );
      }

      return notifications.Values
         .Select(item => item.Notification with
         {
            Subscriptions = item.Subscriptions
         })
         .ToArray();
   }

   public async Task MarkNotificationSentAsync(
      Guid memberId,
      Guid activityId,
      DateTimeOffset sentAt,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update member_activity_push_notifications
         set
            sent_at = @sent_at,
            claimed_at = null,
            updated_at = @sent_at
         where member_id = @member_id
            and activity_id = @activity_id
            and sent_at is null
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("member_id", memberId);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("sent_at", sentAt);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task ReleaseNotificationClaimAsync(
      Guid memberId,
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update member_activity_push_notifications
         set
            claimed_at = null,
            updated_at = now()
         where member_id = @member_id
            and activity_id = @activity_id
            and sent_at is null
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("member_id", memberId);
      command.Parameters.AddWithValue("activity_id", activityId);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }
}
