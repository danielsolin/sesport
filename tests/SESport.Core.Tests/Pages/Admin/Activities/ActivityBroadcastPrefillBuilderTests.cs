using SESport.Core.Broadcast;

namespace SESport.Core.Tests.Pages.Admin.Activities;

public sealed class ActivityBroadcastPrefillBuilderTests
{
   [Fact]
   public void NormalizeBroadcastIdsKeepsOnlyTheFirstRealId()
   {
      var first = Guid.NewGuid();
      var second = Guid.NewGuid();

      var normalized = BroadcastActivityPrefillBuilder.NormalizeBroadcastIds(
         [Guid.Empty, first, second, first]
      );

      Assert.Equal([first], normalized);
   }

   [Fact]
   public void SelectLinkedEntityIdsMatchesParticipantNamesStrictly()
   {
      var aliceId = Guid.NewGuid();
      var bobId = Guid.NewGuid();
      var entities =
         new[]
         {
         new BroadcastEntityOption(
            aliceId,
            " Alice ",
            TrackedEntityTypeIds.Person,
            "Tennis",
            ""
         ),
         new BroadcastEntityOption(
            Guid.NewGuid(),
            "Alice",
            "Organization",
            "Tennis",
            ""
         ),
         new BroadcastEntityOption(
            bobId,
            "Bob",
            TrackedEntityTypeIds.Person,
            "Hockey",
               ""
            )
         };
      var participationCheck = new BroadcastParticipationCheck(
         Guid.NewGuid(),
         "completed",
         "Yes",
         ["alice", " BOB ", "Unknown", "alice"],
         [],
         null
      );

      var matched = BroadcastActivityPrefillBuilder.SelectLinkedEntityIds(
         entities,
         participationCheck
      );

      Assert.Equal([aliceId, bobId], matched);
   }

   [Fact]
   public void CreateEvidenceCommentIncludesAiContext()
   {
      var broadcast = new BroadcastActivitySource(
         Guid.NewGuid(),
         "SVT",
         "Sweden vs Finland",
         "World Cup qualifier",
         ["football"],
         new DateTimeOffset(2026, 6, 1, 18, 0, 0, TimeSpan.Zero),
         new DateTimeOffset(2026, 6, 1, 20, 0, 0, TimeSpan.Zero)
      );
      var participationCheck = new BroadcastParticipationCheck(
         Guid.NewGuid(),
         "completed",
         "Yes",
         ["Alice", "Bob"],
         ["https://example.test/a"],
         null
      );

      var comment = BroadcastActivityPrefillBuilder.CreateEvidenceComment(
         broadcast,
         participationCheck
      );

      Assert.Contains("2026-06-01 20:00", comment);
      Assert.Contains("SVT", comment);
      Assert.Contains("AI participation: Yes", comment);
      Assert.Contains("AI participants: Alice, Bob", comment);
      Assert.Contains("- https://example.test/a", comment);
   }

   [Fact]
   public void CreateActivityTitleRemovesRedundantOrganizationName()
   {
      var broadcast = new BroadcastActivitySource(
         Guid.NewGuid(),
         "SVT",
         "GT World Challenge, GT World Challenge America",
         null,
         ["motorsport"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow
      );
      var participationCheck = new BroadcastParticipationCheck(
         Guid.NewGuid(),
         "completed",
         "Yes",
         ["Hampus Ericsson"],
         [],
         null
      );
      var entities =
         new[]
         {
         new BroadcastEntityOption(
            Guid.NewGuid(),
            "Hampus Ericsson",
            TrackedEntityTypeIds.Person,
            "Motorsport",
            "GT World Challenge"
         )
      };

      var title = BroadcastActivityPrefillBuilder.CreateActivityTitle(
         broadcast,
         entities,
         participationCheck
      );

      Assert.Equal("America", title);
   }

   [Fact]
   public void CreateActivityTitleTreatsTourenAsTour()
   {
      var broadcast = new BroadcastActivitySource(
         Guid.NewGuid(),
         "SVT",
         "LPGA Touren, Final",
         null,
         ["golf"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow
      );
      var participationCheck = new BroadcastParticipationCheck(
         Guid.NewGuid(),
         "completed",
         "Yes",
         ["Hampus Ericsson"],
         [],
         null
      );
      var entities =
         new[]
         {
         new BroadcastEntityOption(
            Guid.NewGuid(),
            "Hampus Ericsson",
            TrackedEntityTypeIds.Person,
            "Golf",
               "LPGA Tour"
            )
         };

      var title = BroadcastActivityPrefillBuilder.CreateActivityTitle(
         broadcast,
         entities,
         participationCheck
      );

      Assert.Equal("Final", title);
   }

   [Fact]
   public void CreateActivityTitleTreatsSerienAsSeries()
   {
      var broadcast = new BroadcastActivitySource(
         Guid.NewGuid(),
         "SVT",
         "IndyCar Serien, Race 1",
         null,
         ["motorsport"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow
      );
      var participationCheck = new BroadcastParticipationCheck(
         Guid.NewGuid(),
         "completed",
         "Yes",
         ["Hampus Ericsson"],
         [],
         null
      );
      var entities =
         new[]
         {
         new BroadcastEntityOption(
            Guid.NewGuid(),
            "Hampus Ericsson",
            TrackedEntityTypeIds.Person,
            "Motorsport",
               "IndyCar Series"
            )
         };

      var title = BroadcastActivityPrefillBuilder.CreateActivityTitle(
         broadcast,
         entities,
         participationCheck
      );

      Assert.Equal("Race 1", title);
   }

   [Fact]
   public void CreateActivityTitleNormalizesShoutedTitles()
   {
      var broadcast = new BroadcastActivitySource(
         Guid.NewGuid(),
         "SVT",
         "CANADIAN OPEN",
         null,
         ["golf"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow
      );

      var title = BroadcastActivityPrefillBuilder.CreateActivityTitle(
         broadcast,
         [],
         null
      );

      Assert.Equal("Canadian Open", title);
   }

   [Fact]
   public void CreateActivityTitleKeepsKnownAcronyms()
   {
      var broadcast = new BroadcastActivitySource(
         Guid.NewGuid(),
         "SVT",
         "GT WORLD CHALLENGE",
         null,
         ["motorsport"],
         DateTimeOffset.UtcNow,
         DateTimeOffset.UtcNow
      );

      var title = BroadcastActivityPrefillBuilder.CreateActivityTitle(
         broadcast,
         [],
         null
      );

      Assert.Equal("GT World Challenge", title);
   }
}
