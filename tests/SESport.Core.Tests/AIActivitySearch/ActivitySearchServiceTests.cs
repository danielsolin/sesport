using SESport.AI.ActivitySearch;
using SESport.Core.Ingestion;

namespace SESport.Core.Tests.AIActivitySearch;

public class ActivitySearchServiceTests
{
   [Fact]
   public async Task SearchMapsDraftsToAiActivityProposals()
   {
      var client = new StubActivitySearchModelClient(
         new ActivitySearchModelResult(
            "{\"proposals\":[]}",
            "{\"id\":\"response:test\"}",
            [
               new ActivityProposalDraft(
                  "Tre Kronor vs Finland",
                  "A scheduled international ice hockey match.",
                  "Match",
                  new DateOnly(2026, 6, 1),
                  new TimeOnly(19, 0),
                  "Europe/Stockholm",
                  "International friendly",
                  "CompetesIn",
                  "Tre Kronor is one of the participating teams.",
                  0.88m,
                  [
                     new ActivityProposalEvidenceDraft(
                        "Swehockey",
                        new Uri("https://example.test/game"),
                        "Schedule",
                        "The source lists the match.",
                        "Tre Kronor vs Finland"
                     )
                  ]
               )
            ]
         )
         {
            Producer = "openrouter/openai/gpt-oss-20b",
            Prompt = "Find planned activities for Tre Kronor."
         }
      );
      var service = new ActivitySearchService(client, "openrouter/free");
      var result = await service.SearchAsync(
         new ActivitySearchRequest(CreateEntity(), new DateOnly(2026, 5, 31)),
         CancellationToken.None
      );

      var proposal = Assert.Single(result.Proposals);

      Assert.Equal(
         ActivityProposalProducerType.AiSearch,
         proposal.ProducerType
      );
      Assert.Equal("openrouter/openai/gpt-oss-20b", proposal.Producer);
      Assert.Equal("Find planned activities for Tre Kronor.", proposal.Prompt);
      Assert.Equal(ActivityType.Match, proposal.Type);
      Assert.Equal("Tre Kronor vs Finland", proposal.Title);
      Assert.Equal(ActivityTimeKind.Scheduled, proposal.Time.Kind);
      Assert.Equal(new TimeOnly(19, 0), proposal.Time.LocalStartTime);
      Assert.Null(proposal.Time.StartsAt);
      Assert.Equal("Europe/Stockholm", proposal.Time.TimeZoneId);
      Assert.Single(proposal.EntityLinks);
      Assert.Single(proposal.Evidence);
      Assert.Equal(0.88m, proposal.Confidence);
   }

   [Fact]
   public async Task SearchFiltersDraftsOutsideDateWindow()
   {
      var client = new StubActivitySearchModelClient(
         new ActivitySearchModelResult(
            "{\"proposals\":[]}",
            "{\"id\":\"response:test\"}",
            [
               CreateDraft("Old match", new DateOnly(2026, 5, 30)),
               CreateDraft("Current match", new DateOnly(2026, 5, 31)),
               CreateDraft("Future match", new DateOnly(2026, 6, 14)),
               CreateDraft("Too far ahead", new DateOnly(2026, 7, 1))
            ]
         )
      );
      var service = new ActivitySearchService(client);
      var result = await service.SearchAsync(
         new ActivitySearchRequest(
            CreateEntity(),
            new DateOnly(2026, 5, 31),
            LookAheadDays: 30
         ),
         CancellationToken.None
      );

      Assert.Collection(
         result.Proposals,
         proposal => Assert.Equal("Current match", proposal.Title),
         proposal => Assert.Equal("Future match", proposal.Title)
      );
   }

   [Fact]
   public async Task SearchLimitsDraftsAfterDateFiltering()
   {
      var client = new StubActivitySearchModelClient(
         new ActivitySearchModelResult(
            "{\"proposals\":[]}",
            "{\"id\":\"response:test\"}",
            [
               CreateDraft("Old match", new DateOnly(2026, 5, 30)),
               CreateDraft("First match", new DateOnly(2026, 5, 31)),
               CreateDraft("Second match", new DateOnly(2026, 6, 1))
            ]
         )
      );
      var service = new ActivitySearchService(client);
      var result = await service.SearchAsync(
         new ActivitySearchRequest(
            CreateEntity(),
            new DateOnly(2026, 5, 31),
            MaxProposals: 1
         ),
         CancellationToken.None
      );

      var proposal = Assert.Single(result.Proposals);

      Assert.Equal("First match", proposal.Title);
   }

   private static ActivityProposalDraft CreateDraft(
      string title,
      DateOnly activityDate
   )
   {
      return new ActivityProposalDraft(
         title,
         "A scheduled international ice hockey match.",
         "Match",
         activityDate,
         null,
         "Europe/Stockholm",
         "International friendly",
         "CompetesIn",
         "Tre Kronor is one of the participating teams.",
         0.88m,
         []
      );
   }

   private static ActivitySearchEntity CreateEntity()
   {
      return new ActivitySearchEntity(
         new ExternalEntityId("tre-kronor"),
         "Tre Kronor",
         "national_team",
         new ImportedSport(new ExternalEntityId("ice-hockey"), "ice hockey"),
         "Represents Sweden",
         "Current Swedish men's national ice hockey team.",
         ["championships", "roster announcements"],
         "Swehockey",
         "Strong long-term watchlist anchor"
      );
   }

   private sealed class StubActivitySearchModelClient(
      ActivitySearchModelResult result
   ) : IActivitySearchModelClient
   {
      public Task<ActivitySearchModelResult> SearchAsync(
         ActivitySearchRequest request,
         CancellationToken cancellationToken
      )
      {
         return Task.FromResult(result);
      }
   }
}
