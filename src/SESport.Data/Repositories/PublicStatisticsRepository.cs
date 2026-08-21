using Npgsql;

using SESport.Core.Configuration;
using SESport.Core.Domain;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class PublicStatisticsRepository(NpgsqlDataSource dataSource)
{
   public async Task<PublicStatisticsSnapshot> GetMonthlyAsync(
      DateOnly month,
      int leaderRankLimit,
      CancellationToken cancellationToken
   )
   {
      var monthStart = new DateOnly(month.Year, month.Month, 1);
      var nextMonth = monthStart.AddMonths(1);
      var sql = $$"""
         with public_activities as (
            select
               a.id,
               a.sport_id,
               case
                  when a.starts_at is null then a.activity_date
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
         ),
         person_days as (
            select distinct
               person.id as person_id,
               person.canonical_name as person_name,
               activity.display_date,
               activity.sport_id
            from public_activities activity
            join activity_entity_links activity_link
               on activity_link.activity_id = activity.id
               and activity_link.is_active
            join entities person
               on person.id = activity_link.entity_id
               and person.entity_type_id =
                  '{{TrackedEntityTypeIds.Person}}'
            where activity.display_date >= @month_start
               and activity.display_date < @next_month
         ),
         person_counts as (
            select
               person_id,
               person_name,
               count(distinct display_date)::int as points
            from person_days
            group by person_id, person_name
         ),
         ranked_people as (
            select
               person_counts.person_id,
               person_counts.person_name,
               person_counts.points,
               rank() over (
                  order by person_counts.points desc
               )::int as rank,
               count(*) over ()::int as participant_count
            from person_counts
         )
         select
            ranked_people.rank,
            ranked_people.person_name,
            coalesce(
               string_agg(
                  distinct coalesce(
                     nullif(s.display_name, ''),
                     s.name
                  ),
                  ', ' order by coalesce(
                     nullif(s.display_name, ''),
                     s.name
                  )
               ),
               ''
            ) as sport_names,
            ranked_people.points,
            ranked_people.participant_count
         from ranked_people
         join person_days
            on person_days.person_id = ranked_people.person_id
         join sports s
            on s.id = person_days.sport_id
         where ranked_people.rank <= @rank_limit
         group by
            ranked_people.person_id,
            ranked_people.rank,
            ranked_people.person_name,
            ranked_people.points,
            ranked_people.participant_count
         order by ranked_people.rank, ranked_people.person_name;
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("month_start", monthStart);
      command.Parameters.AddWithValue("next_month", nextMonth);
      command.Parameters.AddWithValue("time_zone", SportDay.TimeZoneId);
      command.Parameters.AddWithValue(
         "cutoff",
         SportDay.Cutoff.ToTimeSpan()
      );
      command.Parameters.AddWithValue(
         "rank_limit",
         Math.Max(leaderRankLimit, 1)
      );
      PublicActivityQuerySupport.AddExclusionParameters(command);

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var leaders = new List<PublicStatisticsLeader>();
      var participantCount = 0;

      while(await reader.ReadAsync(cancellationToken))
      {
         participantCount = reader.GetInt32(4);
         leaders.Add(
            new PublicStatisticsLeader(
               reader.GetInt32(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetInt32(3)
            )
         );
      }

      return new PublicStatisticsSnapshot(participantCount, leaders);
   }
}
