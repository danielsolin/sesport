namespace SESport.Core.Tests.Database;

public class PostgresMigrationTests
{
   [Fact]
   public void BaselineMigrationDefinesCurrentSchema()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "001_baseline.sql"
         )
      );

      Assert.Contains("create table sports", migration);
      Assert.Contains("create table entities", migration);
      Assert.Contains("create table activities", migration);
      Assert.Contains("create table activity_proposals", migration);
      Assert.Contains("create table tv_sport_broadcasts", migration);
      Assert.Contains("create table ai_job_runs", migration);
      Assert.Contains("create table ai_activity_search_runs", migration);
      Assert.Contains(
         "create unique index entity_to_entity_links_entity_pair_unique",
         migration
      );
      Assert.Contains("openrouter:web_search", migration);
      Assert.DoesNotContain("tracked_entities", migration);
      Assert.DoesNotContain("alter table", migration);
      Assert.DoesNotContain("drop column", migration);
      Assert.DoesNotContain("rename to", migration);
      Assert.DoesNotContain("delete from", migration);
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
