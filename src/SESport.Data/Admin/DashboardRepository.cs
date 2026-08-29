using Npgsql;

using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Configuration;
using SESport.Core.Domain;
using SESport.Core.Sources;
using SESport.Data.Activities;
using SESport.Data.Models;

namespace SESport.Data.Admin;

public sealed class DashboardRepository(NpgsqlDataSource dataSource)
{
   public async Task<AdminDashboardSnapshot> GetAsync(
      DateTimeOffset now,
      CancellationToken cancellationToken
   )
   {
      var sql = $$"""
         with dates as (
            select generate_series(
               @today::date,
               @coverage_end::date,
               interval '1 day'
            )::date as date
         ),
         broadcast_stats as (
            select
               (
                  (
                     b.starts_at at time zone @time_zone
                  ) - @sport_day_cutoff
               )::date as date,
               count(*)::int as visible_count,
               count(*) filter (
                  where b.entity_id is null
                     and b.activity_group_source_activity_id is null
                     and b.activity_group_draft_title is null
                     and not exists (
                        select 1
                        from ai_job_runs run
                        where run.job_id = @participation_job_id
                           and run.correlation_id = b.id::text
                     )
               )::int as unreviewed_count
            from broadcasts b
            where b.hidden_at is null
               and b.starts_at >= (
                  @today::date + @sport_day_cutoff
               ) at time zone @time_zone
               and b.starts_at < (
                  (@coverage_end::date + 1) + @sport_day_cutoff
               ) at time zone @time_zone
            group by date
         ),
         activity_stats as (
            select
               a.activity_date as date,
               count(*) filter (
                  where a.publication_status_id = @published_status
               )::int as published_count,
               count(*) filter (
                  where a.publication_status_id = @draft_status
               )::int as draft_count
            from activities a
            where a.activity_date >= @today
               and a.activity_date <= @coverage_end
            group by a.activity_date
         )
         select
            dates.date,
            coalesce(broadcast_stats.visible_count, 0),
            coalesce(broadcast_stats.unreviewed_count, 0),
            coalesce(activity_stats.published_count, 0),
            coalesce(activity_stats.draft_count, 0)
         from dates
         left join broadcast_stats using(date)
         left join activity_stats using(date)
         order by dates.date;

         with public_activity_variants as (
            select
               a.id,
               a.activity_group_id,
               a.updated_at,
               a.created_at,
               coalesce(ag.no_grouping, false) as no_grouping,
               coalesce(s.is_team_sport, false) as is_team_sport,
               case
                  when coalesce(s.is_team_sport, false)
                  then upper(regexp_replace(
                     btrim(a.title),
                     '[[:space:]]+',
                     ' ',
                     'g'
                  ))
               end as normalized_title,
               case
                  when coalesce(
                     ag.public_date_mode,
                     '{{ActivityGroupPublicDateModeIds.SportDay}}'
                  ) =
                     '{{ActivityGroupPublicDateModeIds.LocalCalendarDate}}'
                  then (a.starts_at at time zone @time_zone)::date
                  else (
                     (a.starts_at at time zone @time_zone) -
                     @sport_day_cutoff
                  )::date
               end as display_date
            from activities a
            left join activity_groups ag
               on ag.id = a.activity_group_id
            join sports s on s.id = a.sport_id
            where a.publication_status_id = @published_status
               and a.starts_at is not null
               and a.ends_at is not null
               {{PublicActivityQuerySupport.ExclusionClause}}
         ),
         upcoming as (
            select
               a.id,
               a.activity_date,
               a.title,
               a.publication_status_id,
               a.description is null
                  or btrim(a.description) = ''
                  as missing_description,
               not exists (
                  select 1
                  from activity_entity_links link
                  join entities entity on entity.id = link.entity_id
                  where link.activity_id = a.id
                     and entity.entity_type_id = @person_type
               ) as no_participants,
               a.activity_group_id is null as no_group,
               not exists (
                  select 1
                  from activities source_activity
                  join sources source
                     on source.correlation_type =
                        @activity_correlation
                     and source.correlation_id =
                        source_activity.id::text
                  where source_activity.id = a.id
                     or (
                        a.activity_group_id is not null
                        and source_activity.activity_group_id =
                           a.activity_group_id
                     )
               ) as no_related_source,
               coalesce(s.requires_start_time, false)
                  and coalesce(
                     a.starts_at,
                     (
                        a.activity_date
                        + coalesce(
                           a.local_start_time,
                           time '23:59:59'
                        )
                     ) at time zone a.time_zone_id
                  ) <= @participant_start_time_cutoff
                  and exists (
                     select 1
                     from activity_entity_links participant_link
                     join entities participant
                        on participant.id = participant_link.entity_id
                     where participant_link.activity_id = a.id
                        and participant_link.is_active
                        and participant.entity_type_id = @person_type
                        and not exists (
                           select 1
                           from activity_participant_ai_results result
                           where result.activity_id = a.id
                              and result.entity_id =
                                 participant_link.entity_id
                              and result.job_id =
                                 @participant_start_job_id
                              and result.field_key =
                                 @participant_start_field_key
                              and nullif(
                                 btrim(result.value_text), ''
                              ) is not null
                        )
                        and not exists (
                           select 1
                           from public_activity_variants current_variant
                           join public_activity_variants variant
                              on variant.activity_group_id =
                                 current_variant.activity_group_id
                              and variant.display_date =
                                 current_variant.display_date
                              and not variant.no_grouping
                              and variant.id <> current_variant.id
                              and (
                                 not current_variant.is_team_sport
                                 or variant.normalized_title =
                                    current_variant.normalized_title
                              )
                           where current_variant.id = a.id
                              and not current_variant.no_grouping
                              and exists (
                                 select 1
                                 from activity_entity_links variant_link
                                 join entities variant_participant
                                    on variant_participant.id =
                                       variant_link.entity_id
                                 where variant_link.activity_id = variant.id
                                    and variant_link.entity_id =
                                       participant_link.entity_id
                                    and variant_link.is_active
                                    and variant_participant.entity_type_id =
                                       @person_type
                                    and exists (
                                       select 1
                                       from lateral (
                                          select nullif(
                                             btrim(
                                                variant_result.value_text
                                             ),
                                             ''
                                          ) as start_time
                                          from
                                             activity_participant_ai_results
                                                variant_result
                                          where variant_result.activity_id =
                                             variant.id
                                             and variant_result.entity_id =
                                                variant_link.entity_id
                                             and variant_result.job_id =
                                                @participant_start_job_id
                                             and variant_result.field_key =
                                                @participant_start_field_key
                                             and variant_result.updated_at >=
                                                variant.updated_at
                                          order by
                                             variant_result.updated_at desc,
                                             variant_result.sort_order asc,
                                             variant_result.id desc
                                          limit 1
                                       ) variant_start
                                       where
                                          variant_start.start_time is not null
                                    )
                              )
                        )
                  ) as activity_missing_participant_start_time,
               a.publication_status_id = @published_status
                  and exists (
                     select 1
                     from activity_entity_links participant_link
                     join entities participant
                        on participant.id = participant_link.entity_id
                     where participant_link.activity_id = a.id
                        and participant_link.is_active
                        and participant.entity_type_id = @person_type
                        and (
                           participant.birthdate is null
                           or nullif(
                              btrim(participant.formative_club), ''
                           ) is null
                        )
                  ) as participant_missing_person_data,
               coalesce(
                  (
                     (a.starts_at at time zone @time_zone)
                     - @sport_day_cutoff
                  )::date,
                  a.activity_date
               ) as participant_activity_date
            from activities a
            left join sports s on s.id = a.sport_id
            where a.activity_date >= @today
               and a.activity_date <= @activity_end
               and coalesce(
               a.starts_at,
               (
                  a.activity_date
                  + coalesce(
                     a.local_start_time,
                     time '23:59:59'
                  )
               ) at time zone a.time_zone_id
               ) > @now
         ),
         missing_start_groups as (
            select
               variant.activity_group_id,
               variant.display_date,
               variant.is_team_sport,
               variant.normalized_title,
               (
                  array_agg(
                     variant.id
                     order by variant.created_at, variant.id
                  )
               )[1] as representative_id
            from public_activity_variants variant
            join upcoming activity
               on activity.id = variant.id
            where variant.activity_group_id is not null
               and not variant.no_grouping
               and activity.activity_missing_participant_start_time
            group by
               variant.activity_group_id,
               variant.display_date,
               variant.is_team_sport,
               variant.normalized_title
         ),
         dashboard_issues as (
            select
               activity.*,
               activity.activity_missing_participant_start_time
                  and (
                     public_variant.id is null
                     or public_variant.activity_group_id is null
                     or public_variant.no_grouping
                     or coalesce(
                        missing_group.representative_id = activity.id,
                        false
                     )
                  ) as missing_participant_start_time
            from upcoming activity
            left join public_activity_variants public_variant
               on public_variant.id = activity.id
            left join missing_start_groups missing_group
               on missing_group.activity_group_id =
                  public_variant.activity_group_id
               and missing_group.display_date =
                  public_variant.display_date
               and missing_group.is_team_sport =
                  public_variant.is_team_sport
               and missing_group.normalized_title is not distinct from
                  public_variant.normalized_title
         )
         select
            id,
            activity_date,
            title,
            publication_status_id,
            publication_status_id = @draft_status,
            missing_description,
            no_participants,
            no_group,
            no_related_source,
            missing_participant_start_time,
            participant_missing_person_data,
            participant_activity_date
         from dashboard_issues
         where publication_status_id = @draft_status
            or missing_description
            or no_participants
            or no_group
            or no_related_source
            or missing_participant_start_time
            or participant_missing_person_data
         order by
            (publication_status_id = @draft_status) desc,
            activity_date,
            no_participants desc,
            missing_description desc,
            no_group desc,
            no_related_source desc,
            missing_participant_start_time desc,
            participant_missing_person_data desc,
            title
         limit {{DashboardDefaults.ActivityIssueLimit}};

         select
            count(*) filter (
               where status_id = @pending_status
            )::int,
            count(*) filter (
               where status_id = @running_status
            )::int,
            count(*) filter (
               where status_id = @running_status
                  and started_at < @stale_running_before
            )::int,
            count(*) filter (
               where status_id = @failed_status
                  and started_at >= @failed_since
            )::int
         from ai_job_runs
         where status_id in (
            @pending_status,
            @running_status
         )
            or (
               status_id = @failed_status
               and started_at >= @failed_since
            );

         select
            run.source_key,
            run.status,
            run.broadcast_count,
            run.started_at,
            run.finished_at
         from broadcast_import_runs run
         where run.status <> @completed_import_status
            or exists (
               select 1
               from broadcasts broadcast
               where broadcast.import_run_id = run.id
            )
         order by run.started_at desc
         limit 1;

         select
            id,
            target_type_id,
            text,
            correlation_id,
            created_at
         from todos
         where done_at is null
         order by created_at, id;
         """;

      var today = SportDay.GetSportDate(now);

      await using var command = dataSource.CreateCommand(sql);
      AddParameters(command, now, today);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      var dates = await ReadDatesAsync(reader, cancellationToken);
      await reader.NextResultAsync(cancellationToken);
      var issues = await ReadActivityIssuesAsync(
         reader,
         cancellationToken
      );
      await reader.NextResultAsync(cancellationToken);
      var aiHealth = await ReadAiHealthAsync(reader, cancellationToken);
      await reader.NextResultAsync(cancellationToken);
      var importHealth = await ReadImportHealthAsync(
         reader,
         cancellationToken
      );
      await reader.NextResultAsync(cancellationToken);
      var todos = await ReadTodosAsync(reader, cancellationToken);

      return new AdminDashboardSnapshot(
         dates,
         issues,
         aiHealth,
         importHealth,
         todos
      );
   }

