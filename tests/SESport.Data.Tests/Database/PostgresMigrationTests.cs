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
