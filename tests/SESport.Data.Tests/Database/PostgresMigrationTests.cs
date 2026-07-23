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
