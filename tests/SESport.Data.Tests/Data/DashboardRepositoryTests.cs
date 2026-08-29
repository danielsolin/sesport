using Npgsql;

using SESport.Core.Formatting;
using SESport.Core.Sources;

namespace SESport.Core.Tests.Data;

public sealed class DashboardRepositoryTests
{
   [Fact]
   public async Task GetAsyncDoesNotFlagStartTimeCoveredByPublicGroup()
   {
      var activityDate = DistantActivityDate;
      var now = ToUtc(activityDate, new TimeOnly(8, 0));
      var activityGroupId = Guid.NewGuid();
      var currentActivityId = Guid.NewGuid();
      var variantActivityId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var runId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();

      try
      {
         await InsertTestDataAsync(
            dataSource,
            activityDate,
            activityGroupId,
            currentActivityId,
            variantActivityId,
            personId,
            runId
         );

         var repository = new DashboardRepository(dataSource);
         var dashboard = await repository.GetAsync(
            now,
            CancellationToken.None
         );
         var issue = dashboard.ActivityIssues.Single(
            item => item.Id == currentActivityId
         );

         Assert.False(issue.HasMissingParticipantStartTime);
      }
      finally
      {
         await DeleteTestDataAsync(
            dataSource,
            activityGroupId,
            currentActivityId,
            variantActivityId,
            personId,
            runId
         );
      }
   }

   [Fact]
   public async Task GetAsyncReportsOneMissingStartTimeForPublicGroup()
   {
      var activityDate = DistantActivityDate;
      var now = ToUtc(activityDate, new TimeOnly(8, 0));
      var activityGroupId = Guid.NewGuid();
      var currentActivityId = Guid.NewGuid();
      var variantActivityId = Guid.NewGuid();
      var personId = Guid.NewGuid();
      var runId = Guid.NewGuid();

      await using var dataSource = CreateDataSource();

      try
      {
         await InsertTestDataAsync(
            dataSource,
            activityDate,
            activityGroupId,
            currentActivityId,
            variantActivityId,
            personId,
            runId,
            string.Empty,
            true
         );

         var repository = new DashboardRepository(dataSource);
         var dashboard = await repository.GetAsync(
            now,
            CancellationToken.None
         );
         var groupIssues = dashboard.ActivityIssues
            .Where(item => item.Id == currentActivityId ||
               item.Id == variantActivityId)
            .ToList();

         var issue = Assert.Single(groupIssues);
         Assert.True(issue.HasMissingParticipantStartTime);
      }
      finally
      {
         await DeleteTestDataAsync(
            dataSource,
            activityGroupId,
            currentActivityId,
            variantActivityId,
            personId,
            runId
         );
      }
   }

   [Fact]
   public async Task GetAsyncReturnsCoverageAndHealth()
   {
      await using var dataSource = CreateDataSource();
      var repository = new DashboardRepository(dataSource);
      var now = DateTimeOffset.UtcNow;

      var dashboard = await repository.GetAsync(
         now,
         CancellationToken.None
      );

      Assert.Equal(
         DashboardDefaults.CoverageDayCount,
         dashboard.Dates.Count
      );
      Assert.Equal(
         SportDay.GetSportDate(now),
         dashboard.Dates[0].Date
      );
      Assert.All(
         dashboard.Dates,
         date =>
         {
            Assert.True(date.VisibleBroadcastCount >= 0);
            Assert.True(date.UnreviewedBroadcastCount >= 0);
            Assert.True(date.PublishedActivityCount >= 0);
            Assert.True(date.DraftActivityCount >= 0);
         }
      );
      Assert.True(dashboard.AiHealth.PendingCount >= 0);
      Assert.True(dashboard.AiHealth.RunningCount >= 0);
      Assert.True(dashboard.AiHealth.StaleRunningCount >= 0);
      Assert.True(dashboard.AiHealth.FailedLast25HoursCount >= 0);
   }

   private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
   {
      return TimeZoneHelper.ToUtc(date, time, SportDay.TimeZoneId);
   }

