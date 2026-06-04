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
      Assert.DoesNotContain("activity_proposal_groups", migration);
      Assert.DoesNotContain("group_id", migration);
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

   [Fact]
   public void ThirdMigrationAddsActivityProposalRejectReasonModel()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "003_add_activity_proposal_reject_reason.sql"
         )
      );

      Assert.Contains(
         "create table if not exists proposal_reject_reasons",
         migration
      );
      Assert.Contains("reject_reason_id", migration);
      Assert.Contains("reject_comment", migration);
      Assert.Contains("Hallucination", migration);
      Assert.Contains("Duplicate", migration);
      Assert.Contains("OutOfScope", migration);
   }

   [Fact]
   public void FourthMigrationAddsActivityProposalProducer()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "004_add_activity_proposal_producer.sql"
         )
      );

      Assert.Contains("alter table activity_proposals", migration);
      Assert.Contains("producer text", migration);
   }

   [Fact]
   public void FifthMigrationAddsAiActivitySearchRunLog()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "005_add_ai_activity_search_runs.sql"
         )
      );

      Assert.Contains(
         "create table if not exists ai_activity_search_runs",
         migration
      );
      Assert.Contains(
         "create table if not exists ai_activity_search_run_items",
         migration
      );
      Assert.Contains("requested_model", migration);
      Assert.Contains("persisted_proposal_count", migration);
      Assert.Contains(
         "entity_id uuid null references tracked_entities",
         migration
      );
   }

   [Fact]
   public void SixthMigrationAddsActivityProposalPrompt()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "006_add_activity_proposal_prompt.sql"
         )
      );

      Assert.Contains("alter table activity_proposals", migration);
      Assert.Contains("prompt text", migration);
   }

   [Fact]
   public void SeventhMigrationAddsTvSportBroadcasts()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "007_add_tv_sport_broadcasts.sql"
         )
      );

      Assert.Contains(
         "create table if not exists tv_sport_import_runs",
         migration
      );
      Assert.Contains(
         "create table if not exists tv_sport_broadcasts",
         migration
      );
      Assert.Contains("categories text[] not null", migration);
      Assert.Contains("description text null", migration);
      Assert.Contains("is_replay boolean not null", migration);
      Assert.Contains("original_air_date date null", migration);
      Assert.Contains("raw_programme_xml text null", migration);
      Assert.Contains("unique (fingerprint)", migration);
   }

   [Fact]
   public void EighthMigrationAddsTvSportBroadcastReplayFields()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "008_add_tv_sport_broadcast_replay_fields.sql"
         )
      );

      Assert.Contains("alter table tv_sport_broadcasts", migration);
      Assert.Contains(
         "add column if not exists is_replay boolean not null default false",
         migration
      );
      Assert.Contains(
         "add column if not exists original_air_date date null",
         migration
      );
   }

   [Fact]
   public void NinthMigrationAddsTvSportBroadcastHiddenAt()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "009_add_tv_sport_broadcast_hidden_at.sql"
         )
      );

      Assert.Contains("alter table tv_sport_broadcasts", migration);
      Assert.Contains(
         "add column if not exists hidden_at timestamptz null",
         migration
      );
      Assert.Contains("where hidden_at is null", migration);
   }

   [Fact]
   public void TenthMigrationAddsEntityToEntityLinks()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "010_add_entity_to_entity_links.sql"
         )
      );

      Assert.Contains(
         "create table if not exists entity_to_entity_links",
         migration
      );
      Assert.Contains(
         "source_entity_id uuid not null references tracked_entities(id)",
         migration
      );
      Assert.Contains(
         "target_entity_id uuid not null references tracked_entities(id)",
         migration
      );
      Assert.Contains(
         "check (source_entity_id <> target_entity_id)",
         migration
      );
      Assert.Contains("unique (source_entity_id, target_entity_id)", migration);
   }

   [Fact]
   public void EleventhMigrationMakesEntityLinksUndirected()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "011_make_entity_links_undirected.sql"
         )
      );

      Assert.Contains(
         "delete from entity_to_entity_links duplicate",
         migration
      );
      Assert.Contains(
         "create unique index if not exists " +
            "entity_to_entity_links_entity_pair_unique",
         migration
      );
      Assert.Contains("least(source_entity_id, target_entity_id)", migration);
      Assert.Contains(
         "greatest(source_entity_id, target_entity_id)",
         migration
      );
   }

   [Fact]
   public void TwelfthMigrationAddsTvSportIgnoreRules()
   {
      var migration = File.ReadAllText(
         Path.Combine(
            FindRepositoryRoot(),
            "database",
            "migrations",
            "012_add_tv_sport_ignore.sql"
         )
      );

      Assert.Contains("create table if not exists tv_sport_ignore", migration);
      Assert.Contains("kind text not null", migration);
      Assert.Contains("value text not null", migration);
      Assert.Contains("source_key text null", migration);
      Assert.Contains("is_active boolean not null default true", migration);
      Assert.Contains("unique nulls not distinct", migration);
      Assert.Contains("'channel_name'", migration);
      Assert.Contains("'SE - Horse & Country TV'", migration);
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