   private static void AddParameters(
      NpgsqlCommand command,
      DateTimeOffset now,
      DateOnly today
   )
   {
      command.Parameters.AddWithValue("today", today);
      command.Parameters.AddWithValue(
         "coverage_end",
         today.AddDays(DashboardDefaults.CoverageDayCount - 1)
      );
      command.Parameters.AddWithValue(
         "activity_end",
         today.AddDays(DashboardDefaults.ActivityHorizonDayCount)
      );
      command.Parameters.AddWithValue("now", now);
      command.Parameters.AddWithValue(
         "participant_start_time_cutoff",
         now.AddHours(
            DashboardDefaults.ParticipantStartTimeWarningHorizonHours
         )
      );
      command.Parameters.AddWithValue(
         "failed_since",
         now.AddHours(-DashboardDefaults.FailedAiRunLookbackHours)
      );
      command.Parameters.AddWithValue(
         "stale_running_before",
         now - AiWorkerDefaults.RunTimeoutStaleAge
      );
      command.Parameters.AddWithValue(
         "sport_day_cutoff",
         SportDay.Cutoff.ToTimeSpan()
      );
      command.Parameters.AddWithValue(
         "time_zone",
         SportDay.TimeZoneId
      );
      command.Parameters.AddWithValue(
         "participation_job_id",
         AiJobIds.DecidePrimaryCountryParticipation
      );
      command.Parameters.AddWithValue(
         "participant_start_job_id",
         AiJobIds.FindParticipantsStart
      );
      command.Parameters.AddWithValue(
         "participant_start_field_key",
         ActivityParticipantAiFieldKeys.StartTime
      );
      command.Parameters.AddWithValue(
         "published_status",
         ActivityPublicationStatusIds.Published
      );
      command.Parameters.AddWithValue(
         "draft_status",
         ActivityPublicationStatusIds.Draft
      );
      command.Parameters.AddWithValue(
         "pending_status",
         AiJobRunStatusIds.Pending
      );
      command.Parameters.AddWithValue(
         "running_status",
         AiJobRunStatusIds.Running
      );
      command.Parameters.AddWithValue(
         "failed_status",
         AiJobRunStatusIds.Failed
      );
      command.Parameters.AddWithValue(
         "person_type",
         TrackedEntityTypeIds.Person
      );
      command.Parameters.AddWithValue(
         "activity_correlation",
         SourceCorrelationTypes.Activity
      );
      command.Parameters.AddWithValue(
         "completed_import_status",
         BroadcastImportRunStatus.Completed.ToString()
      );
      PublicActivityQuerySupport.AddExclusionParameters(command);
   }