   private static async Task InsertTestDataAsync(
      NpgsqlDataSource dataSource,
      DateOnly activityDate,
      Guid activityGroupId,
      Guid currentActivityId,
      Guid variantActivityId,
      Guid personId,
      Guid runId,
      string variantStartTime = "10:30",
      bool includeDescription = false
   )
   {
      var currentStartsAt = ToUtc(activityDate, new TimeOnly(10, 0));
      var currentEndsAt = ToUtc(activityDate, new TimeOnly(12, 0));
      var variantStartsAt = ToUtc(activityDate, new TimeOnly(11, 0));
      var variantEndsAt = ToUtc(activityDate, new TimeOnly(13, 0));

      await using var connection = await dataSource.OpenConnectionAsync();
      await using var transaction = await connection.BeginTransactionAsync();
      await using var command = new NpgsqlCommand(
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
            expected_stability_id,
            birthdate,
            formative_club
         )
         values (
            @person_id,
            @person_name,
            @person_type,
            'golf',
            @country_id,
            'NationalityOrSportingIdentity',
            'Dashboard grouping regression test',
            'tier_3',
            'short_term',
            date '1990-01-01',
            'Test club'
         );

         insert into activity_groups (
            id,
            title,
            sport_id,
            start_date,
            end_date
         )
         values (
            @activity_group_id,
            @group_title,
            'golf',
            @activity_date,
            @activity_date
         );

         insert into activities (
            id,
            title,
            description,
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
            slug,
            activity_group_id,
            published_at
         )
         values
         (
            @current_activity_id,
            @current_title,
            case when @include_description
               then 'Dashboard grouping test'
            end,
            'Match',
            'golf',
            @activity_date,
            time '10:00',
            @current_starts_at,
            time '12:00',
            @current_ends_at,
            'Europe/Stockholm',
            @published_status,
            'Channel One',
            @current_slug,
            @activity_group_id,
            now()
         ),
         (
            @variant_activity_id,
            @variant_title,
            case when @include_description
               then 'Dashboard grouping test'
            end,
            'Match',
            'golf',
            @activity_date,
            time '11:00',
            @variant_starts_at,
            time '13:00',
            @variant_ends_at,
            'Europe/Stockholm',
            @published_status,
            'Channel Two',
            @variant_slug,
            @activity_group_id,
            now()
         );

         insert into activity_entity_links (
            id,
            activity_id,
            entity_id
         )
         values
         (
            @current_link_id,
            @current_activity_id,
            @person_id
         ),
         (
            @variant_link_id,
            @variant_activity_id,
            @person_id
         );

         insert into ai_job_runs (
            id,
            job_id,
            prompt_id,
            provider_id,
            status_id,
            correlation_id,
            input_payload,
            rendered_prompt,
            started_at,
            completed_at
         )
         select
            @run_id,
            job.id,
            coalesce(job.active_prompt_id, prompt.id),
            job.provider_id,
            @completed_status,
            @variant_activity_id::text,
            '{}'::jsonb,
            'Dashboard grouping regression test',
            now(),
            now()
         from ai_jobs job
         left join ai_job_prompts prompt
            on prompt.job_id = job.id
               and prompt.enabled
         where job.id = @job_id
         order by prompt.version desc nulls last
         limit 1;

         insert into sources (
            id,
            correlation_type,
            correlation_id,
            kind,
            url
         )
         values (
            @source_id,
            @activity_correlation_type,
            @variant_activity_id::text,
            @source_kind,
            @source_url
         );

         insert into activity_participant_ai_results (
            id,
            activity_id,
            job_id,
            run_id,
            entity_id,
            field_key,
            value_text,
            value_json,
            source_id,
            sort_order
         )
         values (
            @result_id,
            @variant_activity_id,
            @job_id,
            @run_id,
            @person_id,
            @field_key,
            @variant_start_time,
            to_jsonb(@variant_start_time::text),
            @source_id,
            0
         );
         """,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("person_id", personId);
      command.Parameters.AddWithValue(
         "person_name",
         $"Dashboard test person {personId:N}"
      );
      command.Parameters.AddWithValue(
         "person_type",
         TrackedEntityTypeIds.Person
      );
      command.Parameters.AddWithValue("country_id", PrimaryCountry.Id);
      command.Parameters.AddWithValue(
         "activity_group_id",
         activityGroupId
      );
      command.Parameters.AddWithValue(
         "group_title",
         $"Dashboard test group {activityGroupId:N}"
      );
      command.Parameters.AddWithValue("activity_date", activityDate);
      command.Parameters.AddWithValue(
         "current_activity_id",
         currentActivityId
      );
      command.Parameters.AddWithValue(
         "current_title",
         $"Dashboard grouped activity one {currentActivityId:N}"
      );
      command.Parameters.AddWithValue("current_starts_at", currentStartsAt);
      command.Parameters.AddWithValue("current_ends_at", currentEndsAt);
      command.Parameters.AddWithValue(
         "current_slug",
         $"dashboard-grouped-one-{currentActivityId:N}"
      );
      command.Parameters.AddWithValue(
         "variant_activity_id",
         variantActivityId
      );
      command.Parameters.AddWithValue(
         "variant_title",
         $"Dashboard grouped activity two {variantActivityId:N}"
      );
      command.Parameters.AddWithValue("variant_starts_at", variantStartsAt);
      command.Parameters.AddWithValue("variant_ends_at", variantEndsAt);
      command.Parameters.AddWithValue(
         "variant_slug",
         $"dashboard-grouped-two-{variantActivityId:N}"
      );
      command.Parameters.AddWithValue(
         "published_status",
         ActivityPublicationStatusIds.Published
      );
      command.Parameters.AddWithValue(
         "current_link_id",
         Guid.NewGuid()
      );
      command.Parameters.AddWithValue(
         "variant_link_id",
         Guid.NewGuid()
      );
      command.Parameters.AddWithValue("run_id", runId);
      command.Parameters.AddWithValue(
         "job_id",
         AiJobIds.FindParticipantsStart
      );
      command.Parameters.AddWithValue(
         "completed_status",
         AiJobRunStatusIds.Completed
      );
      command.Parameters.AddWithValue(
         "source_id",
         Guid.NewGuid()
      );
      command.Parameters.AddWithValue(
         "activity_correlation_type",
         SourceCorrelationTypes.Activity
      );
      command.Parameters.AddWithValue(
         "source_kind",
         SourceKinds.ParticipantStartEvidence
      );
      command.Parameters.AddWithValue(
         "source_url",
         $"https://example.test/dashboard-{variantActivityId:N}"
      );
      command.Parameters.AddWithValue("result_id", Guid.NewGuid());
      command.Parameters.AddWithValue(
         "field_key",
         ActivityParticipantAiFieldKeys.StartTime
      );
      command.Parameters.AddWithValue(
         "variant_start_time",
         variantStartTime
      );
      command.Parameters.AddWithValue(
         "include_description",
         includeDescription
      );

      await command.ExecuteNonQueryAsync();
      await transaction.CommitAsync();
   }

   private static async Task DeleteTestDataAsync(
      NpgsqlDataSource dataSource,
      Guid activityGroupId,
      Guid currentActivityId,
      Guid variantActivityId,
      Guid personId,
      Guid runId
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync();
      await using var transaction = await connection.BeginTransactionAsync();
      await using var command = new NpgsqlCommand(
         """
         delete from activity_participant_ai_results
         where activity_id in (@current_activity_id, @variant_activity_id);

         delete from sources
         where correlation_type = @activity_correlation_type
            and correlation_id in (
               @current_activity_id::text,
               @variant_activity_id::text
            );

         delete from activity_entity_links
         where activity_id in (@current_activity_id, @variant_activity_id);

         delete from activities
         where id in (@current_activity_id, @variant_activity_id);

         delete from ai_job_runs
         where id = @run_id;

         delete from activity_groups
         where id = @activity_group_id;

         delete from entities
         where id = @person_id;
         """,
         connection,
         transaction
      );
      command.Parameters.AddWithValue(
         "current_activity_id",
         currentActivityId
      );
      command.Parameters.AddWithValue(
         "variant_activity_id",
         variantActivityId
      );
      command.Parameters.AddWithValue(
         "activity_correlation_type",
         SourceCorrelationTypes.Activity
      );
      command.Parameters.AddWithValue("run_id", runId);
      command.Parameters.AddWithValue("activity_group_id", activityGroupId);
      command.Parameters.AddWithValue("person_id", personId);

      await command.ExecuteNonQueryAsync();
      await transaction.CommitAsync();
   }
}
