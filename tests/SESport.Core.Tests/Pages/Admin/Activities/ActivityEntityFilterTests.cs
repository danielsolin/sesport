using SESport.Data;
using SESport.Web.Pages.Admin.Activities;

namespace SESport.Core.Tests.Pages.Admin.Activities;

public sealed class ActivityEntityFilterTests
{
   [Fact]
   public void FilterPersonEntitiesOnlyReturnsPersonRows()
   {
      var entities = new[]
      {
         new EntityOption(
            Guid.NewGuid(),
            "Alice",
            "Person",
            "Tennis",
            "Team A"
         ),
         new EntityOption(
            Guid.NewGuid(),
            "Club",
            "Organization",
            "Tennis",
            ""
         ),
         new EntityOption(
            Guid.NewGuid(),
            "Bob",
            "Person",
            "Hockey",
            ""
         )
      };

      var filtered = ActivityEntityFilter.FilterPersonEntities(entities);

      Assert.Equal(2, filtered.Count);
      Assert.All(filtered, entity => Assert.Equal("Person", entity.Type));
      Assert.Equal("Alice", filtered[0].Name);
      Assert.Equal("Bob", filtered[1].Name);
   }
}
