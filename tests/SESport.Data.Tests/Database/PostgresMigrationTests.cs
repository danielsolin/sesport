namespace SESport.Core.Tests.Database;

public partial class PostgresMigrationTests
{
   [Fact]
   public void MigrationsDoNotModifyApplicationData()
   {
      var migrationDirectory = Path.Combine(
         FindRepositoryRoot(),
         "database",
         "migrations"
      );
      foreach(var migrationPath in Directory.GetFiles(
         migrationDirectory,
         "*.sql"
      ))
      {
         var migration = File.ReadAllText(migrationPath);

         Assert.False(
            DataStatementPattern().IsMatch(migration),
            $"Migration contains a data statement: {migrationPath}"
         );
      }
   }

   [System.Text.RegularExpressions.GeneratedRegex(
      @"^\s*(insert\s+into|update\s|delete\s+from|merge\s+into|" +
      @"copy\s|truncate\s|select\s+setval\s*\()\b",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase |
      System.Text.RegularExpressions.RegexOptions.Multiline
   )]
   private static partial System.Text.RegularExpressions.Regex
      DataStatementPattern();

   [Fact]
   public void BaselineMigrationDefinesCurrentSchema()
   {
      var baseline = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "001_baseline.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains("create table public.activity_groups", baseline);
      Assert.Contains("create table public.broadcasts", baseline);
      Assert.Contains("create table public.broadcast_import_runs", baseline);
      Assert.Contains("create table public.broadcast_ignore", baseline);
      Assert.Contains("create table public.entities", baseline);
      Assert.Contains("create table public.activities", baseline);
      Assert.Contains("create table public.ai_jobs", baseline);
      Assert.Contains("create table public.ai_job_runs", baseline);
      Assert.Contains("entities_person_gender_id_valid", baseline);
      Assert.Contains("requires_web_search boolean default true not null",
         baseline);
      Assert.Contains("active_prompt_id uuid", baseline);
      Assert.Contains("tool_trace jsonb", baseline);
      Assert.Contains("activity_group_source_kind_id text", baseline);
      Assert.Contains("activity_group_source_activity_id", baseline);
      Assert.Contains("activity_group_draft_title text", baseline);
      Assert.Contains("activity_group_id uuid", baseline);
      Assert.Contains("broadcasts_activity_group_source_kind_check", baseline);
      Assert.Contains("broadcast_ignore_kind_value_source_unique", baseline);
      Assert.Contains("create index activity_groups_sport_title_date_idx",
         baseline);
      Assert.Contains("create index activities_activity_group_id_idx",
         baseline);
      Assert.Contains(
         "create index activity_entity_links_organization_entity_id_idx",
         baseline
      );
      Assert.Contains("create index broadcasts_categories_gin_idx", baseline);
      Assert.Contains("create index ai_job_runs_started_at_desc_idx",
         baseline);
      Assert.Contains("create index ai_job_runs_exec_claim_idx", baseline);
      Assert.Contains("create index ai_job_runs_exec_env_idx", baseline);
      Assert.Contains("ai_jobs_active_prompt_id_fkey", baseline);
      Assert.Contains("local_end_time time without time zone", baseline);
      Assert.Contains("ends_at timestamp with time zone", baseline);
      Assert.Contains("activities_end_time_shape_check", baseline);
      Assert.Contains("ends_at > starts_at", baseline);
   }

