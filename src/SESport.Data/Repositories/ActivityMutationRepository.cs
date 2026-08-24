using Npgsql;
using NpgsqlTypes;
using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Core.Sources;
using SESport.Data.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SESport.Data.Repositories;

public sealed class ActivityMutationRepository(NpgsqlDataSource dataSource)
{
   public async Task<bool> UpdateActivityGroupAsync(
      ActivityGroupEditModel model,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update activity_groups
         set title = @title,
            sport_id = @sport_id,
            no_grouping = @no_grouping,
            public_date_mode = @public_date_mode,
            updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", model.Id);
      command.Parameters.AddWithValue("title", model.Title.Trim());
      command.Parameters.AddWithValue("sport_id", model.SportId);
      command.Parameters.AddWithValue(
         "no_grouping",
         model.NoGrouping
      );
      command.Parameters.AddWithValue(
         "public_date_mode",
         model.PublicDateMode
      );

      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }

   public async Task<Guid> SaveAsync(
      ActivityEditModel model,
      CancellationToken cancellationToken
   )
   {
      var id = model.Id ?? Guid.NewGuid();
      var status = model.IsPublished
         ? ActivityPublicationStatusIds.Published
         : ActivityPublicationStatusIds.Draft;
      var startsAt = GetStartsAt(model);
      var endsAt = GetEndsAt(model);

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );
      var previousActivityGroupId = model.Id is null
         ? null
         : await GetActivityGroupIdAsync(
            connection,
            transaction,
            id,
            cancellationToken
         );

      var slug = await CreateSlugAsync(
         connection,
         transaction,
         model,
         id,
         cancellationToken
      );

      await EnsureActivityGroupAsync(
         connection,
         transaction,
         model,
         cancellationToken
      );

      if(model.Id is null)
      {
         await InsertActivityAsync(
            connection,
            transaction,
            id,
            model,
            startsAt,
            endsAt,
            status,
            slug,
            cancellationToken
         );
      }
      else
      {
         await UpdateActivityAsync(
            connection,
            transaction,
            id,
            model,
            startsAt,
            endsAt,
            status,
            slug,
            cancellationToken
         );
      }

