using Npgsql;

using NpgsqlTypes;

using SESport.Core.AI;
using SESport.Core.Domain;
using SESport.Data.Models;

namespace SESport.Data.Activities;

public sealed class ActivityReadRepository(NpgsqlDataSource dataSource)
{
   public async Task<ActivitySearchPage> SearchAsync(
      string? text,
      DateOnly? date,
      string? sport,
      int limit,
      int offset,
      CancellationToken cancellationToken
   )
   {
      var localCalendarDateMode =
         ActivityGroupPublicDateModeIds.LocalCalendarDate;
      var sql = $$"""
         with candidate_activities as (
            select
               a.id,
               a.title,
               s.id as sport_id,
               coalesce(s.display_name, s.name) as sport_name,
               at.id as activity_type_id,
               at.label as activity_type_name,
               a.activity_date,
               a.local_start_time,
               a.starts_at,
               a.ends_at,
               ag.id as activity_group_id,
               coalesce(ag.no_grouping, false) as no_grouping,
               coalesce(
                  ag.public_date_mode,
                  '{{ActivityGroupPublicDateModeIds.SportDay}}'
               ) as public_date_mode,
               s.is_team_sport,
               case
                  when a.starts_at is null then null
                  when coalesce(
                     ag.public_date_mode,
                     '{{ActivityGroupPublicDateModeIds.SportDay}}'
                  ) =
                     '{{localCalendarDateMode}}'
                  then (a.starts_at at time zone @time_zone)::date
                  else (
                     (a.starts_at at time zone @time_zone) - @cutoff
                  )::date
               end as display_date,
               participant_names.names as participant_names
            from activities a
            join sports s on s.id = a.sport_id
            join activity_types at on at.id = a.activity_type_id
            left join activity_groups ag
               on ag.id = a.activity_group_id
            left join lateral (
               select coalesce(
                  array_agg(
                     participant.canonical_name
                     order by participant.canonical_name
                  ),
                  '{}'::text[]
               ) as names
               from activity_entity_links participant_link
               join entities participant
                  on participant.id = participant_link.entity_id
               where participant_link.activity_id = a.id
                  and participant.entity_type_id in (
                     '{{TrackedEntityTypeIds.Person}}',
                     '{{TrackedEntityTypeIds.NationalTeam}}',
                     '{{TrackedEntityTypeIds.Pair}}'
                  )
            ) participant_names on true
            where a.publication_status_id =
               '{{ActivityPublicationStatusIds.Published}}'
               and (
                  @text is null
                  or a.title ilike @text escape '\'
                  or coalesce(a.description, '') ilike @text escape '\'
                  or coalesce(ag.title, '') ilike @text escape '\'
                  or exists (
                     select 1
                     from activity_entity_links participant_link
                     join entities participant
                        on participant.id = participant_link.entity_id
                     where participant_link.activity_id = a.id
                        and participant.entity_type_id in (
                           '{{TrackedEntityTypeIds.Person}}',
                           '{{TrackedEntityTypeIds.NationalTeam}}',
                           '{{TrackedEntityTypeIds.Pair}}'
                        )
                        and (
                           participant.canonical_name ilike @text
                              escape '\'
                           or coalesce(participant.alias_name, '')
                              ilike @text escape '\'
                        )
                  )
                  or exists (
                     select 1
                     from entities organization
                     where organization.id =
                        {{ActivityQueryRepository
                           .GetActivityOrganizationEntityIdSql("a")}}
                        and (
                           organization.canonical_name ilike @text
                              escape '\'
                           or coalesce(organization.alias_name, '')
                              ilike @text escape '\'
                        )
                  )
               )
               and (
                  @sport is null
                  or s.id ilike @sport escape '\'
                  or s.name ilike @sport escape '\'
                  or coalesce(s.display_name, '') ilike @sport
                     escape '\'
               )
               and (
                  @date is null
                  or (
                     a.starts_at is not null
                     and case
                        when coalesce(
                           ag.public_date_mode,
                           '{{ActivityGroupPublicDateModeIds.SportDay}}'
                        ) =
                           '{{localCalendarDateMode}}'
                        then (a.starts_at at time zone @time_zone)::date
                        else (
                           (a.starts_at at time zone @time_zone) - @cutoff
                        )::date
                     end = @date
                  )
                  or (
                     a.starts_at is null
                     and a.activity_date = @date
                  )
               )
         ),
         prepared_activities as (
            select
               candidate_activities.*,
               activity_group_id is not null
                  and not no_grouping
                  and starts_at is not null
                  and ends_at is not null as can_group
            from candidate_activities
         ),
         keyed_activities as (
            select
               prepared_activities.*,
               case
                  when can_group then activity_group_id
                  else id
               end as grouping_id,
               case
                  when can_group and is_team_sport then
                     upper(regexp_replace(trim(title), '\s+', ' ', 'g'))
                  else null
               end as grouping_team_title
            from prepared_activities
         ),
         ranked_activities as (
            select
               keyed_activities.*,
               row_number() over (
                  partition by
                     grouping_id,
                     display_date,
                     grouping_team_title
                  order by
                     starts_at nulls last,
                     title,
                     id
               ) as grouping_row_number
            from keyed_activities
         )
         select
            id,
            title,
            sport_id,
            sport_name,
            activity_type_id,
            activity_type_name,
            activity_date,
            local_start_time,
            starts_at,
            participant_names
         from ranked_activities
         where grouping_row_number = 1
         order by
            starts_at nulls last,
            activity_date,
            local_start_time nulls last,
            lower(title),
            id
         offset @offset
         limit @limit
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.Add(
         new NpgsqlParameter("text", NpgsqlDbType.Text)
         {
            Value = CreateLikePattern(text) ?? (object)DBNull.Value
         }
      );
      command.Parameters.Add(
         new NpgsqlParameter("sport", NpgsqlDbType.Text)
         {
            Value = CreateLikePattern(sport) ?? (object)DBNull.Value
         }
      );
      command.Parameters.Add(
         new NpgsqlParameter("date", NpgsqlDbType.Date)
         {
            Value = date ?? (object)DBNull.Value
         }
      );
      command.Parameters.AddWithValue("time_zone", SportDay.TimeZoneId);
      command.Parameters.AddWithValue(
         "cutoff",
         NpgsqlDbType.Time,
         SportDay.Cutoff.ToTimeSpan()
      );
      command.Parameters.AddWithValue("offset", offset);
      command.Parameters.AddWithValue("limit", checked(limit + 1));

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var rows = new List<ActivitySearchReadModel>();

      while(await reader.ReadAsync(cancellationToken))
      {
         rows.Add(
            new ActivitySearchReadModel(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.GetString(5),
               reader.GetFieldValue<DateOnly>(6),
               ReadTimeOnly(reader, 7),
               ReadDateTimeOffset(reader, 8),
               reader.GetFieldValue<string[]>(9)
            )
         );
      }

      var hasMore = rows.Count > limit;
      if(hasMore)
      {
         rows.RemoveAt(rows.Count - 1);
      }

      return new ActivitySearchPage(rows, hasMore);
   }

   public async Task<ActivityReadModel?> GetPublishedAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      var sql = $$"""
         select
            a.id,
            a.title,
            a.description,
            s.id,
            coalesce(s.display_name, s.name),
            at.id,
            at.label,
            a.activity_date,
            a.local_start_time,
            a.local_end_time,
            a.starts_at,
            a.ends_at,
            a.time_zone_id,
            ag.id,
            ag.title,
            organization.id,
            coalesce(
               organization.alias_name,
               organization.canonical_name
            )
         from activities a
         join sports s on s.id = a.sport_id
         join activity_types at on at.id = a.activity_type_id
         left join activity_groups ag
            on ag.id = a.activity_group_id
         left join entities organization
            on organization.id =
               {{ActivityQueryRepository
                  .GetActivityOrganizationEntityIdSql("a")}}
         where a.id = @id
            and a.publication_status_id =
               '{{ActivityPublicationStatusIds.Published}}'
         """;

      ActivityReadModel activity;
      await using(var command = dataSource.CreateCommand(sql))
      {
         command.Parameters.AddWithValue("id", id);
         await using var reader = await command.ExecuteReaderAsync(
            cancellationToken
         );

         if(!await reader.ReadAsync(cancellationToken))
         {
            return null;
         }

         var activityGroup = reader.IsDBNull(13)
            ? null
            : new ActivityReadGroup(
               reader.GetGuid(13),
               reader.GetString(14)
            );
         var organization = reader.IsDBNull(15)
            ? null
            : new ActivityReadOrganization(
               reader.GetGuid(15),
               reader.GetString(16)
            );

         activity = new ActivityReadModel(
            reader.GetGuid(0),
            reader.GetString(1),
            ReadString(reader, 2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetFieldValue<DateOnly>(7),
            ReadTimeOnly(reader, 8),
            ReadTimeOnly(reader, 9),
            ReadDateTimeOffset(reader, 10),
            ReadDateTimeOffset(reader, 11),
            reader.GetString(12),
            activityGroup,
            organization,
            []
         );
      }

      var participants = await GetParticipantsAsync(
         activity.Id,
         cancellationToken
      );
      return activity with { Participants = participants };
   }

   private async Task<IReadOnlyList<ActivityReadParticipant>>
      GetParticipantsAsync(
         Guid activityId,
         CancellationToken cancellationToken
      )
   {
      const string sql = $$"""
         select
            person.id,
            person.canonical_name,
            person.birthdate,
            person.formative_club,
            participant_start.start_time
         from activity_entity_links participant_link
         join activities activity
            on activity.id = participant_link.activity_id
         join entities person
            on person.id = participant_link.entity_id
         left join lateral (
            select nullif(btrim(result.value_text), '') as start_time
            from activity_participant_ai_results result
            where result.activity_id = participant_link.activity_id
               and result.entity_id = person.id
               and result.job_id = '{{AiJobIds.FindParticipantsStart}}'
               and result.field_key =
                  '{{ActivityParticipantAiFieldKeys.StartTime}}'
               and result.updated_at >= activity.updated_at
            order by
               result.updated_at desc,
               result.sort_order asc,
               result.id desc
            limit 1
         ) participant_start on true
         where participant_link.activity_id = @activity_id
            and person.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
         order by
            person.canonical_name,
            person.id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var participants = new List<ActivityReadParticipant>();

      while(await reader.ReadAsync(cancellationToken))
      {
         participants.Add(
            new ActivityReadParticipant(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.IsDBNull(2)
                  ? null
                  : reader.GetFieldValue<DateOnly>(2),
               ReadString(reader, 3),
               ReadString(reader, 4)
            )
         );
      }

      return participants;
   }

   private static string? CreateLikePattern(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      var escaped = value.Trim()
         .Replace("\\", "\\\\", StringComparison.Ordinal)
         .Replace("%", "\\%", StringComparison.Ordinal)
         .Replace("_", "\\_", StringComparison.Ordinal);
      return $"%{escaped}%";
   }

   private static string? ReadString(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static TimeOnly? ReadTimeOnly(
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
}
