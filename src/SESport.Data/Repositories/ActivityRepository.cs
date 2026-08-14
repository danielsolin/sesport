using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Npgsql;

using NpgsqlTypes;

using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class ActivityRepository(NpgsqlDataSource dataSource)
{
   private const string TimedOrderClause = """
      order by
         a.starts_at nulls last,
         a.activity_date,
         a.local_start_time nulls last,
         a.title
      """;

   private const string DefaultOrderClause = """
      order by
         a.activity_date,
         a.local_start_time nulls last,
         a.title
      """;

   private const string TestActivityTitle = "Test Activity";
   private const string TestActivitySlugPattern = "test-activity-%";

   private const string PublicActivityExclusionClause = """
      and not (
         (
            a.title = @test_activity_title
            or coalesce(a.slug, '') like @test_activity_slug_pattern
         )
         and a.published_at is null
      )
      """;

   private static string BuildTimedDateFilterSql()
   {
      return $$"""
         (
            (
               coalesce(
                  ag.public_date_mode,
                  '{{ActivityGroupPublicDateModeIds.SportDay}}'
               ) = '{{ActivityGroupPublicDateModeIds.SportDay}}'
               and a.starts_at >= @start
               and a.starts_at < @end
            )
            or (
               coalesce(
                  ag.public_date_mode,
                  '{{ActivityGroupPublicDateModeIds.SportDay}}'
               ) = '{{ActivityGroupPublicDateModeIds.LocalCalendarDate}}'
               and (a.starts_at at time zone @time_zone)::date = @date
            )
         )
         """;
   }

   public async Task<IReadOnlyList<ActivityListItem>> GetActivitiesAsync(
      DateOnly date,
      string? status,
      IReadOnlyCollection<string> sportIds,
      CancellationToken cancellationToken
   )
   {
      var normalizedSports = SportFilter.Normalize(sportIds);
      // Timed rows follow their group's public date mode.
      var window = SportDay.ForDate(date);
      var start = ToUtc(window.StartDate, window.Cutoff);
      var end = ToUtc(window.EndDateExclusive, window.Cutoff);
      var whereClause = new StringBuilder()
         .AppendLine("where (")
         .AppendLine(BuildTimedDateFilterSql())
         .AppendLine("   or (")
         .AppendLine("      a.starts_at is null")
         .AppendLine("      and a.activity_date = @date")
         .AppendLine("   )")
         .AppendLine(")");

      if(!string.Equals(
         status,
         ActivityListStatusIds.All,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         whereClause.AppendLine("   and a.publication_status_id = @status");
      }

      if(normalizedSports.Count > 0)
      {
         whereClause.AppendLine("   and a.sport_id = any(@sport_ids)");
      }

      return await QueryActivityListAsync(
         whereClause.ToString(),
         TimedOrderClause,
         "s.name",
         command =>
         {
            command.Parameters.AddWithValue("start", start);
            command.Parameters.AddWithValue("end", end);
            command.Parameters.AddWithValue(
               "time_zone",
               SportDay.TimeZoneId
            );
            command.Parameters.AddWithValue("date", date);

            if(!string.Equals(
               status,
               ActivityListStatusIds.All,
               StringComparison.OrdinalIgnoreCase
            ))
            {
               command.Parameters.AddWithValue(
                  "status",
                  status ?? ActivityListStatusIds.All
               );
            }

            if(normalizedSports.Count > 0)
            {
               command.Parameters.AddWithValue(
                  "sport_ids",
                  normalizedSports.ToArray()
               );
            }
         },
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<ActivityListItem>> GetPublishedForDateAsync(
      DateOnly date,
      CancellationToken cancellationToken
   )
   {
      return await GetPublishedActivitiesAsync(
         SportDay.ForDate(date),
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<PublishedDateParticipantCount>>
      GetPublishedDateParticipantCountsFromAsync(
         DateOnly firstDate,
         CancellationToken cancellationToken
      )
   {
      var sql = $$"""
         with dated_activities as (
            select
               a.id,
               case
                  when coalesce(
                     ag.public_date_mode,
                     '{{ActivityGroupPublicDateModeIds.SportDay}}'
                  ) =
                     '{{ActivityGroupPublicDateModeIds.LocalCalendarDate}}'
                  then (a.starts_at at time zone @time_zone)::date
                  else (
                     (a.starts_at at time zone @time_zone) - @cutoff
                  )::date
               end as display_date
            from activities a
            left join activity_groups ag
               on ag.id = a.activity_group_id
            where a.publication_status_id =
               '{{ActivityPublicationStatusIds.Published}}'
               {{PublicActivityExclusionClause}}
         )
         select
            dated.display_date,
            count(distinct e.id) filter (
               where e.entity_type_id in (
                  '{{TrackedEntityTypeIds.Person}}',
                  '{{TrackedEntityTypeIds.NationalTeam}}',
                  '{{TrackedEntityTypeIds.Pair}}'
               )
                  and al.is_active
            )::integer as participant_count
         from dated_activities dated
         left join activity_entity_links al on al.activity_id = dated.id
         left join entities e on e.id = al.entity_id
         where dated.display_date >= @first_date
         group by dated.display_date
         order by dated.display_date
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "time_zone",
         SportDay.TimeZoneId
      );
      command.Parameters.AddWithValue(
         "cutoff",
         SportDay.Cutoff.ToTimeSpan()
      );
      command.Parameters.AddWithValue(
         "first_date",
         firstDate
      );
      AddPublicActivityExclusionParameters(command);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var dates = new List<PublishedDateParticipantCount>();

      while(await reader.ReadAsync(cancellationToken))
      {
         dates.Add(
            new PublishedDateParticipantCount(
               reader.GetFieldValue<DateOnly>(0),
               reader.GetInt32(1)
            )
         );
      }

      return dates;
   }

   private async Task<IReadOnlyList<ActivityListItem>>
      GetPublishedActivitiesAsync(
         SportDayWindow window,
         CancellationToken cancellationToken
      )
   {
      var activities = await QueryActivityListAsync(
         $$"""
            where a.publication_status_id =
               '{{ActivityPublicationStatusIds.Published}}'
               and {{BuildTimedDateFilterSql()}}
               {{PublicActivityExclusionClause}}
         """,
         DefaultOrderClause,
         """
         coalesce(s.display_name, s.name)
         """,
         command =>
         {
            command.Parameters.AddWithValue(
               "start",
               ToUtc(window.StartDate, window.Cutoff)
            );
            command.Parameters.AddWithValue(
               "end",
               ToUtc(window.EndDateExclusive, window.Cutoff)
            );
            command.Parameters.AddWithValue(
               "time_zone",
               SportDay.TimeZoneId
            );
            command.Parameters.AddWithValue("date", window.StartDate);
            AddPublicActivityExclusionParameters(command);
         },
         cancellationToken
      );

      return await ApplyNationalTeamFlagsAsync(
         activities,
         cancellationToken
      );
   }

   private static void AddPublicActivityExclusionParameters(
      NpgsqlCommand command
   )
   {
      command.Parameters.AddWithValue(
         "test_activity_title",
         TestActivityTitle
      );
      command.Parameters.AddWithValue(
         "test_activity_slug_pattern",
         TestActivitySlugPattern
      );
   }

   private async Task<IReadOnlyList<ActivityListItem>>
      ApplyNationalTeamFlagsAsync(
         IReadOnlyList<ActivityListItem> activities,
         CancellationToken cancellationToken
      )
   {
      if(activities.Count == 0)
      {
         return activities;
      }

      const string sql = $$"""
         select distinct al.activity_id
         from activity_entity_links al
         join entities org on org.id = al.organization_entity_id
         where al.activity_id = any(@activity_ids)
            and org.entity_type_id = '{{TrackedEntityTypeIds.NationalTeam}}'
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "activity_ids",
         activities.Select(activity => activity.Id).ToArray()
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var nationalTeamActivityIds = new HashSet<Guid>();

      while(await reader.ReadAsync(cancellationToken))
      {
         nationalTeamActivityIds.Add(reader.GetGuid(0));
      }

      return activities
         .Select(activity => nationalTeamActivityIds.Contains(activity.Id)
            ? activity with
            {
               HasNationalTeamRelatedOrganization = true
            }
            : activity)
         .ToArray();
   }

   public async Task<IReadOnlyList<EntityOption>> GetEntityOptionsAsync(
      CancellationToken cancellationToken
   )
   {
      var sql = $$"""
         select
            e.id,
            e.canonical_name,
            e.entity_type_id,
            s.name,
            coalesce(org.organization_names, ''),
            e.person_gender_id,
            e.alias_name
         from entities e
         join sports s
            on s.id = e.sport_id
            and e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
         join entity_watch_priorities p
            on p.id = e.watch_priority_id
         {{GetLinkedOrganizationNamesLateralSql("e")}}
         order by
            p.sort_order,
            e.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var entities = new List<EntityOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         entities.Add(
            new EntityOption(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.IsDBNull(5) ? null : reader.GetString(5),
               reader.IsDBNull(6) ? null : reader.GetString(6)
            )
         );
      }

      return entities;
   }

   public async Task<IReadOnlyList<EntityOption>>
      GetPersonEntitiesForOrganizationAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      )
   {
      var sql = $$"""
         select
            e.id,
            e.canonical_name,
            e.entity_type_id,
            s.name,
            coalesce(org.alias_name, org.canonical_name) as organization_names,
            e.person_gender_id,
            e.alias_name
         from entities e
         join entities org
            on org.id = @organization_entity_id
         join sports s
            on s.id = e.sport_id
         join entity_watch_priorities p
            on p.id = e.watch_priority_id
         where e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
            and exists (
               select 1
               from entity_to_entity_links l
               where (l.source_entity_id = @organization_entity_id
                     and l.target_entity_id = e.id)
                  or (l.target_entity_id = @organization_entity_id
                     and l.source_entity_id = e.id)
            )
         order by sort_order, canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationEntityId
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var entities = new List<EntityOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         entities.Add(
            new EntityOption(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.IsDBNull(5) ? null : reader.GetString(5),
               reader.IsDBNull(6) ? null : reader.GetString(6)
            )
         );
      }

      return entities;
   }

   public async Task<IReadOnlyList<EntityOption>>
      GetPersonEntitiesForPromptCandidatesAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      )
   {
      var sql = $$"""
         with candidate_rows as (
            select distinct
               e.id,
               e.canonical_name,
               e.entity_type_id,
               s.name,
               coalesce(
                  org.alias_name,
                  org.canonical_name
               ) as organization_names,
               p.sort_order,
               e.person_gender_id,
               e.alias_name
            from entities e
            join entities org
               on org.id = @organization_entity_id
            join sports s
               on s.id = e.sport_id
            join entity_watch_priorities p
               on p.id = e.watch_priority_id
            where e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
               and exists (
                  select 1
                  from entity_to_entity_links l
                  where (l.source_entity_id = @organization_entity_id
                        and l.target_entity_id = e.id)
                     or (l.target_entity_id = @organization_entity_id
                        and l.source_entity_id = e.id)
               )
         )
         select
            id,
            canonical_name,
            entity_type_id,
            name,
            organization_names,
            person_gender_id,
            alias_name
         from candidate_rows
         order by sort_order, random()
         limit 5
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationEntityId
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var entities = new List<EntityOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         entities.Add(
            new EntityOption(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.IsDBNull(5) ? null : reader.GetString(5),
               reader.IsDBNull(6) ? null : reader.GetString(6)
            )
         );
      }

      return entities;
   }

   public async Task<IReadOnlyList<LookupOption>> GetActivityTypeOptionsAsync(
      CancellationToken cancellationToken
   )
   {
      return await GetLookupOptionsAsync(
         "select id, label from activity_types order by sort_order, label",
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<LookupOption>> GetSportOptionsAsync(
      CancellationToken cancellationToken
   )
   {
      return await GetLookupOptionsAsync(
         "select id, name from sports order by name",
         cancellationToken
      );
   }

   public async Task<bool> RequiresParticipantStartTimesAsync(
      string sportId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select requires_start_time
         from sports
         where id = @sport_id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("sport_id", sportId.Trim());
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return false;
      }

      return reader.GetBoolean(0);
   }

   public async Task<IReadOnlyList<LookupOption>>
      SearchActivityGroupOptionsAsync(
         string? term,
         string? sportId,
         CancellationToken cancellationToken,
         Guid? organizationEntityId = null
      )
   {
      term = term?.Trim() ?? string.Empty;
      var applyTermFilter = term != string.Empty;
      var escapedTerm = applyTermFilter
         ? term
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
         : string.Empty;
      var termFilterSql = applyTermFilter
         ? "and title ilike @term escape '\\'"
         : string.Empty;
      var sql = $$"""
         select id::text, title
         from activity_groups
         where (@sport_id is null or sport_id = @sport_id)
            and (@organization_entity_id is null or exists (
               select 1
               from activities a
               join activity_entity_links al
                  on al.activity_id = a.id
               where a.activity_group_id = activity_groups.id
                  and al.organization_entity_id =
                     @organization_entity_id
            ))
            {{termFilterSql}}
         order by start_date desc, end_date desc, title, id
         limit 20
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.Add(
         "sport_id",
         NpgsqlDbType.Text
      ).Value = string.IsNullOrWhiteSpace(sportId)
         ? DBNull.Value
         : sportId;
      command.Parameters.Add(
         "organization_entity_id",
         NpgsqlDbType.Uuid
      ).Value = (object?)organizationEntityId ?? DBNull.Value;
      if(applyTermFilter)
      {
         command.Parameters.AddWithValue("term", $"%{escapedTerm}%");
      }

      return await ReadLookupOptionsAsync(command, cancellationToken);
   }

   public async Task<ActivityEditModel?> GetForEditAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            a.id,
            a.title,
            a.description,
            a.teaser,
            a.activity_type_id,
            a.sport_id,
            a.activity_date,
            a.local_start_time,
            a.local_end_time,
            a.time_zone_id,
            a.publication_status_id,
            a.tv_channel_name,
            a.activity_group_id,
            ag.title as activity_group_title
         from activities a
         left join activity_groups ag on ag.id = a.activity_group_id
         where a.id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      var model = new ActivityEditModel
      {
         Id = reader.GetGuid(0),
         Title = reader.GetString(1),
         Description = ReadString(reader, 2),
         Teaser = ReadString(reader, 3),
         ActivityType = reader.GetString(4),
         SportId = reader.GetString(5),
         ActivityDate = reader.GetFieldValue<DateOnly>(6),
         LocalStartTime = ReadTimeOnly(reader, 7),
         LocalEndTime = ReadTimeOnly(reader, 8),
         TimeZoneId = reader.GetString(9),
         IsPublished =
            reader.GetString(10) == ActivityPublicationStatusIds.Published,
         TvChannelName = ReadString(reader, 11),
         ActivityGroupId = reader.IsDBNull(12) ? null : reader.GetGuid(12),
         ActivityGroupTitle = ReadString(reader, 13)
      };

      await reader.DisposeAsync();

      const string linkSql = """
         select entity_id, organization_entity_id
         from activity_entity_links
         where activity_id = @id
         order by id
         """;

      await using var linkCommand = dataSource.CreateCommand(linkSql);
      linkCommand.Parameters.AddWithValue("id", id);
      await using var linkReader = await linkCommand.ExecuteReaderAsync(
         cancellationToken
      );

      while(await linkReader.ReadAsync(cancellationToken))
      {
         model.LinkedEntityIds.Add(linkReader.GetGuid(0));

         if(linkReader.IsDBNull(1))
         {
            continue;
         }

         var organizationEntityId = linkReader.GetGuid(1);

         if(model.OrganizationEntityId is null)
         {
            model.OrganizationEntityId = organizationEntityId;
         }
         else if(model.OrganizationEntityId != organizationEntityId)
         {
            model.OrganizationEntityId = null;
         }
      }

      var sourceSql = $"""
         select id, kind, url, title, excerpt, observed_at
         from sources
         where correlation_type = '{SourceCorrelationTypes.Activity}'
            and correlation_id = @id
         order by observed_at desc, created_at desc, id desc
         """;

      await using var sourceCommand = dataSource.CreateCommand(sourceSql);
      sourceCommand.Parameters.AddWithValue("id", id.ToString());
      await using var sourceReader = await sourceCommand.ExecuteReaderAsync(
         cancellationToken
      );

      while(await sourceReader.ReadAsync(cancellationToken))
      {
         model.Sources.Add(
            new ActivitySourceEditModel
            {
               Id = sourceReader.GetGuid(0),
               Kind = sourceReader.GetString(1),
               Url = sourceReader.GetString(2),
               Title = ReadString(sourceReader, 3),
               Excerpt = ReadString(sourceReader, 4),
               ObservedAt = sourceReader.GetFieldValue<DateTimeOffset>(5)
            }
         );
      }

      return model;
   }

   public async Task<IReadOnlyList<string>> GetOtherGroupDescriptionsAsync(
      Guid activityGroupId,
      Guid? excludedActivityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select btrim(description) as description
         from activities
         where activity_group_id = @activity_group_id
            and description is not null
            and btrim(description) <> ''
            and (@excluded_activity_id is null
               or id <> @excluded_activity_id)
         group by btrim(description)
         order by max(activity_date) desc, description
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "activity_group_id",
         activityGroupId
      );
      command.Parameters.Add(
         "excluded_activity_id",
         NpgsqlDbType.Uuid
      ).Value = (object?)excludedActivityId ?? DBNull.Value;
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var descriptions = new List<string>();

      while(await reader.ReadAsync(cancellationToken))
      {
         descriptions.Add(reader.GetString(0));
      }

      return descriptions;
   }

   public async Task<Guid?> GetActivityGroupIdAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select activity_group_id
         from activities
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      var result = await command.ExecuteScalarAsync(cancellationToken);

      return result is null || result is DBNull
         ? null
         : (Guid)result;
   }

   public async Task<string?> GetActivityGroupTitleAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select title
         from activity_groups
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      return (string?)await command.ExecuteScalarAsync(cancellationToken);
   }

   public async Task<ActivityGroupEditModel?> GetActivityGroupForEditAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            id,
            title,
            sport_id,
            start_date,
            end_date,
            no_grouping,
            public_date_mode
         from activity_groups
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return new ActivityGroupEditModel
      {
         Id = reader.GetGuid(0),
         Title = reader.GetString(1),
         SportId = reader.GetString(2),
         StartDate = reader.GetFieldValue<DateOnly>(3),
         EndDate = reader.GetFieldValue<DateOnly>(4),
         NoGrouping = reader.GetBoolean(5),
         PublicDateMode = reader.GetString(6)
      };
   }

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

   public async Task<IReadOnlyList<ActivityGroupActivityListItem>>
      GetActivitiesForGroupEditAsync(
         Guid activityGroupId,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            id,
            title,
            description,
            activity_date,
            local_start_time,
            local_end_time
         from activities
         where activity_group_id = @activity_group_id
         order by
            activity_date,
            local_start_time nulls last,
            title
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "activity_group_id",
         activityGroupId
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var activities = new List<ActivityGroupActivityListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         activities.Add(
            new ActivityGroupActivityListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               ReadString(reader, 2),
               reader.GetFieldValue<DateOnly>(3),
               ReadTimeOnly(reader, 4),
               ReadTimeOnly(reader, 5)
            )
         );
      }

      return activities;
   }

   public async Task<IReadOnlyList<ActivityGroupSourceListItem>>
      GetSourcesForGroupEditAsync(
         Guid activityGroupId,
         CancellationToken cancellationToken
      )
   {
      const string sql = $"""
         select
            s.kind,
            s.url,
            s.title,
            s.excerpt,
            s.observed_at
         from activities a
         join sources s
            on s.correlation_type = '{SourceCorrelationTypes.Activity}'
            and s.correlation_id = a.id::text
         where a.activity_group_id = @activity_group_id
         order by s.observed_at desc, s.created_at desc, s.id desc
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "activity_group_id",
         activityGroupId
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var sources = new List<ActivityGroupSourceListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         sources.Add(
            new ActivityGroupSourceListItem(
               reader.GetString(0),
               reader.GetString(1),
               ReadString(reader, 2),
               ReadString(reader, 3),
               reader.GetFieldValue<DateTimeOffset>(4)
            )
         );
      }

      return sources;
   }

   public async Task<
      IReadOnlyDictionary<Guid, IReadOnlyList<ActivityGroupParticipant>>>
      GetActivityGroupParticipantsAsync(
         IReadOnlyCollection<Guid> activityGroupIds,
         CancellationToken cancellationToken
      )
   {
      if(activityGroupIds.Count == 0)
      {
         return new Dictionary<
            Guid,
            IReadOnlyList<ActivityGroupParticipant>
         >();
      }

      var sql = $$"""
         select distinct
            a.activity_group_id,
            e.id,
            e.canonical_name
         from activities a
         join activity_entity_links al on al.activity_id = a.id
         join entities e on e.id = al.entity_id
         where a.activity_group_id = any(@activity_group_ids)
            and e.entity_type_id in (
               '{{TrackedEntityTypeIds.Person}}',
               '{{TrackedEntityTypeIds.NationalTeam}}',
               '{{TrackedEntityTypeIds.Pair}}'
            )
         order by a.activity_group_id, e.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "activity_group_ids",
         activityGroupIds.Distinct().ToArray()
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var participants = new Dictionary<
         Guid,
         List<ActivityGroupParticipant>
      >();

      while(await reader.ReadAsync(cancellationToken))
      {
         var activityGroupId = reader.GetGuid(0);

         if(!participants.TryGetValue(activityGroupId, out var group))
         {
            group = [];
            participants[activityGroupId] = group;
         }

         group.Add(
            new ActivityGroupParticipant(
               reader.GetGuid(1),
               reader.GetString(2)
            )
         );
      }

      return participants.ToDictionary(
         pair => pair.Key,
         pair => (IReadOnlyList<ActivityGroupParticipant>)pair.Value
      );
   }

   public async Task<IReadOnlyList<ActivityParticipantListItem>>
      GetParticipantsForEditAsync(
         Guid? activityId,
         IReadOnlyCollection<Guid> entityIds,
         CancellationToken cancellationToken
      )
   {
      if(activityId is null && entityIds.Count == 0)
      {
         return [];
      }

      var activityLinkJoin = activityId is null
         ? string.Empty
         : "join activity_entity_links al on al.entity_id = e.id";
      var activeExpression = activityId is null ? "true" : "al.is_active";
      var whereClause = activityId is null
         ? "where e.id = any(@entity_ids)"
         : "where al.activity_id = @activity_id";
      var sql = $$"""
         select distinct
            e.id,
            e.canonical_name,
            coalesce(org.organization_names, ''),
            wp.label,
            case e.person_gender_id
               when '{{PersonGenderIds.Female}}' then 'Female'
               when '{{PersonGenderIds.Male}}' then 'Male'
               else ''
            end,
            coalesce(e.alias_name, ''),
            wp.sort_order as sort_order,
            {{activeExpression}}
         from entities e
         join entity_watch_priorities wp on wp.id = e.watch_priority_id
         {{activityLinkJoin}}
         {{GetLinkedOrganizationNamesLateralSql("e")}}
         {{whereClause}}
         order by sort_order, e.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);

      if(activityId is null)
      {
         command.Parameters.AddWithValue("entity_ids", entityIds.ToArray());
      }
      else
      {
         command.Parameters.AddWithValue("activity_id", activityId.Value);
      }

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var participants = new List<ActivityParticipantListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         participants.Add(
            new ActivityParticipantListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.GetString(5),
               reader.GetBoolean(7)
            )
         );
      }

      return participants;
   }

   public async Task DeleteParticipantAsync(
      Guid activityId,
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from activity_entity_links
         where activity_id = @activity_id
            and entity_id = @entity_id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", entityId);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task SetParticipantActiveAsync(
      Guid activityId,
      Guid entityId,
      bool isActive,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         with selected_activity as (
            select activity_group_id
            from activities
            where id = @activity_id
         )
         update activity_entity_links link
         set is_active = @is_active
         from activities activity
         where link.activity_id = activity.id
            and link.entity_id = @entity_id
            and (
               link.activity_id = @activity_id
               or (
                  activity.activity_group_id = (
                     select activity_group_id
                     from selected_activity
                  )
                  and coalesce(
                     activity.starts_at,
                     (
                        activity.activity_date
                        + coalesce(
                           activity.local_start_time,
                           time '23:59:59'
                        )
                     ) at time zone activity.time_zone_id
                  ) > now()
               )
            )
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", entityId);
      command.Parameters.AddWithValue("is_active", isActive);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task AddParticipantAsync(
      Guid activityId,
      Guid entityId,
      Guid organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = $$"""
         insert into activity_entity_links (
            id,
            activity_id,
            entity_id,
            organization_entity_id
         )
         select
            @id,
            @activity_id,
            e.id,
            @organization_entity_id
         from entities e
         where e.id = @entity_id
            and e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
            and exists (
               select 1
               from entity_to_entity_links l
               where (l.source_entity_id = @organization_entity_id
                     and l.target_entity_id = e.id)
                  or (l.target_entity_id = @organization_entity_id
                     and l.source_entity_id = e.id)
            )
            and not exists (
               select 1
               from activity_entity_links existing
               where existing.activity_id = @activity_id
                  and existing.entity_id = e.id
            )
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", entityId);
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationEntityId
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<IReadOnlyList<ActivityParticipantListItem>>
      SearchParticipantCandidatesAsync(
         Guid organizationEntityId,
         string term,
         IReadOnlyCollection<Guid> excludedEntityIds,
         CancellationToken cancellationToken
      )
   {
      term = term.Trim();
      var applyTermFilter = term != string.Empty;
      var escapedTerm = applyTermFilter
         ? term
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
         : string.Empty;
      var excludedIds = excludedEntityIds
         .Where(entityId => entityId != Guid.Empty)
         .Distinct()
         .ToArray();
      var termFilterSql = applyTermFilter
         ? """
            and (
               e.canonical_name ilike @term escape '\'
               or coalesce(e.alias_name, '') ilike @term escape '\'
            )
            """
         : string.Empty;
      var excludedSql = excludedIds.Length == 0
         ? string.Empty
         : "and e.id <> all(@excluded_entity_ids)";
      var limitSql = applyTermFilter ? "limit 20" : string.Empty;
      var sql = $$"""
         select
            e.id,
            e.canonical_name,
            coalesce(org.organization_names, ''),
            wp.label,
            case e.person_gender_id
               when '{{PersonGenderIds.Female}}' then 'Female'
               when '{{PersonGenderIds.Male}}' then 'Male'
               else ''
            end,
            coalesce(e.alias_name, '')
         from entities e
         join entity_watch_priorities wp on wp.id = e.watch_priority_id
         {{GetLinkedOrganizationNamesLateralSql("e")}}
         where e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
            {{termFilterSql}}
            and exists (
               select 1
               from entity_to_entity_links l
               where (l.source_entity_id = @organization_entity_id
                     and l.target_entity_id = e.id)
                  or (l.target_entity_id = @organization_entity_id
                     and l.source_entity_id = e.id)
            )
            {{excludedSql}}
         order by wp.sort_order, e.canonical_name
         {{limitSql}}
         """;

      await using var command = dataSource.CreateCommand(sql);
      if(applyTermFilter)
      {
         command.Parameters.AddWithValue("term", $"%{escapedTerm}%");
      }
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationEntityId
      );

      if(excludedIds.Length > 0)
      {
         command.Parameters.AddWithValue("excluded_entity_ids", excludedIds);
      }

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var participants = new List<ActivityParticipantListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         participants.Add(
            new ActivityParticipantListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.GetString(5),
               true
            )
         );
      }

      return participants;
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

   private async Task<IReadOnlyList<ActivityListItem>> QueryActivityListAsync(
      string whereClause,
      string orderClause,
      string sportNameExpression,
      Action<NpgsqlCommand>? configureCommand,
      CancellationToken cancellationToken
   )
   {
      var sql = CreateActivityListSql(
         whereClause,
         orderClause,
         sportNameExpression
      );

      await using var command = dataSource.CreateCommand(sql);
      configureCommand?.Invoke(command);
      var activities = await ReadActivityListAsync(
         command,
         cancellationToken
      );
      var participantsByActivity = await GetPublicParticipantsAsync(
         activities.Select(activity => activity.Id).ToArray(),
         cancellationToken
      );

      return activities
         .Select(activity => activity with
         {
            Participants = participantsByActivity.GetValueOrDefault(
               activity.Id,
               []
            )
         })
         .ToArray();
   }

   private async Task<IReadOnlyDictionary<
      Guid,
      IReadOnlyList<PublicActivityParticipant>
   >> GetPublicParticipantsAsync(
      Guid[] activityIds,
      CancellationToken cancellationToken
   )
   {
      if(activityIds.Length == 0)
      {
         return new Dictionary<
            Guid,
            IReadOnlyList<PublicActivityParticipant>
         >();
      }

      var sql = $$"""
         select distinct
            al.activity_id,
            person.id,
            person.canonical_name,
            participant_start.start_time,
            person.birthdate,
            person.height,
            coalesce(person.formative_club, '') as club,
            discipline.id is not null as has_discipline,
            nullif(btrim(discipline.alias_name), '') as discipline_alias_name,
            priority.sort_order,
            participant_team.team_country_id,
            participant_team.team_country_name,
            al.is_active
         from activity_entity_links al
         join activities activity on activity.id = al.activity_id
         join entities person on person.id = al.entity_id
         join entity_watch_priorities priority
            on priority.id = person.watch_priority_id
         left join lateral (
            select nullif(btrim(r.value_text), '') as start_time
            from activity_participant_ai_results r
            where r.activity_id = al.activity_id
               and r.entity_id = person.id
               and r.job_id = '{{AiJobIds.FindParticipantsStart}}'
               and r.field_key =
                  '{{ActivityParticipantAiFieldKeys.StartTime}}'
               and r.updated_at >= activity.updated_at
            order by
               r.updated_at desc,
               r.sort_order asc,
               r.id desc
            limit 1
         ) participant_start on true
         left join lateral (
            select
               linked.id,
               linked.alias_name
            from entity_to_entity_links entity_link
            join entities linked on linked.id = case
               when entity_link.source_entity_id = person.id
                  then entity_link.target_entity_id
               else entity_link.source_entity_id
            end
            where (
               entity_link.source_entity_id = person.id
               or entity_link.target_entity_id = person.id
            )
               and linked.entity_type_id =
                  '{{TrackedEntityTypeIds.Discipline}}'
            order by
               linked.alias_name nulls last,
               linked.canonical_name,
               linked.id
            limit 1
         ) discipline on true
         left join lateral (
            select
               min(team.country_id) as team_country_id,
               min(team.country_name) as team_country_name
            from (
               select distinct
                  linked.country_id,
                  country.name as country_name,
                  linked.canonical_name as team_name
               from entity_to_entity_links entity_link
               join entities linked on linked.id = case
                  when entity_link.source_entity_id = person.id
                     then entity_link.target_entity_id
                  else entity_link.source_entity_id
               end
               join countries country on country.id = linked.country_id
               where (
                  entity_link.source_entity_id = person.id
                  or entity_link.target_entity_id = person.id
               )
                  and linked.entity_type_id =
                     '{{TrackedEntityTypeIds.Team}}'
            ) team
            having count(*) = 1
         ) participant_team on true
         where al.activity_id = any(@activity_ids)
            and person.entity_type_id =
               '{{TrackedEntityTypeIds.Person}}'
         order by
            al.activity_id,
            al.is_active desc,
            priority.sort_order,
            person.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_ids", activityIds);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var participants = new Dictionary<
         Guid,
         List<PublicActivityParticipant>
      >();

      while(await reader.ReadAsync(cancellationToken))
      {
         var activityId = reader.GetGuid(0);

         if(!participants.TryGetValue(activityId, out var activityRows))
         {
            activityRows = [];
            participants[activityId] = activityRows;
         }

         activityRows.Add(
            new PublicActivityParticipant(
               reader.GetGuid(1),
               reader.GetString(2),
               reader.IsDBNull(3)
                  ? null
                  : reader.GetString(3),
               reader.IsDBNull(4)
                  ? null
                  : reader.GetFieldValue<DateOnly>(4),
               reader.IsDBNull(5) ? null : reader.GetInt32(5),
               reader.GetString(6),
               reader.IsDBNull(10) ? null : reader.GetString(10),
               reader.IsDBNull(11) ? null : reader.GetString(11),
               reader.GetBoolean(12),
               reader.GetBoolean(7),
               reader.IsDBNull(8) ? null : reader.GetString(8)
            )
            {
               WatchPriority = reader.GetInt32(9)
            }
         );
      }

      return participants.ToDictionary(
         pair => pair.Key,
         pair => (IReadOnlyList<PublicActivityParticipant>)pair.Value
      );
   }

   private static string CreateActivityListSql(
      string whereClause,
      string orderClause,
      string sportNameExpression
   )
   {
      var builder = new StringBuilder()
         .AppendLine("select")
         .AppendLine("   a.id,")
         .AppendLine("   a.title,")
         .AppendLine("   a.description,")
         .AppendLine("   a.teaser,")
         .AppendLine("   at.label,")
         .AppendLine("   s.id,")
         .AppendLine($"   {sportNameExpression},")
         .AppendLine("   s.icon_id,")
         .AppendLine("   a.activity_date,")
         .AppendLine("   a.local_start_time,")
         .AppendLine("   a.starts_at,")
         .AppendLine("   a.publication_status_id,")
         .AppendLine("   a.tv_channel_name,")
         .AppendLine("   coalesce(")
         .AppendLine("      string_agg(")
         .AppendLine("         te.canonical_name,")
         .AppendLine("         ', ' order by te.canonical_name")
         .AppendLine("      ),")
         .AppendLine("      ''")
         .AppendLine("   ) as entities,")
         .AppendLine(
            "   coalesce(rp.related_person_entities, '') as " +
            "related_person_entities,"
         )
         .AppendLine(
            "   coalesce(rp.related_person_entity_ids, '{}'::uuid[]) " +
            "as related_person_entity_ids,"
         )
         .AppendLine(
            "   coalesce(" +
            "rp.active_related_person_entity_ids, '{}'::uuid[]) " +
            "as active_related_person_entity_ids,"
         )
         .AppendLine(
            "   coalesce(ro.related_organization_entities, '') " +
            "as related_organization_entities,"
         )
         .AppendLine("   a.local_end_time,")
         .AppendLine("   a.ends_at,")
         .AppendLine("   a.activity_group_id,")
         .AppendLine("   ag.title as activity_group_title,")
         .AppendLine(
            "   coalesce(ag.no_grouping, false),"
         )
         .AppendLine(
            "   coalesce(ro.related_organization_canonical_entities, '') " +
            "as related_organization_canonical_entities,"
         )
         .AppendLine("   s.is_team_sport,")
         .AppendLine(
            $"   coalesce(ag.public_date_mode, " +
            $"'{ActivityGroupPublicDateModeIds.SportDay}')"
         )
         .AppendLine("from activities a")
         .AppendLine(
            "left join activity_groups ag on ag.id = a.activity_group_id"
         )
         .AppendLine("join sports s on s.id = a.sport_id")
         .AppendLine("join activity_types at on at.id = a.activity_type_id")
         .AppendLine(
            "left join activity_entity_links l on l.activity_id = a.id"
         )
         .AppendLine("left join entities te on te.id = l.entity_id")
         .AppendLine("left join lateral (")
         .AppendLine("   select")
         .AppendLine("      string_agg(")
         .AppendLine("         person_name,")
         .AppendLine("         ', ' order by sort_order, person_name")
         .AppendLine("      ) as related_person_entities,")
         .AppendLine("      coalesce(")
         .AppendLine("         array_agg(")
         .AppendLine("            person_id order by sort_order, person_name")
         .AppendLine("         ),")
         .AppendLine("         '{}'::uuid[]")
         .AppendLine("      ) as related_person_entity_ids,")
         .AppendLine("      coalesce(")
         .AppendLine("         array_agg(")
         .AppendLine("            person_id order by sort_order, person_name")
         .AppendLine("            ) filter (where is_active),")
         .AppendLine("         '{}'::uuid[]")
         .AppendLine(
            "      ) as active_related_person_entity_ids"
         )
         .AppendLine("   from (")
         .AppendLine("      select distinct")
         .AppendLine("         p.id as person_id,")
         .AppendLine("         p.canonical_name as person_name,")
         .AppendLine("         wp.sort_order,")
         .AppendLine("         al.is_active")
         .AppendLine("      from activity_entity_links al")
         .AppendLine("      join entities p on p.id = al.entity_id")
         .AppendLine("      join entity_watch_priorities wp")
         .AppendLine("         on wp.id = p.watch_priority_id")
         .AppendLine("      where al.activity_id = a.id")
         .AppendLine(
            $$"""
               and p.entity_type_id in (
                  '{{TrackedEntityTypeIds.Person}}',
                  '{{TrackedEntityTypeIds.NationalTeam}}',
                  '{{TrackedEntityTypeIds.Pair}}'
               )
            """
         )
         .AppendLine("   ) persons")
         .AppendLine(") rp on true")
         .AppendLine("left join lateral (")
         .AppendLine("   select string_agg(")
         .AppendLine("      distinct organization_name,")
         .AppendLine("      ', ' order by organization_name")
         .AppendLine("   ) as related_organization_entities,")
         .AppendLine("   string_agg(")
         .AppendLine("      distinct organization_canonical_name,")
         .AppendLine("      ', ' order by organization_canonical_name")
         .AppendLine(
            "   ) as related_organization_canonical_entities"
         )
         .AppendLine("   from (")
         .AppendLine("      select distinct")
         .AppendLine("         coalesce(context.alias_name,")
         .AppendLine(
            "            context.canonical_name) as organization_name,"
         )
         .AppendLine(
            "         context.canonical_name as organization_canonical_name"
         )
         .AppendLine("      from activity_entity_links al")
         .AppendLine("      join entities p on p.id = al.entity_id")
         .AppendLine("      join entities context")
         .AppendLine("         on context.id = al.organization_entity_id")
         .AppendLine("      where al.activity_id = a.id")
         .AppendLine(
            $$"""
               and p.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
            """
         )
         .AppendLine(
            "         and " +
            BroadcastEntityFilter.GetNonOrganizationEntityTypePredicateSql(
               "context.entity_type_id"
            )
         )
         .AppendLine("   ) organizations")
         .AppendLine(") ro on true")
         .AppendLine(whereClause)
         .AppendLine(
            "group by a.id, at.label, s.id, " +
            $"{sportNameExpression}, s.icon_id,"
         )
         .AppendLine(
            "         a.tv_channel_name, rp.related_person_entities,"
         )
         .AppendLine(
            "         rp.related_person_entity_ids,"
         )
         .AppendLine(
            "         rp.active_related_person_entity_ids,"
         )
         .AppendLine(
            "         ro.related_organization_entities, a.local_end_time,"
         )
         .AppendLine(
            "         ro.related_organization_canonical_entities,"
         )
         .AppendLine(
            "         a.ends_at, ag.title, ag.no_grouping, " +
            "s.is_team_sport, ag.public_date_mode"
         )
         .AppendLine(orderClause);

      return builder.ToString();
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
      var inactiveEntityIds = new HashSet<Guid>();
      await using(var statusCommand = new NpgsqlCommand(
         """
         select entity_id
         from activity_entity_links
         where activity_id = @activity_id
            and not is_active
         """,
         connection,
         transaction
      ))
      {
         statusCommand.Parameters.AddWithValue("activity_id", activityId);
         await using var reader = await statusCommand.ExecuteReaderAsync(
            cancellationToken
         );

         while(await reader.ReadAsync(cancellationToken))
         {
            inactiveEntityIds.Add(reader.GetGuid(0));
         }
      }

      await using var deleteCommand = new NpgsqlCommand(
         "delete from activity_entity_links where activity_id = @activity_id",
         connection,
         transaction
      );
      deleteCommand.Parameters.AddWithValue("activity_id", activityId);
      await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

      var distinctEntityIds = entityIds
         .Where(entityId => entityId != Guid.Empty)
         .Distinct()
         .ToList();

      if(distinctEntityIds.Count == 0)
      {
         return;
      }

      const string sql = $$"""
         insert into activity_entity_links (
            id,
            activity_id,
            entity_id,
            organization_entity_id,
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
            @is_active
         )
         """;

      foreach(var entityId in distinctEntityIds)
      {
         await using var command = new NpgsqlCommand(
            sql,
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
         command.Parameters.AddWithValue(
            "is_active",
            !inactiveEntityIds.Contains(entityId)
         );
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

   private async Task<IReadOnlyList<LookupOption>> GetLookupOptionsAsync(
      string sql,
      CancellationToken cancellationToken
   )
   {
      await using var command = dataSource.CreateCommand(sql);
      return await ReadLookupOptionsAsync(command, cancellationToken);
   }

   private static async Task<IReadOnlyList<LookupOption>>
      ReadLookupOptionsAsync(
         NpgsqlCommand command,
         CancellationToken cancellationToken
      )
   {
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<LookupOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         options.Add(
            new LookupOption(reader.GetString(0), reader.GetString(1))
         );
      }

      return options;
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

   private static string? ReadString(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static async Task<IReadOnlyList<ActivityListItem>>
      ReadActivityListAsync(
         NpgsqlCommand command,
         CancellationToken cancellationToken
      )
   {
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var activities = new List<ActivityListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         activities.Add(
            new ActivityListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               ReadString(reader, 2),
               ReadString(reader, 3),
               reader.GetString(4),
               reader.GetString(5),
               reader.GetString(6),
               GetSportIconPath(ReadString(reader, 7)),
               DateDisplay.Format(
                  reader.GetFieldValue<DateOnly>(8),
                  ReadTimeOnly(reader, 9)
               ),
               ReadDateTimeOffset(reader, 10),
               ReadString(reader, 12),
               reader.GetString(11),
               reader.GetString(14),
               ReadGuidArray(reader, 15),
               reader.GetString(17)
            )
            {
               ActiveRelatedPersonEntityIds = ReadGuidArray(reader, 16),
               ActivityDate = reader.GetFieldValue<DateOnly>(8),
               LocalStartTime = ReadTimeOnly(reader, 9),
               LocalEndTime = ReadTimeOnly(reader, 18),
               EndsAt = ReadDateTimeOffset(reader, 19),
               ActivityGroupId = reader.IsDBNull(20)
                  ? null
                  : reader.GetGuid(20),
               ActivityGroupTitle = ReadString(reader, 21),
               NoGrouping = reader.GetBoolean(22),
               RelatedOrganizationCanonicalEntities =
                  reader.GetString(23),
               IsTeamSport = reader.GetBoolean(24),
               PublicDateMode = reader.GetString(25)
            }
         );
      }

      return activities;
   }

   private static TimeOnly? ReadTimeOnly(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetFieldValue<TimeOnly>(ordinal);
   }

   private static DateTimeOffset? ReadDateTimeOffset(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetFieldValue<DateTimeOffset>(ordinal);
   }

   private static Guid[] ReadGuidArray(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal)
         ? []
         : reader.GetFieldValue<Guid[]>(ordinal);
   }

   private static object BlankToDbNull(string? value)
   {
      return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
   }

   private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
   {
      return TimeZoneHelper.ToUtc(date, time, SportDay.TimeZoneId);
   }

   private static string? GetSportIconPath(string? iconId)
   {
      if(string.IsNullOrWhiteSpace(iconId))
      {
         return null;
      }

      var fileName = Regex.Replace(
            iconId.Trim().ToLowerInvariant(),
            "[^a-z0-9_-]+",
            "-"
         )
         .Trim('-');

      return $"/icons/sports/{fileName}.svg";
   }

   internal static string GetLinkedOrganizationNamesLateralSql(
      string entityAlias
   )
   {
      var entityIdSql = $"{entityAlias}.id";

      return $"""
         left join lateral (
            select string_agg(
               distinct organization_name,
               ', ' order by organization_name
            ) as organization_names
            from (
               select distinct
                  coalesce(entity.alias_name,
                     entity.canonical_name) as organization_name
               from entity_to_entity_links l
               join entities entity
                  on entity.id =
                     case
                        when source_entity_id = {entityIdSql}
                           then target_entity_id
                        else source_entity_id
                     end
               where (l.source_entity_id = {entityIdSql}
                     or l.target_entity_id = {entityIdSql})
                  and {BroadcastEntityFilter
                     .GetNonOrganizationEntityTypePredicateSql(
                        "entity.entity_type_id"
                     )}
            ) organizations
         ) org on true
         """;
   }
}
