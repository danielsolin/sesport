namespace SESport.Core.Tests.Database;

public class PostgresMigrationTests
{
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
      );

      Assert.Contains("create table activity_groups", baseline);
      Assert.Contains("create table broadcasts", baseline);
      Assert.Contains("create table broadcast_import_runs", baseline);
      Assert.Contains("create table broadcast_ignore", baseline);
      Assert.Contains("create table entities", baseline);
      Assert.Contains("create table activities", baseline);
      Assert.Contains("create table ai_jobs", baseline);
      Assert.Contains("create table ai_job_runs", baseline);
      Assert.Contains("entities_person_gender_id_valid", baseline);
      Assert.Contains("requires_web_search boolean not null default true",
         baseline);
      Assert.Contains("active_prompt_id uuid null", baseline);
      Assert.Contains("tool_trace jsonb null", baseline);
      Assert.Contains("activity_group_source_kind_id text null", baseline);
      Assert.Contains("activity_group_source_activity_id", baseline);
      Assert.Contains("activity_group_draft_title text null", baseline);
      Assert.Contains("activity_group_id uuid null references activity_groups",
         baseline);
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
      Assert.DoesNotContain("tv_sport_", baseline);
      Assert.DoesNotContain("tracked_entities", baseline);
      Assert.DoesNotContain("rename to", baseline);
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
