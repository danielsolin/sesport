using SESport.Data;
using SESport.Web.Services;

namespace SESport.Core.Tests.Services;

public sealed class AdminNavigationBuilderTests
{
   [Fact]
   public void BuildConfigNavigationGroupsPlacesActivityProposalsInLegacy()
   {
      var referenceTables =
         new[]
         {
            new ReferenceTableInfo(
               "sports",
               "Sports",
               "Sports available when creating activities and entities.",
               ReferenceTableKind.Sports
            ),
            new ReferenceTableInfo(
               "activity-types",
               "Activity types",
               "Small controlled vocabulary for activity classification."
            )
         };

      var groups = AdminNavigationBuilder.BuildConfigNavigationGroups(
         referenceTables
      );
      var legacyGroup = Assert.Single(
         groups,
         group => string.Equals(
            group.Title,
            "Legacy",
            StringComparison.OrdinalIgnoreCase
         )
      );

      var legacyItem = Assert.Single(legacyGroup.Items);
      Assert.Equal("Activity Proposals", legacyItem.Title);
      Assert.Equal("/Admin/Activities/Proposals", legacyItem.Href);
      Assert.Equal("Legacy", groups[^1].Title);

      var referenceGroup = Assert.Single(
         groups,
         group => string.Equals(
            group.Title,
            "Reference tables",
            StringComparison.OrdinalIgnoreCase
         )
      );

      Assert.Equal(
         [
            "Activity types",
            "Broadcast Ignore Rules",
            "Sports"
         ],
         referenceGroup.Items.Select(item => item.Title)
      );
   }
}
