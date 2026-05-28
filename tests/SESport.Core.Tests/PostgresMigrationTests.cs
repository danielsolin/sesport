namespace SESport.Core.Tests;

public class PostgresMigrationTests
{
   [Fact]
   public void FirstMigrationCreatesAndSeedsCompetitions()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "postgres",
            "migrations",
            "001_create_competitions.sql"
         )
      );

      Assert.Contains("create table if not exists competitions", migration);
      Assert.Contains("status text not null", migration);
      Assert.Contains("'Ongoing'", migration);
      Assert.Contains(
         "'competition:iihf-world-championship-2026'",
         migration
      );
   }

   private static string FindRepositoryRoot()
   {
      var directory = new DirectoryInfo(AppContext.BaseDirectory);

      while (directory is not null)
      {
         if (File.Exists(Path.Combine(directory.FullName, "SESport.sln")))
         {
            return directory.FullName;
         }

         directory = directory.Parent;
      }

      throw new InvalidOperationException("Could not find repository root.");
   }
}
