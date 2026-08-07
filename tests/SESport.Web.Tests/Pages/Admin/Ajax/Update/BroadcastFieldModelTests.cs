using Microsoft.AspNetCore.Mvc;

using Npgsql;

using SESport.Core.Configuration;
using SESport.Core.Formatting;
using SESport.Data.Models;
using SESport.Data.Repositories;
using SESport.Web.Pages.Admin.Ajax.Update;

namespace SESport.Core.Tests.Pages.Admin.Ajax.Update;

public sealed class BroadcastFieldModelTests
{
   [Fact]
   public async Task OnPostAsyncUpdatesOrganizationAndRejectsInvalidType()
   {
      var broadcastId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var broadcastTitle = $"Broadcast {uniqueSuffix}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminBroadcastRepository(dataSource);
      var adminRepository = new AdminRepository(dataSource);
      var model = new BroadcastFieldModel(repository, adminRepository);

      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "channel-1",
         "Viaplay",
         broadcastTitle,
         ["Old", "Categories"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2)
      );
      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         $"Person {personId:N}",
         TrackedEntityTypeIds.Person,
         "football"
      );

      try
      {
         var updateResult = await model.OnPostAsync(
            broadcastId,
            "organization",
            organizationId.ToString(),
            CancellationToken.None
         );

         var updatePayload = Assert.IsType<JsonResult>(updateResult).Value!;
         var groupText = (string?)updatePayload.GetType()
            .GetProperty("groupText")
            ?.GetValue(updatePayload);

         Assert.Equal($"NEW: {broadcastTitle}", groupText);
         AssertBroadcastEntityId(
            dataSource,
            broadcastId,
            organizationId
         );

         var invalidResult = await model.OnPostAsync(
            broadcastId,
            "organization",
            personId.ToString(),
            CancellationToken.None
         );

         Assert.IsType<BadRequestObjectResult>(invalidResult);
         AssertBroadcastEntityId(
            dataSource,
            broadcastId,
            organizationId
         );

         var clearResult = await model.OnPostAsync(
            broadcastId,
            "organization",
            null,
            CancellationToken.None
         );

         Assert.IsType<JsonResult>(clearResult);
         AssertBroadcastEntityId(dataSource, broadcastId, null);
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task OnPostAsyncMatchesGroupUsingOrganizationSport()
   {
      var broadcastId = Guid.NewGuid();
      var activityGroupId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var title = $"Austrian Open {uniqueSuffix}";
      var activityDate = DistantActivityDate;

      await using var dataSource = CreateDataSource();
      var repository = new AdminBroadcastRepository(dataSource);
      var adminRepository = new AdminRepository(dataSource);
      var model = new BroadcastFieldModel(repository, adminRepository);

      await InsertActivityGroupAsync(
         dataSource,
         activityGroupId,
         title,
         "tennis",
         activityDate,
         activityDate
      );
      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "tennis"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         $"Person {personId:N}",
         TrackedEntityTypeIds.Person,
         "tennis"
      );

      var activityId = await InsertActivityAsync(
         dataSource,
         activityGroupId,
         title,
         "tennis",
         activityDate,
         organizationEntityId: organizationId,
         linkedEntityIds: [personId]
      );

      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "channel-1",
         "Viaplay",
         title,
         [],
         TimeZoneHelper.ToUtc(
            activityDate,
            new TimeOnly(12, 0),
            SportDay.TimeZoneId
         ),
         TimeZoneHelper.ToUtc(
            activityDate,
            new TimeOnly(14, 0),
            SportDay.TimeZoneId
         )
      );

      try
      {
         var updateResult = await model.OnPostAsync(
            broadcastId,
            "organization",
            organizationId.ToString(),
            CancellationToken.None
         );

         var updatePayload = Assert.IsType<JsonResult>(updateResult).Value!;
         var groupText = (string?)updatePayload.GetType()
            .GetProperty("groupText")
            ?.GetValue(updatePayload);
         var broadcast = await repository.GetByIdAsync(
            broadcastId,
            CancellationToken.None
         );

         Assert.Equal(title, groupText);
         Assert.NotNull(broadcast);
         Assert.Equal(activityGroupId, broadcast!.ActivityGroupId);
         Assert.Equal(activityId, broadcast.ActivityGroupSourceActivityId);
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteActivityGroupAsync(dataSource, activityGroupId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task OnPostAsyncSelectsExistingRelevantActivityGroup()
   {
      var broadcastId = Guid.NewGuid();
      var currentGroupId = Guid.NewGuid();
      var selectedGroupId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var broadcastTitle = $"Current group {uniqueSuffix}";
      var selectedTitle = $"Selected group {uniqueSuffix}";
      var activityDate = DistantActivityDate;

      await using var dataSource = CreateDataSource();
      var repository = new AdminBroadcastRepository(dataSource);
      var adminRepository = new AdminRepository(dataSource);
      var model = new BroadcastFieldModel(repository, adminRepository);
      Guid currentActivityId = Guid.Empty;
      Guid selectedActivityId = Guid.Empty;

      try
      {
         await InsertRelatedEntityAsync(
            dataSource,
            organizationId,
            $"Organization {organizationId:N}",
            TrackedEntityTypeIds.Organization,
            "tennis"
         );
         await InsertRelatedEntityAsync(
            dataSource,
            personId,
            $"Person {personId:N}",
            TrackedEntityTypeIds.Person,
            "tennis"
         );
         await InsertActivityGroupAsync(
            dataSource,
            currentGroupId,
            broadcastTitle,
            "tennis",
            activityDate,
            activityDate
         );
         await InsertActivityGroupAsync(
            dataSource,
            selectedGroupId,
            selectedTitle,
            "tennis",
            activityDate,
            activityDate
         );

         currentActivityId = await InsertActivityAsync(
            dataSource,
            currentGroupId,
            broadcastTitle,
            "tennis",
            activityDate,
            organizationEntityId: organizationId,
            linkedEntityIds: [personId]
         );
         selectedActivityId = await InsertActivityAsync(
            dataSource,
            selectedGroupId,
            selectedTitle,
            "tennis",
            activityDate,
            organizationEntityId: organizationId,
            linkedEntityIds: [personId]
         );
         await InsertBroadcastAsync(
            dataSource,
            broadcastId,
            sourceKey,
            $"external-{uniqueSuffix}",
            $"fingerprint-{uniqueSuffix}",
            "channel-1",
            "Viaplay",
            broadcastTitle,
            [],
            TimeZoneHelper.ToUtc(
               activityDate,
               new TimeOnly(12, 0),
               SportDay.TimeZoneId
            ),
            TimeZoneHelper.ToUtc(
               activityDate,
               new TimeOnly(14, 0),
               SportDay.TimeZoneId
            )
         );

         var organizationResult = await model.OnPostAsync(
            broadcastId,
            "organization",
            organizationId.ToString(),
            CancellationToken.None
         );

         Assert.IsType<JsonResult>(organizationResult);

         var updateResult = await model.OnPostAsync(
            broadcastId,
            "group",
            selectedTitle,
            CancellationToken.None,
            selectedGroupId
         );

         Assert.IsType<JsonResult>(updateResult);
         var broadcast = await repository.GetByIdAsync(
            broadcastId,
            CancellationToken.None
         );

         Assert.NotNull(broadcast);
         Assert.Equal(selectedGroupId, broadcast!.ActivityGroupId);
         Assert.Equal(
            selectedActivityId,
            broadcast.ActivityGroupSourceActivityId
         );
         Assert.Null(broadcast.ActivityGroupDraftTitle);
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteActivityAsync(dataSource, currentActivityId);
         await DeleteActivityAsync(dataSource, selectedActivityId);
         await DeleteActivityGroupAsync(dataSource, currentGroupId);
         await DeleteActivityGroupAsync(dataSource, selectedGroupId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task OnPostAsyncDoesNotMatchGroupWithoutOrgLink()
   {
      var broadcastId = Guid.NewGuid();
      var activityGroupId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var title = $"Austrian Open {uniqueSuffix}";
      var activityDate = DistantActivityDate;

      await using var dataSource = CreateDataSource();
      var repository = new AdminBroadcastRepository(dataSource);
      var adminRepository = new AdminRepository(dataSource);
      var model = new BroadcastFieldModel(repository, adminRepository);

      await InsertActivityGroupAsync(
         dataSource,
         activityGroupId,
         title,
         "tennis",
         activityDate,
         activityDate
      );

      var activityId = await InsertActivityAsync(
         dataSource,
         activityGroupId,
         title,
         "tennis",
         activityDate
      );

      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "channel-1",
         "Viaplay",
         title,
         ["tennis"],
         TimeZoneHelper.ToUtc(
            activityDate,
            new TimeOnly(12, 0),
            SportDay.TimeZoneId
         ),
         TimeZoneHelper.ToUtc(
            activityDate,
            new TimeOnly(14, 0),
            SportDay.TimeZoneId
         )
      );
      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "tennis"
      );

      try
      {
         var updateResult = await model.OnPostAsync(
            broadcastId,
            "organization",
            organizationId.ToString(),
            CancellationToken.None
         );

         var updatePayload = Assert.IsType<JsonResult>(updateResult).Value!;
         var groupText = (string?)updatePayload.GetType()
            .GetProperty("groupText")
            ?.GetValue(updatePayload);
         var broadcast = await repository.GetByIdAsync(
            broadcastId,
            CancellationToken.None
         );

         Assert.Equal($"NEW: {title}", groupText);
         Assert.NotNull(broadcast);
         Assert.Null(broadcast!.ActivityGroupId);
         Assert.Null(broadcast.ActivityGroupSourceActivityId);
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteActivityGroupAsync(dataSource, activityGroupId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task OnPostAsyncMatchesNearbyGroupWithDifferentTitle()
   {
      var broadcastId = Guid.NewGuid();
      var activityGroupId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var broadcastSuffix = Guid.NewGuid().ToString("N");
      var activitySuffix = Guid.NewGuid().ToString("N");
      var broadcastTitle = $"Austrian Open {broadcastSuffix}";
      var activityTitle = $"Nordea Open Båstad {activitySuffix}";
      var activityDate = DistantActivityDate;

      await using var dataSource = CreateDataSource();
      var repository = new AdminBroadcastRepository(dataSource);
      var adminRepository = new AdminRepository(dataSource);
      var model = new BroadcastFieldModel(repository, adminRepository);

      await InsertActivityGroupAsync(
         dataSource,
         activityGroupId,
         activityTitle,
         "tennis",
         activityDate,
         activityDate
      );
      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "tennis"
      );
      await InsertRelatedEntityAsync(
         dataSource,
         personId,
         $"Person {personId:N}",
         TrackedEntityTypeIds.Person,
         "tennis"
      );

      var activityId = await InsertActivityAsync(
         dataSource,
         activityGroupId,
         activityTitle,
         "tennis",
         activityDate,
         organizationEntityId: organizationId,
         linkedEntityIds: [personId]
      );

      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         $"external-{broadcastSuffix}",
         $"fingerprint-{broadcastSuffix}",
         "channel-1",
         "Viaplay",
         broadcastTitle,
         ["tennis"],
         TimeZoneHelper.ToUtc(
            activityDate,
            new TimeOnly(12, 0),
            SportDay.TimeZoneId
         ),
         TimeZoneHelper.ToUtc(
            activityDate,
            new TimeOnly(14, 0),
            SportDay.TimeZoneId
         )
      );

      try
      {
         var updateResult = await model.OnPostAsync(
            broadcastId,
            "organization",
            organizationId.ToString(),
            CancellationToken.None
         );

         var updatePayload = Assert.IsType<JsonResult>(updateResult).Value!;
         var groupText = (string?)updatePayload.GetType()
            .GetProperty("groupText")
            ?.GetValue(updatePayload);
         var broadcast = await repository.GetByIdAsync(
            broadcastId,
            CancellationToken.None
         );

         Assert.Equal(activityTitle, groupText);
         Assert.NotNull(broadcast);
         Assert.Equal(activityGroupId, broadcast!.ActivityGroupId);
         Assert.Equal(activityId, broadcast.ActivityGroupSourceActivityId);
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteActivityGroupAsync(dataSource, activityGroupId);
         await DeleteEntityAsync(dataSource, personId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task OnPostAsyncDoesNotRenameExistingActivityGroup()
   {
      var broadcastId = Guid.NewGuid();
      var activityGroupId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var broadcastTitle = $"Broadcast {uniqueSuffix}";
      var originalGroupTitle = $"Original group {uniqueSuffix}";
      var updatedGroupTitle = $"Updated group {uniqueSuffix}";
      var activityTitle = $"Activity {uniqueSuffix}";
      var activityDate = DistantActivityDate;

      await using var dataSource = CreateDataSource();
      var repository = new AdminBroadcastRepository(dataSource);
      var adminRepository = new AdminRepository(dataSource);
      var model = new BroadcastFieldModel(repository, adminRepository);

      await InsertActivityGroupAsync(
         dataSource,
         activityGroupId,
         originalGroupTitle,
         "football",
         activityDate,
         activityDate
      );

      var activityId = await InsertActivityAsync(
         dataSource,
         activityGroupId,
         activityTitle,
         "football",
         activityDate
      );

      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "channel-1",
         "Viaplay",
         broadcastTitle,
         ["Old", "Categories"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2),
         null,
         BroadcastActivitySourceKindIds.ActivityGroupForActivity,
         activityId
      );

      try
      {
         var updateResult = await model.OnPostAsync(
            broadcastId,
            "group",
            updatedGroupTitle,
            CancellationToken.None
         );

         var updatePayload = Assert.IsType<JsonResult>(updateResult).Value!;
         var groupText = (string?)updatePayload.GetType()
            .GetProperty("groupText")
            ?.GetValue(updatePayload);
         var groupValue = (string?)updatePayload.GetType()
            .GetProperty("groupValue")
            ?.GetValue(updatePayload);
         var returnedActivityGroupId = (string?)updatePayload.GetType()
            .GetProperty("activityGroupId")
            ?.GetValue(updatePayload);
         var returnedActivityGroupTitle = (string?)updatePayload.GetType()
            .GetProperty("activityGroupTitle")
            ?.GetValue(updatePayload);
         var returnedActivityGroupDraftTitle =
            (string?)updatePayload.GetType()
               .GetProperty("activityGroupDraftTitle")
               ?.GetValue(updatePayload);
         var returnedActivityGroupSourceKindId =
            (string?)updatePayload.GetType()
               .GetProperty("activityGroupSourceKindId")
               ?.GetValue(updatePayload);

         Assert.Equal($"NEW: {updatedGroupTitle}", groupText);
         Assert.Equal(updatedGroupTitle, groupValue);
         Assert.Equal(string.Empty, returnedActivityGroupId);
         Assert.Equal(string.Empty, returnedActivityGroupTitle);
         Assert.Equal(updatedGroupTitle, returnedActivityGroupDraftTitle);
         Assert.Equal(
            BroadcastActivitySourceKindIds.ActivityGroupForActivity,
            returnedActivityGroupSourceKindId
         );
         AssertBroadcastActivityGroupDraftTitle(
            dataSource,
            broadcastId,
            updatedGroupTitle
         );
         AssertActivityGroupTitle(
            dataSource,
            activityGroupId,
            originalGroupTitle
         );

         var broadcast = await repository.GetByIdAsync(
            broadcastId,
            CancellationToken.None
         );

         Assert.NotNull(broadcast);
         Assert.Null(broadcast!.ActivityGroupId);
         Assert.Null(broadcast.ActivityGroupSourceActivityId);
         Assert.Equal(
            BroadcastActivitySourceKindIds.ActivityGroupForActivity,
            broadcast.ActivityGroupSourceKindId
         );
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteActivityAsync(dataSource, activityId);
         await DeleteActivityGroupAsync(dataSource, activityGroupId);
      }
   }

   [Fact]
   public async Task OnPostAsyncUpdatesDraftActivityGroupTitle()
   {
      var broadcastId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var broadcastTitle = $"Broadcast {uniqueSuffix}";
      var updatedGroupTitle = $"Updated group {uniqueSuffix}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminBroadcastRepository(dataSource);
      var adminRepository = new AdminRepository(dataSource);
      var model = new BroadcastFieldModel(repository, adminRepository);

      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "channel-1",
         "Viaplay",
         broadcastTitle,
         ["Old", "Categories"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2)
      );
      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );

      try
      {
         var organizationResult = await model.OnPostAsync(
            broadcastId,
            "organization",
            organizationId.ToString(),
            CancellationToken.None
         );

         Assert.IsType<JsonResult>(organizationResult);

         var updateResult = await model.OnPostAsync(
            broadcastId,
            "group",
            updatedGroupTitle,
            CancellationToken.None
         );

         var updatePayload = Assert.IsType<JsonResult>(updateResult).Value!;
         var groupText = (string?)updatePayload.GetType()
            .GetProperty("groupText")
            ?.GetValue(updatePayload);
         var groupValue = (string?)updatePayload.GetType()
            .GetProperty("groupValue")
            ?.GetValue(updatePayload);
         var activityGroupIdValue = (string?)updatePayload.GetType()
            .GetProperty("activityGroupId")
            ?.GetValue(updatePayload);
         var activityGroupSourceKindId = (string?)updatePayload.GetType()
            .GetProperty("activityGroupSourceKindId")
            ?.GetValue(updatePayload);

         Assert.Equal($"NEW: {updatedGroupTitle}", groupText);
         Assert.Equal(updatedGroupTitle, groupValue);
         Assert.Equal(string.Empty, activityGroupIdValue);
         Assert.Equal(
            BroadcastActivitySourceKindIds.ActivityGroupForActivity,
            activityGroupSourceKindId
         );
         AssertBroadcastActivityGroupDraftTitle(
            dataSource,
            broadcastId,
            updatedGroupTitle
         );
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   [Fact]
   public async Task OnPostAsyncClearsDraftActivityGroupTitle()
   {
      var broadcastId = Guid.NewGuid();
      var organizationId = Guid.NewGuid();
      var sourceKey = $"test-source-{Guid.NewGuid():N}";
      var uniqueSuffix = Guid.NewGuid().ToString("N");
      var broadcastTitle = $"Broadcast {uniqueSuffix}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminBroadcastRepository(dataSource);
      var adminRepository = new AdminRepository(dataSource);
      var model = new BroadcastFieldModel(repository, adminRepository);

      await InsertBroadcastAsync(
         dataSource,
         broadcastId,
         sourceKey,
         $"external-{uniqueSuffix}",
         $"fingerprint-{uniqueSuffix}",
         "channel-1",
         "Viaplay",
         broadcastTitle,
         ["Old", "Categories"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow.AddHours(2)
      );
      await InsertRelatedEntityAsync(
         dataSource,
         organizationId,
         $"Organization {organizationId:N}",
         TrackedEntityTypeIds.Organization,
         "football"
      );

      try
      {
         var organizationResult = await model.OnPostAsync(
            broadcastId,
            "organization",
            organizationId.ToString(),
            CancellationToken.None
         );

         Assert.IsType<JsonResult>(organizationResult);

         var clearResult = await model.OnPostAsync(
            broadcastId,
            "group",
            string.Empty,
            CancellationToken.None
         );

         var clearPayload = Assert.IsType<JsonResult>(clearResult).Value!;
         var groupText = (string?)clearPayload.GetType()
            .GetProperty("groupText")
            ?.GetValue(clearPayload);
         var activityGroupId = (string?)clearPayload.GetType()
            .GetProperty("activityGroupId")
            ?.GetValue(clearPayload);
         var activityGroupTitle = (string?)clearPayload.GetType()
            .GetProperty("activityGroupTitle")
            ?.GetValue(clearPayload);
         var activityGroupDraftTitle = (string?)clearPayload.GetType()
            .GetProperty("activityGroupDraftTitle")
            ?.GetValue(clearPayload);
         var activityGroupSourceKindId = (string?)clearPayload.GetType()
            .GetProperty("activityGroupSourceKindId")
            ?.GetValue(clearPayload);

         Assert.Equal("-", groupText);
         Assert.Equal(string.Empty, activityGroupId);
         Assert.Equal(string.Empty, activityGroupTitle);
         Assert.Equal(string.Empty, activityGroupDraftTitle);
         Assert.Equal(string.Empty, activityGroupSourceKindId);

         var broadcast = await repository.GetByIdAsync(
            broadcastId,
            CancellationToken.None
         );

         Assert.NotNull(broadcast);
         Assert.Null(broadcast!.ActivityGroupId);
         Assert.Null(broadcast.ActivityGroupTitle);
         Assert.Null(broadcast.ActivityGroupDraftTitle);
         Assert.Null(broadcast.ActivityGroupSourceKindId);
         Assert.Null(broadcast.ActivityGroupSourceActivityId);
      }
      finally
      {
         await DeleteBroadcastAsync(dataSource, broadcastId);
         await DeleteEntityAsync(dataSource, organizationId);
      }
   }

   private static async Task InsertBroadcastAsync(
      NpgsqlDataSource dataSource,
      Guid broadcastId,
      string sourceKey,
      string externalId,
      string fingerprint,
      string channelId,
      string channelName,
      string title,
      string[] categories,
      DateTimeOffset startsAt,
      DateTimeOffset endsAt,
      string? activityGroupDraftTitle = null,
      string? activityGroupSourceKindId = null,
      Guid? activityGroupSourceActivityId = null
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into broadcasts (
            id,
            source_key,
            external_id,
            fingerprint,
            channel_id,
            channel_name,
            title,
            description,
            categories,
            is_replay,
            original_air_date,
            starts_at,
            ends_at,
            time_zone_id,
            activity_group_source_kind_id,
            activity_group_source_activity_id,
            activity_group_draft_title,
            raw_programme_xml
         )
         values (
            @id,
            @source_key,
            @external_id,
            @fingerprint,
            @channel_id,
            @channel_name,
            @title,
            null,
            @categories,
            false,
            null,
            @starts_at,
            @ends_at,
            'Europe/Stockholm',
            @activity_group_source_kind_id,
            @activity_group_source_activity_id,
            @activity_group_draft_title,
            null
         )
         """;
      command.Parameters.AddWithValue("id", broadcastId);
      command.Parameters.AddWithValue("source_key", sourceKey);
      command.Parameters.AddWithValue("external_id", externalId);
      command.Parameters.AddWithValue("fingerprint", fingerprint);
      command.Parameters.AddWithValue("channel_id", channelId);
      command.Parameters.AddWithValue("channel_name", channelName);
      command.Parameters.AddWithValue("title", title);
      command.Parameters.AddWithValue("categories", categories);
      command.Parameters.AddWithValue("starts_at", startsAt);
      command.Parameters.AddWithValue("ends_at", endsAt);
      command.Parameters.AddWithValue(
         "activity_group_source_kind_id",
         (object?)activityGroupSourceKindId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "activity_group_source_activity_id",
         (object?)activityGroupSourceActivityId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "activity_group_draft_title",
         (object?)activityGroupDraftTitle ?? DBNull.Value
      );

      await command.ExecuteNonQueryAsync();
   }

   private static async Task<Guid> InsertActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityGroupId,
      string title,
      string sportId,
      DateOnly activityDate,
      Guid? organizationEntityId = null,
      Guid[]? linkedEntityIds = null
   )
   {
      var repository = new ActivityRepository(dataSource);

      return await repository.SaveAsync(
         new ActivityEditModel
         {
            Title = title,
            ActivityType = ActivityType.Match.ToString(),
            SportId = sportId,
            ActivityDate = activityDate,
            LocalStartTime = new TimeOnly(12, 0),
            TimeZoneId = SportDay.TimeZoneId,
            LinkedEntityIds = linkedEntityIds is null
               ? []
               : [.. linkedEntityIds],
            OrganizationEntityId = organizationEntityId,
            ActivityGroupId = activityGroupId
         },
         CancellationToken.None
      );
   }

   private static async Task InsertActivityGroupAsync(
      NpgsqlDataSource dataSource,
      Guid activityGroupId,
      string title,
      string sportId,
      DateOnly startDate,
      DateOnly endDate
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         insert into activity_groups (
            id,
            title,
            sport_id,
            start_date,
            end_date
         )
         values (
            @id,
            @title,
            @sport_id,
            @start_date,
            @end_date
         )
         """;
      command.Parameters.AddWithValue("id", activityGroupId);
      command.Parameters.AddWithValue("title", title);
      command.Parameters.AddWithValue("sport_id", sportId);
      command.Parameters.AddWithValue("start_date", startDate);
      command.Parameters.AddWithValue("end_date", endDate);

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
            @country_id,
            'NationalityOrSportingIdentity',
            'Test coverage',
            'tier_3',
            'short_term'
         )
         """;
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("canonical_name", entityName);
      command.Parameters.AddWithValue("country_id", PrimaryCountry.Id);
      command.Parameters.AddWithValue("entity_type_id", entityTypeId);
      command.Parameters.AddWithValue("sport_id", sportId);

      await command.ExecuteNonQueryAsync();
   }

   private static void AssertBroadcastEntityId(
      NpgsqlDataSource dataSource,
      Guid broadcastId,
      Guid? expectedEntityId
   )
   {
      using var connection = dataSource.OpenConnection();
      using var command = connection.CreateCommand();
      command.CommandText = """
         select entity_id
         from broadcasts
         where id = @id
         """;
      command.Parameters.AddWithValue("id", broadcastId);

      var actualValue = command.ExecuteScalar();

      if(expectedEntityId is null)
      {
         Assert.True(actualValue is null || actualValue is DBNull);
         return;
      }

      Assert.Equal(
         expectedEntityId,
         actualValue is Guid actualGuid ? actualGuid : null
      );
   }

   private static void AssertActivityGroupTitle(
      NpgsqlDataSource dataSource,
      Guid activityGroupId,
      string expectedTitle
   )
   {
      using var connection = dataSource.OpenConnection();
      using var command = connection.CreateCommand();
      command.CommandText = """
         select title
         from activity_groups
         where id = @id
         """;
      command.Parameters.AddWithValue("id", activityGroupId);

      var actualValue = command.ExecuteScalar();
      Assert.Equal(expectedTitle, actualValue as string);
   }

   private static void AssertBroadcastActivityGroupDraftTitle(
      NpgsqlDataSource dataSource,
      Guid broadcastId,
      string expectedTitle
   )
   {
      using var connection = dataSource.OpenConnection();
      using var command = connection.CreateCommand();
      command.CommandText = """
         select activity_group_draft_title
         from broadcasts
         where id = @id
         """;
      command.Parameters.AddWithValue("id", broadcastId);

      var actualValue = command.ExecuteScalar();
      Assert.Equal(expectedTitle, actualValue as string);
   }

   private static async Task DeleteActivityAsync(
      NpgsqlDataSource dataSource,
      Guid activityId
   )
   {
      var repository = new ActivityRepository(dataSource);
      await repository.DeleteAsync(activityId, CancellationToken.None);
   }

   private static async Task DeleteActivityGroupAsync(
      NpgsqlDataSource dataSource,
      Guid activityGroupId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from activity_groups
         where id = @id
         """;
      command.Parameters.AddWithValue("id", activityGroupId);

      await command.ExecuteNonQueryAsync();
   }

   private static async Task DeleteBroadcastAsync(
      NpgsqlDataSource dataSource,
      Guid broadcastId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = """
         delete from broadcasts
         where id = @id
         """;
      command.Parameters.AddWithValue("id", broadcastId);

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
}
