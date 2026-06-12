using SESport.Core.Broadcast;

namespace SESport.Core.Tests.Pages.Admin.Activities;

public sealed class ActivityEntityFilterTests
{
   [Fact]
   public void FilterSelectableEntitiesReturnsPersonAndNationalTeamRows()
   {
      var entities = new[]
      {
         new BroadcastEntityOption(
            Guid.NewGuid(),
            "Alice",
            TrackedEntityTypeIds.Person,
            "Tennis",
            "Team A"
         ),
         new BroadcastEntityOption(
            Guid.NewGuid(),
            "Sweden",
            TrackedEntityTypeIds.NationalTeam,
            "Tennis",
            ""
         ),
         new BroadcastEntityOption(
            Guid.NewGuid(),
            "Club",
            "Organization",
            "Tennis",
            ""
         ),
         new BroadcastEntityOption(
            Guid.NewGuid(),
            "Bob",
            TrackedEntityTypeIds.Person,
            "Hockey",
            ""
         )
      };

      var filtered = BroadcastEntityFilter.FilterSelectableEntities(entities);

      Assert.Equal(3, filtered.Count);
      Assert.All(
         filtered,
         entity => Assert.True(
            entity.Type == TrackedEntityTypeIds.Person ||
               entity.Type == TrackedEntityTypeIds.NationalTeam
         )
      );
      Assert.Equal("Alice", filtered[0].Name);
      Assert.Equal("Sweden", filtered[1].Name);
      Assert.Equal("Bob", filtered[2].Name);
   }

   [Fact]
   public void MatchPersonEntityIdsTreatsAccentsAsEquivalent()
   {
      var linneaId = Guid.NewGuid();
      var entities = new[]
      {
         new BroadcastEntityOption(
            linneaId,
            "Linnea Ström",
            TrackedEntityTypeIds.Person,
            "Hockey",
            ""
         )
      };

      var matched = BroadcastEntityFilter.MatchPersonEntityIds(
         entities,
         ["Linnea Strom"]
      );

      Assert.Equal([linneaId], matched);
   }
}
