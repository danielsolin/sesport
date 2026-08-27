using System.Text;
using System.Text.RegularExpressions;

using Npgsql;

using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class ActivityQueryRepository(NpgsqlDataSource dataSource)
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
      CancellationToken cancellationToken,
      Guid? watchedByMemberId = null
   )
   {
      return await GetPublishedActivitiesAsync(
         SportDay.ForDate(date),
         cancellationToken,
         watchedByMemberId
      );
   }

   public async Task<IReadOnlyList<ActivityListItem>>
      GetPublishedFutureForMemberWatchesAsync(
         Guid memberId,
         DateTimeOffset now,
         CancellationToken cancellationToken
      )
   {
      var activities = await QueryActivityListAsync(
         $$"""
            where a.publication_status_id =
               '{{ActivityPublicationStatusIds.Published}}'
               and a.starts_at is not null
               and (
                  a.starts_at > @now
                  or a.ends_at > @now
               )
               and exists (
                  select 1
                  from activity_entity_links watched_link
                  join member_entity_watches watch
                     on watch.entity_id = watched_link.entity_id
                     and watch.member_id = @member_id
                  join entities watched_person
                     on watched_person.id = watched_link.entity_id
                  where watched_link.activity_id = a.id
                     and watched_link.is_active
                     and watched_person.entity_type_id =
                        '{{TrackedEntityTypeIds.Person}}'
               )
               {{PublicActivityQuerySupport.ExclusionClause}}
         """,
         TimedOrderClause,
         """
         coalesce(s.display_name, s.name)
         """,
         command =>
         {
            command.Parameters.AddWithValue("member_id", memberId);
            command.Parameters.AddWithValue("now", now);
            PublicActivityQuerySupport.AddExclusionParameters(command);
         },
         cancellationToken,
         watchedByMemberId: memberId
      );

      return await ApplyNationalTeamFlagsAsync(
         activities,
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
               {{PublicActivityQuerySupport.ExclusionClause}}
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
      PublicActivityQuerySupport.AddExclusionParameters(command);
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
         CancellationToken cancellationToken,
         Guid? watchedByMemberId = null
      )
   {
      var activities = await QueryActivityListAsync(
         $$"""
            where a.publication_status_id =
               '{{ActivityPublicationStatusIds.Published}}'
               and {{BuildTimedDateFilterSql()}}
               {{PublicActivityQuerySupport.ExclusionClause}}
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
            PublicActivityQuerySupport.AddExclusionParameters(command);
         },
         cancellationToken,
         watchedByMemberId: watchedByMemberId
      );

      return await ApplyNationalTeamFlagsAsync(
         activities,
         cancellationToken
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

      var sql = $$"""
         select a.id
         from activities a
         join entities org on org.id =
            {{GetActivityOrganizationEntityIdSql("a")}}
         where a.id = any(@activity_ids)
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
         entities.Add(ReadEntityOption(reader));
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
         entities.Add(ReadEntityOption(reader));
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
         entities.Add(ReadEntityOption(reader));
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
            ag.title as activity_group_title,
            a.organization_entity_id
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
         ActivityGroupTitle = ReadString(reader, 13),
         OrganizationEntityId = reader.IsDBNull(14)
            ? null
            : reader.GetGuid(14)
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
      Guid? legacyOrganizationEntityId = null;
      var hasLegacyOrganizationConflict = false;

      while(await linkReader.ReadAsync(cancellationToken))
      {
         model.LinkedEntityIds.Add(linkReader.GetGuid(0));

         if(linkReader.IsDBNull(1))
         {
            continue;
         }

         var organizationEntityId = linkReader.GetGuid(1);
         if(legacyOrganizationEntityId is null)
         {
            legacyOrganizationEntityId = organizationEntityId;
         }
         else if(legacyOrganizationEntityId != organizationEntityId)
         {
            hasLegacyOrganizationConflict = true;
         }
      }

      if(model.OrganizationEntityId is null &&
         !hasLegacyOrganizationConflict)
      {
         model.OrganizationEntityId = legacyOrganizationEntityId;
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

   private async Task<IReadOnlyList<ActivityListItem>> QueryActivityListAsync(
      string whereClause,
      string orderClause,
      string sportNameExpression,
      Action<NpgsqlCommand>? configureCommand,
      CancellationToken cancellationToken,
      Guid? watchedByMemberId = null
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
         watchedByMemberId,
         cancellationToken
      );
      var sourcesByActivity = await GetActivitySourcesAsync(
         activities.Select(activity => activity.Id).ToArray(),
         cancellationToken
      );

      return activities
         .Select(activity => activity with
         {
            Participants = participantsByActivity.GetValueOrDefault(
               activity.Id,
               []
            ),
            Sources = sourcesByActivity.GetValueOrDefault(
               activity.Id,
               []
            )
         })
         .ToArray();
   }

   private async Task<IReadOnlyDictionary<
      Guid,
      IReadOnlyList<ActivitySourceListItem>
   >> GetActivitySourcesAsync(
      Guid[] activityIds,
      CancellationToken cancellationToken
   )
   {
      if(activityIds.Length == 0)
      {
         return new Dictionary<
            Guid,
            IReadOnlyList<ActivitySourceListItem>
         >();
      }

      const string sql = """
         with requested_activities as (
            select
               id,
               activity_group_id
            from activities
            where id = any(@activity_ids)
         ),
         matched_sources as (
            select
               requested.id as activity_id,
               s.kind,
               s.url,
               s.title,
               s.observed_at,
               s.created_at,
               s.id as source_id
            from requested_activities requested
            join sources s
               on s.correlation_type = @activity_correlation_type
               and s.correlation_id = requested.id::text
            union all
            select
               requested.id as activity_id,
               s.kind,
               s.url,
               s.title,
               s.observed_at,
               s.created_at,
               s.id as source_id
            from requested_activities requested
            join sources s
               on s.correlation_type = @activity_group_correlation_type
               and s.correlation_id = requested.activity_group_id::text
            where requested.activity_group_id is not null
            union all
            select
               requested.id as activity_id,
               s.kind,
               s.url,
               s.title,
               s.observed_at,
               s.created_at,
               s.id as source_id
            from requested_activities requested
            join activities sibling
               on sibling.activity_group_id = requested.activity_group_id
               and sibling.id <> requested.id
            join sources s
               on s.correlation_type = @activity_correlation_type
               and s.correlation_id = sibling.id::text
               and s.kind = @participation_evidence_kind
            where requested.activity_group_id is not null
               and not exists (
                  select 1
                  from sources own_source
                  where own_source.kind = @participation_evidence_kind
                     and (
                        (
                           own_source.correlation_type =
                              @activity_correlation_type
                           and own_source.correlation_id =
                              requested.id::text
                        )
                        or (
                           own_source.correlation_type =
                              @activity_group_correlation_type
                           and own_source.correlation_id =
                              requested.activity_group_id::text
                        )
                     )
               )
         )
         select
            activity_id,
            kind,
            url,
            title
         from matched_sources
         order by
            activity_id,
            observed_at desc,
            created_at desc,
            source_id desc
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "activity_correlation_type",
         SourceCorrelationTypes.Activity
      );
      command.Parameters.AddWithValue(
         "activity_group_correlation_type",
         SourceCorrelationTypes.ActivityGroup
      );
      command.Parameters.AddWithValue(
         "participation_evidence_kind",
         SourceKinds.ParticipationEvidence
      );
      command.Parameters.AddWithValue(
         "activity_ids",
         activityIds
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var sources = new Dictionary<
         Guid,
         List<ActivitySourceListItem>
      >();

      while(await reader.ReadAsync(cancellationToken))
      {
         var activityId = reader.GetGuid(0);

         if(!sources.TryGetValue(activityId, out var activitySources))
         {
            activitySources = [];
            sources[activityId] = activitySources;
         }

         activitySources.Add(
            new ActivitySourceListItem(
               reader.GetString(1),
               reader.GetString(2),
               reader.IsDBNull(3) ? null : reader.GetString(3)
            )
         );
      }

      return sources.ToDictionary(
         pair => pair.Key,
         pair => (IReadOnlyList<ActivitySourceListItem>)pair.Value
      );
   }

   private async Task<IReadOnlyDictionary<
      Guid,
      IReadOnlyList<PublicActivityParticipant>
   >> GetPublicParticipantsAsync(
      Guid[] activityIds,
      Guid? watchedByMemberId,
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

      var watchedByMemberSql = watchedByMemberId is null
         ? "false"
         : """
            exists (
               select 1
               from member_entity_watches member_watch
               where member_watch.member_id = @member_id
                  and member_watch.entity_id = person.id
            )
            """;
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
            al.is_active,
            al.represented_entity_id is not null
               as has_represented_entity,
            coalesce(
               represented_entity.id is not null
               and represented_entity.entity_type_id <>
                  '{{TrackedEntityTypeIds.NationalTeam}}',
               false
            ) as has_non_national_team_representation,
            coalesce(
               nullif(btrim(represented_entity.alias_name), ''),
               represented_entity.canonical_name
            ) as represented_entity_name,
            represented_entity.canonical_name
               as represented_entity_canonical_name,
            participant_start.source_url,
            {{watchedByMemberSql}} as is_watched_by_member
         from activity_entity_links al
         join activities activity on activity.id = al.activity_id
         join entities person on person.id = al.entity_id
         left join entities represented_entity
            on represented_entity.id = al.represented_entity_id
         join entity_watch_priorities priority
            on priority.id = person.watch_priority_id
         left join lateral (
            select
               nullif(btrim(r.value_text), '') as start_time,
               start_source.url as source_url
            from activity_participant_ai_results r
            left join sources start_source
               on start_source.id = r.source_id
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
      if(watchedByMemberId is not null)
      {
         command.Parameters.AddWithValue(
            "member_id",
            watchedByMemberId.Value
         );
      }
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
               WatchPriority = reader.GetInt32(9),
               HasRepresentedEntity = reader.GetBoolean(13),
               HasNonNationalTeamRepresentation = reader.GetBoolean(14),
               RepresentedEntityName = reader.IsDBNull(15)
                  ? null
                  : reader.GetString(15),
               RepresentedEntityCanonicalName = reader.IsDBNull(16)
                  ? null
                  : reader.GetString(16),
               StartTimeSourceUrl = reader.IsDBNull(17)
                  ? null
                  : reader.GetString(17),
               IsWatchedByMember = reader.GetBoolean(18)
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
            $"'{ActivityGroupPublicDateModeIds.SportDay}'),"
         )
         .AppendLine("   ro.organization_country_id")
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
            "   ) as related_organization_canonical_entities,"
         )
         .AppendLine(
            "   max(organization_country_id) as " +
            "organization_country_id"
         )
         .AppendLine("   from (")
         .AppendLine("      select distinct")
         .AppendLine("         coalesce(context.alias_name,")
         .AppendLine(
            "            context.canonical_name) as organization_name,"
         )
         .AppendLine(
            "         context.canonical_name as organization_canonical_name,"
         )
         .AppendLine("         context.country_id as organization_country_id")
         .AppendLine("      from entities context")
         .AppendLine(
            "      where context.id = " +
            GetActivityOrganizationEntityIdSql("a")
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
         .AppendLine("         ro.organization_country_id,")
         .AppendLine(
            "         a.ends_at, ag.title, ag.no_grouping, " +
            "s.is_team_sport, ag.public_date_mode"
         )
         .AppendLine(orderClause);

      return builder.ToString();
   }

   internal static string GetActivityOrganizationEntityIdSql(
      string activityAlias
   )
   {
      return $"""
         coalesce(
            {activityAlias}.organization_entity_id,
            (
               select
                  (array_agg(legacy_link.organization_entity_id))[1]
               from activity_entity_links legacy_link
               where legacy_link.activity_id = {activityAlias}.id
                  and legacy_link.organization_entity_id is not null
               having count(distinct legacy_link.organization_entity_id) = 1
            )
         )
         """;
   }
   private async Task<IReadOnlyList<LookupOption>> GetLookupOptionsAsync(
      string sql,
      CancellationToken cancellationToken
   )
   {
      await using var command = dataSource.CreateCommand(sql);
      return await ReadLookupOptionsAsync(command, cancellationToken);
   }

   internal static async Task<IReadOnlyList<LookupOption>>
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
   internal static string? ReadString(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static EntityOption ReadEntityOption(NpgsqlDataReader reader)
   {
      return new EntityOption(
         reader.GetGuid(0),
         reader.GetString(1),
         reader.GetString(2),
         reader.GetString(3),
         reader.GetString(4),
         ReadString(reader, 5),
         ReadString(reader, 6)
      );
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
               PublicDateMode = reader.GetString(25),
               OrganizationCountryId = ReadString(reader, 26)
            }
         );
      }

      return activities;
   }

   internal static TimeOnly? ReadTimeOnly(
      NpgsqlDataReader reader,
      int ordinal
   )
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
