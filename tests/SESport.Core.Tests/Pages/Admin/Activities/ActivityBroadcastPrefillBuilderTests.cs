using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Data;
using SESport.Web.Pages.Admin.Activities;

namespace SESport.Core.Tests.Pages.Admin.Activities;

public sealed class ActivityBroadcastPrefillBuilderTests
{
   [Fact]
   public void NormalizeBroadcastIdsKeepsOnlyTheFirstRealId()
   {
      var first = Guid.NewGuid();
      var second = Guid.NewGuid();

      var normalized = ActivityBroadcastPrefillBuilder.NormalizeBroadcastIds(
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
            new EntityOption(
               aliceId,
               " Alice ",
               TrackedEntityTypeIds.Person,
               "Tennis",
               ""
            ),
            new EntityOption(
               Guid.NewGuid(),
               "Alice",
               "Organization",
               "Tennis",
               ""
            ),
            new EntityOption(
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

      var matched = ActivityBroadcastPrefillBuilder.SelectLinkedEntityIds(
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

      var comment = ActivityBroadcastPrefillBuilder.CreateEvidenceComment(
         broadcast,
         participationCheck
      );

      Assert.Contains("2026-06-01 20:00", comment);
      Assert.Contains("SVT", comment);
      Assert.Contains("AI participation: Yes", comment);
      Assert.Contains("AI participants: Alice, Bob", comment);
      Assert.Contains("- https://example.test/a", comment);
   }
}
