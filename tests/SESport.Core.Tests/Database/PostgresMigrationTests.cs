namespace SESport.Core.Tests.Database;

public class PostgresMigrationTests
{
   [Fact]
   public void FirstMigrationCreatesEntityFirstActivityProposalModel()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "001_create_entity_activity_model.sql"
         )
      );

      Assert.Contains("create table if not exists tracked_entities", migration);
      Assert.Contains("create table if not exists sports", migration);
      Assert.Contains("create table if not exists activity_types", migration);
      Assert.Contains(
         "create table if not exists activity_entity_link_roles",
         migration
      );
      Assert.Contains(
         "create table if not exists entity_watch_priorities",
         migration
      );
      Assert.Contains(
         "create table if not exists entity_stability_kinds",
         migration
      );
      Assert.Contains("create table if not exists sources", migration);
      Assert.Contains(
         "create table if not exists activity_proposals",
         migration
      );
      Assert.Contains(
         "create table if not exists activity_proposal_entity_links",
         migration
      );
      Assert.Contains(
         "create table if not exists activity_proposal_evidence",
         migration
      );
      Assert.Contains("create table if not exists activities", migration);
      Assert.Contains(
         "create table if not exists activity_entity_links",
         migration
      );
      Assert.Contains(
         "create table if not exists activity_evidence",
         migration
      );
      Assert.Contains(
         "create table if not exists activity_proposal_groups",
         migration
      );
      Assert.Contains("'Pending'", migration);
      Assert.Contains(
         "status_id = 'Approved' and activity_id is not null",
         migration
      );
      Assert.Contains("activity_date date not null", migration);
      Assert.DoesNotContain("country_relevance_explanation", migration);
      Assert.DoesNotContain("entity_evidence", migration);
      Assert.DoesNotContain("entity_relationships", migration);
      Assert.DoesNotContain("time_description", migration);
      Assert.DoesNotContain("DateRange", migration);
      Assert.DoesNotContain("ToBeDetermined", migration);
   }

   [Fact]
   public void SecondMigrationAddsActivityPublicationModel()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "002_add_activity_publication.sql"
         )
      );

      Assert.Contains(
         "create table if not exists activity_publication_statuses",
         migration
      );
      Assert.Contains("publication_status_id", migration);
      Assert.Contains("Published", migration);
      Assert.Contains("slug", migration);
      Assert.Contains("published_at", migration);
      Assert.Contains("activities_slug_unique", migration);
      Assert.Contains("activity_date", migration);
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
