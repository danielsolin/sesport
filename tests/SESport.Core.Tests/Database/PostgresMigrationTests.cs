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
      var renameMigration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "002_rename_tv_sport_to_broadcasts.sql"
         )
      );

      Assert.Contains("create table sports", baseline);
      Assert.Contains("create table entities", baseline);
      Assert.Contains("create table activities", baseline);
      Assert.Contains("create table activity_proposals", baseline);
      Assert.Contains("create table tv_sport_broadcasts", baseline);
      Assert.Contains("create table ai_job_runs", baseline);
      Assert.Contains("create table ai_activity_search_runs", baseline);
      Assert.Contains("alter table tv_sport_broadcasts", renameMigration);
      Assert.Contains("rename to broadcasts", renameMigration);
      Assert.Contains(
         "create unique index entity_to_entity_links_entity_pair_unique",
         baseline
      );
      Assert.Contains("openrouter:web_search", baseline);
      Assert.DoesNotContain("tracked_entities", baseline);
      Assert.DoesNotContain("alter table", baseline);
      Assert.DoesNotContain("drop column", baseline);
      Assert.DoesNotContain("rename to", baseline);
      Assert.DoesNotContain("delete from", baseline);
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
