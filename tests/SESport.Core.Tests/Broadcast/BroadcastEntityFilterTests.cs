using SESport.Core.Broadcast;

namespace SESport.Core.Tests.Broadcast;

public sealed class BroadcastEntityFilterTests
{
   [Fact]
   public void MatchPersonEntityIdsIgnoresNameCasingAndDoubleLetters()
   {
      var entityId = Guid.NewGuid();
      var entities = new[]
      {
         new BroadcastEntityOption(
            entityId,
            "Jonas Andersson",
            TrackedEntityTypeIds.Person,
            "motorsport",
            ""
         )
      };

      var matched = BroadcastEntityFilter.MatchPersonEntityIds(
         entities,
         ["Jonas Anderson", "JONAS ANDERSSON", "jonas andersson"]
      );

      Assert.Equal([entityId], matched);
   }

   [Fact]
   public void MatchPersonEntityIdsMatchesTransposedLetters()
   {
      var entityId = Guid.NewGuid();
      var entities = new[]
      {
         new BroadcastEntityOption(
            entityId,
            "Johan Grönvall",
            TrackedEntityTypeIds.Person,
            "motorsport",
            ""
         )
      };

      var matched = BroadcastEntityFilter.MatchPersonEntityIds(
         entities,
         ["Johan Görnvall"]
      );

      Assert.Equal([entityId], matched);
   }
}