   [Fact]
   public void FactsMigrationDefinesPolymorphicFactStorage()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "004_facts.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains("create table public.facts", migration);
      Assert.Contains("activity_id uuid", migration);
      Assert.Contains("entity_id uuid", migration);
      Assert.Contains("fact_text text not null", migration);
      Assert.Contains("facts_exactly_one_subject_check", migration);
      Assert.Contains("references public.activities(id)", migration);
      Assert.Contains("references public.entities(id)", migration);
      Assert.Contains("on delete cascade", migration);
   }

   [Fact]
   public void BroadcastImageMigrationAddsImageUrl()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "009_broadcast_image_url.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains("alter table public.broadcasts", migration);
      Assert.Contains("add column image_url text", migration);
   }

   [Fact]
   public void SportStartTimeMigrationAddsRequiresStartTime()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "015_sport_requires_start_time.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains("alter table public.sports", migration);
      Assert.Contains(
         "add column requires_start_time boolean default false not null",
         migration
      );
   }

   [Fact]
   public void SportTeamMigrationAddsIsTeamSport()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "016_sport_team_sport.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains("alter table public.sports", migration);
      Assert.Contains(
         "add column is_team_sport boolean default false not null",
         migration
      );
   }

   [Fact]
   public void ActivityGroupSeparateParticipantsMigrationAddsSetting()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "019_activity_group_separate_participants.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains("alter table public.activity_groups", migration);
      Assert.Contains(
         "add column separate_participants boolean default false not null",
         migration
      );
   }

   [Fact]
   public void ActivityGroupNoGroupingMigrationRenamesSetting()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "020_activity_group_no_grouping.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains("alter table public.activity_groups", migration);
      Assert.Contains(
         "rename column separate_participants to no_grouping",
         migration
      );
   }

   [Fact]
   public void MemberMigrationDefinesAccountsTokensAndWatches()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "017_members.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains("create table public.members", migration);
      Assert.Contains("email_normalized text not null", migration);
      Assert.Contains("create table public.member_login_tokens", migration);
      Assert.Contains("token_hash text not null", migration);
      Assert.Contains("member_login_tokens_expiry_check", migration);
      Assert.Contains("create table public.member_entity_watches", migration);
      Assert.Contains(
         "primary key (member_id, entity_id)",
         migration
      );
      Assert.Contains(
         "references public.entities(id)",
         migration
      );
      Assert.Contains("on delete cascade", migration);
   }

   [Fact]
   public void TodoMigrationDefinesOpenTodoStorage()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "018_todos.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains("create table public.todos", migration);
      Assert.Contains("target_type_id text not null", migration);
      Assert.Contains("correlation_id text", migration);
      Assert.Contains("done_at timestamp with time zone", migration);
      Assert.Contains("todos_target_type_id_check", migration);
      Assert.Contains("todos_text_not_blank_check", migration);
      Assert.Contains("todos_open_created_at_idx", migration);
   }

   [Fact]
   public void ParticipantAiResultsMigrationDefinesGenericResultStorage()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "010_activity_participant_ai_results.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains(
         "create table public.activity_participant_ai_result_sets",
         migration
      );
      Assert.Contains(
         "create table public.activity_participant_ai_result_values",
         migration
      );
      Assert.Contains("activity_id uuid not null", migration);
      Assert.Contains("job_id text not null", migration);
      Assert.Contains("run_id uuid not null", migration);
      Assert.Contains("checked_sources jsonb not null default '[]'::jsonb",
         migration);
      Assert.Contains("value_json jsonb not null", migration);
      Assert.Contains("primary key (activity_id, job_id)", migration);
      Assert.Contains(
         "primary key (activity_id, job_id, entity_id, field_key)",
         migration
      );
      Assert.Contains("references public.activities(id)", migration);
      Assert.Contains("references public.ai_jobs(id)", migration);
      Assert.Contains("references public.ai_job_runs(id)", migration);
      Assert.Contains("references public.entities(id)", migration);
   }

   [Fact]
   public void ParticipantAiResultsMigrationUsesSingleValueTable()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "014_activity_participant_ai_result_values.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains(
         "drop table public.activity_participant_ai_results",
         migration
      );
      Assert.Contains(
         "create table public.activity_participant_ai_results",
         migration
      );
      Assert.Contains(
         "source_id uuid not null",
         migration
      );
      Assert.Contains(
         "sort_order integer not null",
         migration
      );
      Assert.Contains(
         "activity_participant_ai_results_field_key_not_blank_check",
         migration
      );
      Assert.Contains(
         "activity_participant_ai_results_run_id_idx",
         migration
      );
      Assert.Contains(
         "activity_participant_ai_results_activity_job_idx",
         migration
      );
      Assert.Contains(
         "activity_participant_ai_results_entity_id_idx",
         migration
      );
      Assert.Contains(
         "activity_participant_ai_results_source_id_idx",
         migration
      );
      Assert.Contains(
         "activity_participant_ai_results_sort_order_idx",
         migration
      );
   }

   [Fact]
   public void FactSourceLinksMigrationConnectsFactsAndSources()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "005_fact_source_links.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains("create table public.fact_source_links", migration);
      Assert.Contains("references public.facts(id)", migration);
      Assert.Contains("references public.sources(id)", migration);
      Assert.Contains("primary key (fact_id, source_id)", migration);
      Assert.Contains("on delete cascade", migration);
   }

   [Fact]
   public void LegacyActivityFactsMigrationDropsColumn()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "006_drop_activities_facts.sql"
         )
      ).ToLowerInvariant();

      Assert.Contains("alter table public.activities", migration);
      Assert.Contains("drop column facts", migration);
   }

   private static string FindRepositoryRoot()
   {
      var directory = new DirectoryInfo(AppContext.BaseDirectory);

      while(directory is not null)
      {
         if(File.Exists(Path.Combine(directory.FullName, "SESport.sln")))
         {
            return directory.FullName;
         }

         directory = directory.Parent;
      }

      throw new InvalidOperationException("Could not find repository root.");
   }
}
