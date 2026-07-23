using SESport.Core.Broadcast;

namespace SESport.Core.Tests.Pages.Admin.Activities;

public sealed class ActivityEntityFilterTests
{
   [Fact]
   public void FilterSelectableEntitiesReturnsOnlyPersonRows()
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
            PrimaryCountry.CountryName,
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

      Assert.Equal(2, filtered.Count);
      Assert.All(
         filtered,
         entity => Assert.Equal(TrackedEntityTypeIds.Person, entity.Type)
      );
      Assert.Equal("Alice", filtered[0].Name);
      Assert.Equal("Bob", filtered[1].Name);
   }

   [Theory]
   [InlineData(TrackedEntityTypeIds.Organization, true)]
   [InlineData(TrackedEntityTypeIds.NationalTeam, true)]
   [InlineData(TrackedEntityTypeIds.Series, true)]
   [InlineData(TrackedEntityTypeIds.Person, false)]
   [InlineData(TrackedEntityTypeIds.Pair, false)]
   public void IsOrganizationEntityTypeAllowsBroadcastOrganizationTypes(
      string entityTypeId,
      bool expected
   )
   {
      Assert.Equal(
         expected,
         BroadcastEntityFilter.IsOrganizationEntityType(entityTypeId)
      );
   }

   [Fact]
   public void GetNonOrganizationEntityTypeSqlListsPersonAndPair()
   {
      var sql = BroadcastEntityFilter.GetNonOrganizationEntityTypeSql();

      Assert.Equal("'Person', 'Pair'", sql);
   }

   [Fact]
   public void GetNonOrganizationEntityTypePredicateSqlUsesProvidedColumn()
   {
      var sql = BroadcastEntityFilter.GetNonOrganizationEntityTypePredicateSql(
         "e.entity_type_id"
      );

      Assert.Equal(
         "e.entity_type_id not in ('Person', 'Pair')",
         sql
      );
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

   [Fact]
   public void MatchPersonEntityIdsMatchesCommaSeparatedParticipantName()
   {
      var ludvigId = Guid.NewGuid();
      var entities = new[]
      {
         new BroadcastEntityOption(
            ludvigId,
            "Ludvig Åberg",
            TrackedEntityTypeIds.Person,
            "Golf",
            ""
         )
      };

      var matched = BroadcastEntityFilter.MatchPersonEntityIds(
         entities,
         ["Åberg, Ludvig"]
      );

      Assert.Equal([ludvigId], matched);
   }

   [Fact]
   public void MatchPersonEntityIdsMatchesAliasName()
   {
      var entityId = Guid.NewGuid();
      var entities = new[]
      {
         new BroadcastEntityOption(
            entityId,
            "Daniela Holmqvist",
            TrackedEntityTypeIds.Person,
            "Golf",
            "",
            "Dani Holmqvist"
         )
      };

      var matched = BroadcastEntityFilter.MatchPersonEntityIds(
         entities,
         ["Dani Holmqvist"]
      );

      Assert.Equal([entityId], matched);
   }
}
