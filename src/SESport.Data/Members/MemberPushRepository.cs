using Npgsql;

using SESport.Core.Domain;
using SESport.Data.Models;

namespace SESport.Data.Members;

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
         with watched_activities as (
            select
               member.id as member_id,
               watch.entity_id as watched_person_id,
               person.canonical_name as person_name,
               activity.id as activity_id,
               activity.activity_group_id,
               activity.activity_date,
               activity.activity_type_id,
               activity.sport_id,
               activity.title,
               coalesce(title_base.title, activity.title)
                  as notification_title,
               activity.starts_at,
               activity.ends_at,
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
            left join lateral (
               select base_activity.title
               from activities base_activity
               where activity.activity_group_id is not null
                  and base_activity.activity_group_id =
                     activity.activity_group_id
                  and base_activity.activity_date = activity.activity_date
                  and base_activity.activity_type_id =
                     activity.activity_type_id
                  and base_activity.sport_id = activity.sport_id
                  and base_activity.publication_status_id =
                     '{{ActivityPublicationStatusIds.Published}}'
                  and (
                     left(
                        activity.title,
                        length(base_activity.title) + 3
                     ) = base_activity.title || ' – '
                     or left(
                        activity.title,
                        length(base_activity.title) + 3
                     ) = base_activity.title || ' — '
                     or left(
                        activity.title,
                        length(base_activity.title) + 3
                     ) = base_activity.title || ' - '
                  )
               order by length(base_activity.title) desc,
                  base_activity.title
               limit 1
            ) title_base on true
            where activity.publication_status_id =
               '{{ActivityPublicationStatusIds.Published}}'
               and activity.starts_at is not null
               and person.entity_type_id =
                  '{{TrackedEntityTypeIds.Person}}'
               and coalesce(
                  member.push_notification_lead_time_minutes,
                  @default_lead_time_minutes
               ) > 0
               and exists (
                  select 1
                  from member_push_subscriptions subscription
                  where subscription.member_id = member.id
                     and (
                        subscription.expiration_at is null
                        or subscription.expiration_at > @now
                     )
               )
         ), eligible_activities as (
            select watched.*
            from watched_activities watched
            where watched.starts_at > @now
               and not (
                  watched.activity_type_id = 'Event'
                  and watched.activity_group_id is not null
                  and exists (
                     select 1
                     from watched_activities detailed
                     where detailed.member_id = watched.member_id
                        and detailed.watched_person_id =
                           watched.watched_person_id
                        and detailed.activity_group_id =
                           watched.activity_group_id
                        and detailed.activity_date = watched.activity_date
                        and detailed.activity_type_id <> 'Event'
                        and detailed.starts_at >= watched.starts_at
                        and (
                           watched.ends_at is null
                           or (
                              detailed.ends_at is not null
                              and detailed.ends_at <= watched.ends_at
                           )
                        )
                  )
               )
         ), logical_notifications as (
            select
               eligible.member_id,
               coalesce(
                  eligible.activity_group_id,
                  eligible.activity_id
               ) as notification_group_id,
               eligible.activity_date,
               eligible.activity_type_id,
               eligible.sport_id,
               eligible.notification_title as title,
               min(eligible.starts_at) as starts_at,
               eligible.public_date_mode,
               eligible.lead_time_minutes,
               (
                  array_agg(
                     eligible.activity_id
                     order by
                        (eligible.title <> eligible.notification_title),
                        eligible.starts_at,
                        eligible.activity_id
                  )
               )[1] as activity_id,
               array_agg(
                  distinct eligible.activity_id
                  order by eligible.activity_id
               ) as activity_ids,
               string_agg(
                  distinct eligible.person_name,
                  ', ' order by eligible.person_name
               ) as person_names,
               string_agg(
                  distinct nullif(btrim(channel.channel_name), ''),
                  ', ' order by nullif(btrim(channel.channel_name), '')
               ) as tv_channel_name
            from eligible_activities eligible
            left join lateral unnest(
               string_to_array(eligible.tv_channel_name, ',')
            ) channel(channel_name) on true
            group by
               eligible.member_id,
               coalesce(
                  eligible.activity_group_id,
                  eligible.activity_id
               ),
               eligible.activity_date,
               eligible.activity_type_id,
               eligible.sport_id,
               eligible.notification_title,
               eligible.public_date_mode,
               eligible.lead_time_minutes
         ), notifications_with_history as (
            select
               logical.*,
               array(
                  select distinct broad.activity_id
                  from watched_activities broad
                  where broad.member_id = logical.member_id
                     and broad.activity_group_id =
                        logical.notification_group_id
                     and broad.activity_date = logical.activity_date
                     and broad.activity_type_id = 'Event'
                     and exists (
                        select 1
                        from watched_activities detailed
                        where detailed.member_id = broad.member_id
                           and detailed.watched_person_id =
                              broad.watched_person_id
                           and detailed.activity_group_id =
                              broad.activity_group_id
                           and detailed.activity_date = broad.activity_date
                           and detailed.activity_type_id <> 'Event'
                           and detailed.activity_id = any(
                              logical.activity_ids
                           )
                           and detailed.starts_at >= broad.starts_at
                           and (
                              broad.ends_at is null
                              or (
                                 detailed.ends_at is not null
                                 and detailed.ends_at <= broad.ends_at
                              )
                           )
                     )
               ) as broad_activity_ids
            from logical_notifications logical
         ), candidates as (
            select
               notifications_with_history.*,
               notifications_with_history.starts_at - (
                  notifications_with_history.lead_time_minutes::double
                  precision * interval '1 minute'
               ) as scheduled_at
            from notifications_with_history
            where notifications_with_history.starts_at - (
                  notifications_with_history.lead_time_minutes::double
                  precision * interval '1 minute'
               ) <= @now
               and not exists (
                  select 1
                  from member_activity_push_notifications notification
                  where notification.member_id =
                     notifications_with_history.member_id
                     and notification.activity_id = any(
                        notifications_with_history.activity_ids ||
                        notifications_with_history.broad_activity_ids
                     )
                     and (
                        notification.sent_at is not null
                        or notification.claimed_at is null
                        or notification.claimed_at >= @claim_before
                     )
               )
            order by
               scheduled_at,
               notifications_with_history.member_id,
               notifications_with_history.activity_id
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