   private static async Task<IReadOnlyList<DashboardDateSummary>>
      ReadDatesAsync(
         NpgsqlDataReader reader,
         CancellationToken cancellationToken
      )
   {
      var dates = new List<DashboardDateSummary>();

      while(await reader.ReadAsync(cancellationToken))
      {
         dates.Add(
            new DashboardDateSummary(
               reader.GetFieldValue<DateOnly>(0),
               reader.GetInt32(1),
               reader.GetInt32(2),
               reader.GetInt32(3),
               reader.GetInt32(4)
            )
         );
      }

      return dates;
   }

   private static async Task<IReadOnlyList<DashboardActivityIssue>>
      ReadActivityIssuesAsync(
         NpgsqlDataReader reader,
         CancellationToken cancellationToken
      )
   {
      var issues = new List<DashboardActivityIssue>();

      while(await reader.ReadAsync(cancellationToken))
      {
         issues.Add(
            new DashboardActivityIssue(
               reader.GetGuid(0),
               reader.GetFieldValue<DateOnly>(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetBoolean(4),
               reader.GetBoolean(5),
               reader.GetBoolean(6),
               reader.GetBoolean(7),
               reader.GetBoolean(8),
               reader.GetBoolean(9),
               reader.GetBoolean(10),
               reader.GetFieldValue<DateOnly>(11)
            )
         );
      }

      return issues;
   }

   private static async Task<DashboardAiHealth> ReadAiHealthAsync(
      NpgsqlDataReader reader,
      CancellationToken cancellationToken
   )
   {
      await reader.ReadAsync(cancellationToken);

      return new DashboardAiHealth(
         reader.GetInt32(0),
         reader.GetInt32(1),
         reader.GetInt32(2),
         reader.GetInt32(3)
      );
   }

   private static async Task<DashboardImportHealth?> ReadImportHealthAsync(
      NpgsqlDataReader reader,
      CancellationToken cancellationToken
   )
   {
      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return new DashboardImportHealth(
         reader.GetString(0),
         reader.GetString(1),
         reader.GetInt32(2),
         reader.GetFieldValue<DateTimeOffset>(3),
         reader.IsDBNull(4)
            ? null
            : reader.GetFieldValue<DateTimeOffset>(4)
      );
   }

   private static async Task<IReadOnlyList<TodoItem>> ReadTodosAsync(
      NpgsqlDataReader reader,
      CancellationToken cancellationToken
   )
   {
      var todos = new List<TodoItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         todos.Add(
            new TodoItem(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.IsDBNull(3) ? null : reader.GetString(3),
               reader.GetFieldValue<DateTimeOffset>(4)
            )
         );
      }

      return todos;
   }
}