      await ReplaceEntityLinkAsync(
         connection,
         transaction,
         id,
         model.LinkedEntityIds,
         model.OrganizationEntityId,
         cancellationToken
      );
      await ReplaceSourcesAsync(
         connection,
         transaction,
         id,
         model,
         cancellationToken
      );
      await AddBroadcastLinksAsync(
         connection,
         transaction,
         id,
         model.BroadcastIds,
         cancellationToken
      );
      await SynchronizeActivityGroupDatesAsync(
         connection,
         transaction,
         [
            previousActivityGroupId,
            model.ActivityGroupId
         ],
         cancellationToken
      );
      await transaction.CommitAsync(cancellationToken);
      return id;
   }

   private static async Task AddBroadcastLinksAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      IReadOnlyCollection<Guid> broadcastIds,
      CancellationToken cancellationToken
   )
   {
      if(broadcastIds.Count == 0)
      {
         return;
      }

      const string sql = """
         insert into activity_broadcast_links (
            activity_id,
            broadcast_id
         )
         select @activity_id, broadcast_id
         from unnest(@broadcast_ids) as broadcast_id
         on conflict (activity_id, broadcast_id) do nothing
         """;

      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue(
         "broadcast_ids",
         broadcastIds.Distinct().ToArray()
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task DeleteAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );
      var activityGroupId = await GetActivityGroupIdAsync(
         connection,
         transaction,
         id,
         cancellationToken
      );

      await using(var sourceCommand = new NpgsqlCommand(
         """
         delete from sources
         where correlation_type = @correlation_type
            and correlation_id = @correlation_id
         """,
         connection,
         transaction
      ))
      {
         sourceCommand.Parameters.AddWithValue(
            "correlation_type",
            SourceCorrelationTypes.Activity
         );
         sourceCommand.Parameters.AddWithValue(
            "correlation_id",
            id.ToString()
         );
         await sourceCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      await using(var linkCommand = new NpgsqlCommand(
         "delete from activity_entity_links where activity_id = @activity_id",
         connection,
         transaction
      ))
      {
         linkCommand.Parameters.AddWithValue("activity_id", id);
         await linkCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      await using(var activityCommand = new NpgsqlCommand(
         "delete from activities where id = @id",
         connection,
         transaction
      ))
      {
         activityCommand.Parameters.AddWithValue("id", id);
         await activityCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      await SynchronizeActivityGroupDatesAsync(
         connection,
         transaction,
         [activityGroupId],
         cancellationToken
      );
      await transaction.CommitAsync(cancellationToken);
   }

   public async Task<bool> UpdateTeaserAsync(
      Guid id,
      string teaser,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update activities
         set
            teaser = @teaser,
            updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("teaser", teaser.Trim());
      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }

   public async Task<bool> UpdateEmptyTeaserAsync(
      Guid id,
      string teaser,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update activities
         set
            teaser = @teaser,
            updated_at = now()
         where id = @id
            and coalesce(teaser, '') = ''
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("teaser", teaser.Trim());
      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }
   private static async Task InsertActivityAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid id,
      ActivityEditModel model,
      DateTimeOffset? startsAt,
      DateTimeOffset? endsAt,
      string status,
      string slug,
      CancellationToken cancellationToken
   )
   {
      const string sql = $$"""
         insert into activities (
            id,
            title,
            description,
            teaser,
            activity_type_id,
            sport_id,
            activity_date,
            local_start_time,
            starts_at,
            local_end_time,
            ends_at,
            time_zone_id,
            publication_status_id,
            tv_channel_name,
            activity_group_id,
            organization_entity_id,
            slug,
            published_at
         )
         values (
            @id,
            @title,
            @description,
            @teaser,
            @activity_type_id,
            @sport_id,
            @activity_date,
            @local_start_time,
            @starts_at,
            @local_end_time,
            @ends_at,
            @time_zone_id,
            @publication_status_id,
            @tv_channel_name,
            @activity_group_id,
            @organization_entity_id,
            @slug,
            case
               when @publication_status_id =
                  '{{ActivityPublicationStatusIds.Published}}' then now()
               else null
            end
         )
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      AddActivityParameters(
         command,
         id,
         model,
         startsAt,
         endsAt,
         status,
         slug
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task UpdateActivityAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid id,
      ActivityEditModel model,
      DateTimeOffset? startsAt,
      DateTimeOffset? endsAt,
      string status,
      string slug,
      CancellationToken cancellationToken
   )
   {
      const string sql = $$"""
         update activities
         set
            title = @title,
            description = @description,
            teaser = @teaser,
            activity_type_id = @activity_type_id,
            sport_id = @sport_id,
            activity_date = @activity_date,
            local_start_time = @local_start_time,
            starts_at = @starts_at,
            local_end_time = @local_end_time,
            ends_at = @ends_at,
            time_zone_id = @time_zone_id,
            publication_status_id = @publication_status_id,
            tv_channel_name = @tv_channel_name,
            activity_group_id = @activity_group_id,
            organization_entity_id = @organization_entity_id,
            slug = @slug,
            published_at = case
               when @publication_status_id =
                  '{{ActivityPublicationStatusIds.Published}}' then coalesce(
                  published_at,
                  now()
               )
               else null
            end,
            updated_at = now()
         where id = @id
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      AddActivityParameters(
         command,
         id,
         model,
         startsAt,
         endsAt,
         status,
         slug
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task ReplaceEntityLinkAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      IEnumerable<Guid> entityIds,
      Guid? organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      var existingEntityIds = new HashSet<Guid>();
      await using(var existingCommand = new NpgsqlCommand(
         """
         select entity_id
         from activity_entity_links
         where activity_id = @activity_id
         """,
         connection,
         transaction
      ))
      {
         existingCommand.Parameters.AddWithValue("activity_id", activityId);
         await using var reader = await existingCommand.ExecuteReaderAsync(
            cancellationToken
         );

         while(await reader.ReadAsync(cancellationToken))
         {
            existingEntityIds.Add(reader.GetGuid(0));
         }
      }

      var distinctEntityIds = entityIds
         .Where(entityId => entityId != Guid.Empty)
         .Distinct()
         .ToList();

      var removedEntityIds = existingEntityIds
         .Except(distinctEntityIds)
         .ToArray();

      if(removedEntityIds.Length > 0)
      {
         await using var deleteCommand = new NpgsqlCommand(
            """
            delete from activity_entity_links
            where activity_id = @activity_id
               and entity_id = any(@entity_ids)
            """,
            connection,
            transaction
         );
         deleteCommand.Parameters.AddWithValue("activity_id", activityId);
         deleteCommand.Parameters.AddWithValue(
            "entity_ids",
            removedEntityIds
         );
         await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      var retainedEntityIds = distinctEntityIds
         .Where(existingEntityIds.Contains)
         .ToArray();

      if(retainedEntityIds.Length > 0)
      {
         const string updateSql = $$"""
            update activity_entity_links link
            set organization_entity_id = case
               when e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
               then @organization_entity_id
               else null
            end
            from entities e
            where link.activity_id = @activity_id
               and link.entity_id = e.id
               and link.entity_id = any(@entity_ids)
            """;

         await using var updateCommand = new NpgsqlCommand(
            updateSql,
            connection,
            transaction
         );
         updateCommand.Parameters.AddWithValue("activity_id", activityId);
         updateCommand.Parameters.AddWithValue(
            "entity_ids",
            retainedEntityIds
         );
         updateCommand.Parameters.Add(
            "organization_entity_id",
            NpgsqlDbType.Uuid
         ).Value = organizationEntityId ?? (object)DBNull.Value;
         await updateCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      const string insertSql = $$"""
         insert into activity_entity_links (
            id,
            activity_id,
            entity_id,
            organization_entity_id,
            represented_entity_id,
            is_active
         )
         values (
            @id,
            @activity_id,
            @entity_id,
            case
               when exists (
                  select 1
                  from entities e
                  where e.id = @entity_id
                     and e.entity_type_id =
                        '{{TrackedEntityTypeIds.Person}}'
               )
               then @organization_entity_id
               else null
            end,
            @represented_entity_id,
            true
         )
         """;

      foreach(var entityId in distinctEntityIds
         .Where(entityId => !existingEntityIds.Contains(entityId)))
      {
         var representedEntityId =
            await ActivityParticipantRepository.ResolveRepresentedEntityIdAsync(
               connection,
               transaction,
               entityId,
               organizationEntityId,
               cancellationToken
            );
         await using var command = new NpgsqlCommand(
            insertSql,
            connection,
            transaction
         );
         command.Parameters.AddWithValue("id", Guid.NewGuid());
         command.Parameters.AddWithValue("activity_id", activityId);
         command.Parameters.AddWithValue("entity_id", entityId);
         command.Parameters.Add(
            "organization_entity_id",
            NpgsqlDbType.Uuid
         ).Value = organizationEntityId ?? (object)DBNull.Value;
         command.Parameters.Add(
            "represented_entity_id",
            NpgsqlDbType.Uuid
         ).Value = representedEntityId ?? (object)DBNull.Value;
         await command.ExecuteNonQueryAsync(cancellationToken);
      }
   }

   private static async Task ReplaceSourcesAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      ActivityEditModel model,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into sources (
            id,
            correlation_type,
            correlation_id,
            kind,
            url,
            title,
            excerpt,
            observed_at
         )
         values (
            @id,
            @correlation_type,
            @correlation_id,
            @kind,
            @url,
            @title,
            @excerpt,
            @observed_at
         )
         """;

      foreach(var source in model.Sources)
      {
         if(string.IsNullOrWhiteSpace(source.Url))
         {
            continue;
         }

         await using var command = new NpgsqlCommand(
            sql,
            connection,
            transaction
         );
         command.Parameters.AddWithValue("id", source.Id ?? Guid.NewGuid());
         command.Parameters.AddWithValue(
            "correlation_type",
            SourceCorrelationTypes.Activity
         );
         command.Parameters.AddWithValue(
            "correlation_id",
            activityId.ToString()
         );
         command.Parameters.AddWithValue(
            "kind",
            string.IsNullOrWhiteSpace(source.Kind)
               ? SourceKinds.ActivityEvidence
               : source.Kind.Trim()
         );
         command.Parameters.AddWithValue("url", source.Url.Trim());
         command.Parameters.AddWithValue(
            "title",
            BlankToDbNull(source.Title)
         );
         command.Parameters.AddWithValue(
            "excerpt",
            BlankToDbNull(source.Excerpt)
         );
         command.Parameters.AddWithValue("observed_at", DateTimeOffset.UtcNow);
         await command.ExecuteNonQueryAsync(cancellationToken);
      }
   }

   private static async Task EnsureActivityGroupAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      ActivityEditModel model,
      CancellationToken cancellationToken
   )
   {
      if(model.ActivityGroupId is not null)
      {
         model.ActivityGroupCreationRequired = false;
         return;
      }

      if(!model.ActivityGroupCreationRequired)
      {
         return;
      }

      if(model.ActivityDate is null)
      {
         throw new InvalidOperationException(
            "Activity date is required to create an activity group."
         );
      }

      var activityGroupTitle = string.IsNullOrWhiteSpace(
         model.ActivityGroupTitle
      )
         ? model.Title.Trim()
         : model.ActivityGroupTitle.Trim();

      var activityGroupId = Guid.NewGuid();
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
            @title,
            @sport_id,
            @start_date,
            @end_date
         )
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", activityGroupId);
      command.Parameters.AddWithValue("title", activityGroupTitle);
      command.Parameters.AddWithValue("sport_id", model.SportId.Trim());
      command.Parameters.AddWithValue("start_date", model.ActivityDate.Value);
      command.Parameters.AddWithValue("end_date", model.ActivityDate.Value);

      await command.ExecuteNonQueryAsync(cancellationToken);
      model.ActivityGroupId = activityGroupId;
      model.ActivityGroupCreationRequired = false;
   }

   private static async Task<Guid?> GetActivityGroupIdAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select activity_group_id
         from activities
         where id = @activity_id
         """;

      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("activity_id", activityId);
      var result = await command.ExecuteScalarAsync(cancellationToken);

      return result is null || result is DBNull
         ? null
         : (Guid)result;
   }

   private static async Task SynchronizeActivityGroupDatesAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      IEnumerable<Guid?> activityGroupIds,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update activity_groups ag
         set start_date = dates.start_date,
            end_date = dates.end_date,
            updated_at = now()
         from (
            select
               min(activity_date) as start_date,
               max(activity_date) as end_date
            from activities
            where activity_group_id = @activity_group_id
         ) dates
         where ag.id = @activity_group_id
            and dates.start_date is not null
         """;

      foreach(var activityGroupId in activityGroupIds
         .Where(id => id is not null)
         .Select(id => id!.Value)
         .Distinct())
      {
         await using var command = new NpgsqlCommand(
            sql,
            connection,
            transaction
         );
         command.Parameters.AddWithValue(
            "activity_group_id",
            activityGroupId
         );
         await command.ExecuteNonQueryAsync(cancellationToken);
      }
   }

   private static void AddActivityParameters(
      NpgsqlCommand command,
      Guid id,
      ActivityEditModel model,
      DateTimeOffset? startsAt,
      DateTimeOffset? endsAt,
      string status,
      string slug
   )
   {
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("title", model.Title.Trim());
      command.Parameters.AddWithValue(
         "description",
         BlankToDbNull(model.Description)
      );
      command.Parameters.AddWithValue(
         "teaser",
         BlankToDbNull(model.Teaser)
      );
      command.Parameters.AddWithValue("activity_type_id", model.ActivityType);
      command.Parameters.AddWithValue("sport_id", model.SportId.Trim());
      command.Parameters.AddWithValue(
         "activity_date",
         model.ActivityDate!.Value
      );
      command.Parameters.AddWithValue(
         "local_start_time",
         model.LocalStartTime ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue(
         "starts_at",
         startsAt?.ToUniversalTime() ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue(
         "local_end_time",
         model.LocalEndTime ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue(
         "ends_at",
         endsAt?.ToUniversalTime() ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue("time_zone_id", model.TimeZoneId.Trim());
      command.Parameters.AddWithValue("publication_status_id", status);
      command.Parameters.AddWithValue(
         "tv_channel_name",
         BlankToDbNull(model.TvChannelName)
      );
      command.Parameters.AddWithValue(
         "activity_group_id",
         model.ActivityGroupId ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue(
         "organization_entity_id",
         model.OrganizationEntityId ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue("slug", slug);
   }

   private static DateTimeOffset? GetStartsAt(ActivityEditModel model)
   {
      if(model.ActivityDate is null || model.LocalStartTime is null)
      {
         return null;
      }

      return TimeZoneHelper.ToUtc(
         model.ActivityDate.Value,
         model.LocalStartTime.Value,
         model.TimeZoneId
      );
   }

   private static DateTimeOffset? GetEndsAt(ActivityEditModel model)
   {
      if(model.ActivityDate is null ||
         model.LocalStartTime is null ||
         model.LocalEndTime is null)
      {
         return null;
      }

      var endDate = model.ActivityDate.Value;

      if(model.LocalEndTime < model.LocalStartTime)
      {
         endDate = endDate.AddDays(1);
      }

      return TimeZoneHelper.ToUtc(
         endDate,
         model.LocalEndTime.Value,
         model.TimeZoneId
      );
   }

   private static async Task<string> CreateSlugAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      ActivityEditModel model,
      Guid id,
      CancellationToken cancellationToken
   )
   {
      var baseSlug = NormalizeSlug(
         model.Title,
         model.ActivityDate,
         model.ActivityType
      );

      var existingSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      const string sql = """
         select slug
         from activities
         where id <> @id
            and slug is not null
            and (slug = @base_slug or slug like @prefix)
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("base_slug", baseSlug);
      command.Parameters.AddWithValue("prefix", baseSlug + "-%");

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      while(await reader.ReadAsync(cancellationToken))
      {
         existingSlugs.Add(reader.GetString(0));
      }

      if(!existingSlugs.Contains(baseSlug))
      {
         return baseSlug;
      }

      for(var suffix = 2; ; suffix++)
      {
         var candidate = $"{baseSlug}-{suffix}";
         if(!existingSlugs.Contains(candidate))
         {
            return candidate;
         }
      }
   }

   private static string NormalizeSlug(
      string title,
      DateOnly? activityDate,
      string activityType
   )
   {
      var datePart = DateDisplay.Format(activityDate) ?? "undated";
      var slug = Slugify($"{datePart}-{title}-{activityType}");
      return string.IsNullOrWhiteSpace(slug) ? "activity" : slug;
   }

   private static string Slugify(string value)
   {
      var normalized = value.Normalize(NormalizationForm.FormD);
      var builder = new StringBuilder();

      foreach(var character in normalized)
      {
         var category = CharUnicodeInfo.GetUnicodeCategory(character);
         if(category != UnicodeCategory.NonSpacingMark)
         {
            builder.Append(character);
         }
      }

      return Regex.Replace(
            builder
               .ToString()
               .Normalize(NormalizationForm.FormC)
               .ToLowerInvariant(),
            "[^a-z0-9]+",
            "-"
         )
         .Trim('-');
   }

   private static object BlankToDbNull(string? value)
   {
      return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
   }
}
