using Npgsql;

using SESport.Data.Repositories;

namespace SESport.Core.Tests.Data;

public sealed class AdminMemberRepositoryTests
{
   [Fact]
   public async Task GetMembersAsyncReturnsMemberInformationAndWatchCounts()
   {
      var memberWithoutWatchesId = Guid.NewGuid();
      var memberWithWatchesId = Guid.NewGuid();
      var firstEntityId = Guid.NewGuid();
      var secondEntityId = Guid.NewGuid();
      var sentActivityId = Guid.NewGuid();
      var unsentActivityId = Guid.NewGuid();
      var createdAt = new DateTimeOffset(
         2026,
         8,
         20,
         10,
         15,
         0,
         TimeSpan.Zero
      );
      var lastLoginAt = createdAt.AddDays(1);
      var emailWithoutWatches =
         $"admin-member-a-{memberWithoutWatchesId:N}@example.test";
      var emailWithWatches =
         $"admin-member-b-{memberWithWatchesId:N}@example.test";

      await using var dataSource = CreateDataSource();
      var repository = new AdminMemberRepository(dataSource);

      await InsertMemberAsync(
         dataSource,
         memberWithoutWatchesId,
         emailWithoutWatches,
         createdAt,
         null
      );
      await InsertMemberAsync(
         dataSource,
         memberWithWatchesId,
         emailWithWatches,
         createdAt.AddDays(1),
         lastLoginAt
      );
      await InsertPersonAsync(dataSource, firstEntityId);
      await InsertPersonAsync(dataSource, secondEntityId);
      await InsertActivityAsync(dataSource, sentActivityId, createdAt);
      await InsertActivityAsync(dataSource, unsentActivityId, createdAt);
      await InsertWatchAsync(
         dataSource,
         memberWithWatchesId,
         firstEntityId
      );
      await InsertWatchAsync(
         dataSource,
         memberWithWatchesId,
         secondEntityId
      );
      await InsertLoginTokenAsync(
         dataSource,
         memberWithWatchesId,
         createdAt,
         1,
         createdAt.AddMinutes(1)
      );
      await InsertLoginTokenAsync(
         dataSource,
         memberWithWatchesId,
         createdAt.AddMinutes(2),
         2,
         createdAt.AddMinutes(3)
      );
      await InsertLoginTokenAsync(
         dataSource,
         memberWithWatchesId,
         createdAt.AddMinutes(4),
         3,
         null
      );
      await InsertPushNotificationAsync(
         dataSource,
         memberWithWatchesId,
         sentActivityId,
         createdAt.AddHours(1),
         createdAt.AddHours(1).AddMinutes(1)
      );
      await InsertPushNotificationAsync(
         dataSource,
         memberWithWatchesId,
         unsentActivityId,
         createdAt.AddHours(2),
         null
      );

      try
      {
         var members = await repository.GetMembersAsync(
            CancellationToken.None
         );

         var memberWithoutWatches = Assert.Single(
            members,
            member => member.Id == memberWithoutWatchesId
         );
         var memberWithWatches = Assert.Single(
            members,
            member => member.Id == memberWithWatchesId
         );

         Assert.Equal(emailWithoutWatches, memberWithoutWatches.Email);
         Assert.Equal(createdAt, memberWithoutWatches.CreatedAt);
         Assert.Null(memberWithoutWatches.LastLoginAt);
         Assert.Equal(0, memberWithoutWatches.WatchCount);
         Assert.Equal(emailWithWatches, memberWithWatches.Email);
         Assert.Equal(createdAt.AddDays(1), memberWithWatches.CreatedAt);
         Assert.Equal(lastLoginAt, memberWithWatches.LastLoginAt);
         Assert.Equal(2, memberWithWatches.WatchCount);
         Assert.Equal(1, memberWithWatches.PushNotificationSentCount);
         Assert.Equal(3, memberWithWatches.LoginTokenCreatedCount);
         Assert.Equal(2, memberWithWatches.LoginTokenConsumedCount);
      }
      finally
      {
         await DeleteTestDataAsync(
            dataSource,
            [memberWithoutWatchesId, memberWithWatchesId],
            [firstEntityId, secondEntityId],
            [sentActivityId, unsentActivityId]
         );
      }
   }

   private static async Task InsertMemberAsync(
      NpgsqlDataSource dataSource,
      Guid memberId,
      string email,
      DateTimeOffset createdAt,
      DateTimeOffset? lastLoginAt
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         insert into members (
            id,
            email,
            email_normalized,
            created_at,
            updated_at,
            last_login_at
         )
         values (
            @id,
            @email,
            @email_normalized,
            @created_at,
            @updated_at,
            @last_login_at
         )
         """
      );
      command.Parameters.AddWithValue("id", memberId);
      command.Parameters.AddWithValue("email", email);
      command.Parameters.AddWithValue(
         "email_normalized",
         email.ToLowerInvariant()
      );
      command.Parameters.AddWithValue("created_at", createdAt);
      command.Parameters.AddWithValue("updated_at", createdAt);
      command.Parameters.AddWithValue(
         "last_login_at",
         (object?)lastLoginAt ?? DBNull.Value
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertPersonAsync(
      NpgsqlDataSource dataSource,
      Guid entityId
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
            'Person',
            'football',
            @country_id,
            'NationalityOrSportingIdentity',
            'Admin member repository test',
            'tier_3',
            'short_term'
         )
         """
      );
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue(
         "canonical_name",
         $"Admin member test person {entityId:N}"
      );
      command.Parameters.AddWithValue("country_id", PrimaryCountry.Id);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertWatchAsync(
      NpgsqlDataSource dataSource,
      Guid memberId,
      Guid entityId
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         insert into member_entity_watches (
            member_id,
            entity_id
         )
         values (
            @member_id,
            @entity_id
         )
         """
      );
      command.Parameters.AddWithValue("member_id", memberId);
      command.Parameters.AddWithValue("entity_id", entityId);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertLoginTokenAsync(
      NpgsqlDataSource dataSource,
      Guid memberId,
      DateTimeOffset requestedAt,
      int sequence,
      DateTimeOffset? consumedAt
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         insert into member_login_tokens (
            id,
            member_id,
            token_hash,
            requested_at,
            expires_at,
            consumed_at
         )
         values (
            @id,
            @member_id,
            @token_hash,
            @requested_at,
            @expires_at,
            @consumed_at
         )
         """
      );
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("member_id", memberId);
      command.Parameters.AddWithValue(
         "token_hash",
         $"admin-member-test-token-{memberId:N}-{sequence}"
      );
      command.Parameters.AddWithValue("requested_at", requestedAt);
      command.Parameters.AddWithValue(
         "expires_at",
         requestedAt.AddHours(1)
      );
      command.Parameters.AddWithValue(
         "consumed_at",
         (object?)consumedAt ?? DBNull.Value
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertPushNotificationAsync(
      NpgsqlDataSource dataSource,
      Guid memberId,
      Guid activityId,
      DateTimeOffset scheduledAt,
      DateTimeOffset? sentAt
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         insert into member_activity_push_notifications (
            member_id,
            activity_id,
            scheduled_at,
            sent_at
         )
         values (
            @member_id,
            @activity_id,
            @scheduled_at,
            @sent_at
         )
         """
      );
      command.Parameters.AddWithValue("member_id", memberId);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("scheduled_at", scheduledAt);
      command.Parameters.AddWithValue(
         "sent_at",
         (object?)sentAt ?? DBNull.Value
      );
      await command.ExecuteNonQueryAsync();
   }

   private static async Task InsertActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityId,
      DateTimeOffset startsAt
   )
   {
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
            published_at
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
            @published_at
         )
         """
      );
      command.Parameters.AddWithValue("id", activityId);
      command.Parameters.AddWithValue(
         "title",
         $"Admin member test activity {activityId:N}"
      );
      command.Parameters.AddWithValue(
         "activity_date",
         DateOnly.FromDateTime(startsAt.UtcDateTime)
      );
      command.Parameters.AddWithValue(
         "local_start_time",
         TimeOnly.FromDateTime(startsAt.UtcDateTime)
      );
      command.Parameters.AddWithValue("starts_at", startsAt);
      command.Parameters.AddWithValue(
         "publication_status_id",
         ActivityPublicationStatusIds.Published
      );
      command.Parameters.AddWithValue("slug", $"admin-member-{activityId:N}");
      command.Parameters.AddWithValue("published_at", startsAt);
      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteTestDataAsync(
      NpgsqlDataSource dataSource,
      IReadOnlyCollection<Guid> memberIds,
      IReadOnlyCollection<Guid> entityIds,
      IReadOnlyCollection<Guid> activityIds
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         delete from members
         where id = any(@member_ids);
         delete from activities
         where id = any(@activity_ids);
         delete from entities
         where id = any(@entity_ids);
         """
      );
      command.Parameters.AddWithValue("member_ids", memberIds.ToArray());
      command.Parameters.AddWithValue("entity_ids", entityIds.ToArray());
      command.Parameters.AddWithValue("activity_ids", activityIds.ToArray());
      await command.ExecuteNonQueryAsync();
   }
}
