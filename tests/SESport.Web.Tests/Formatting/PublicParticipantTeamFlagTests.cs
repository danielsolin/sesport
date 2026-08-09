using SESport.Core.Configuration;
using SESport.Web.Formatting;

namespace SESport.Core.Tests.Formatting;

public sealed class PublicParticipantTeamFlagTests
{
   [Fact]
   public void GetPathReturnsFlagForForeignTeamMatch()
   {
      var path = PublicParticipantTeamFlag.GetPath(
         true,
         nameof(ActivityType.Match),
         "pl"
      );

      Assert.Equal("/images/flags/pl.svg", path);
   }

   [Theory]
   [InlineData("be")]
   [InlineData("de")]
   [InlineData("it")]
   [InlineData("no")]
   [InlineData("pt")]
   [InlineData("uk")]
   [InlineData("us")]
   public void GetPathReturnsPathForKnownTeamCountry(string countryId)
   {
      var path = PublicParticipantTeamFlag.GetPath(
         true,
         nameof(ActivityType.Match),
         countryId
      );

      Assert.Equal($"/images/flags/{countryId}.svg", path);
   }

   [Fact]
   public void GetPathHidesCountriesWithoutLocalFlagAsset()
   {
      var path = PublicParticipantTeamFlag.GetPath(
         true,
         nameof(ActivityType.Match),
         "int"
      );

      Assert.Null(path);
   }

   [Fact]
   public void GetPathHidesPrimaryCountryFlag()
   {
      var path = PublicParticipantTeamFlag.GetPath(
         true,
         nameof(ActivityType.Match),
         PrimaryCountry.Id
      );

      Assert.Null(path);
   }

   [Theory]
   [InlineData(false, nameof(ActivityType.Match))]
   [InlineData(true, nameof(ActivityType.Tournament))]
   public void GetPathHidesFlagOutsideTeamMatches(
      bool isTeamSport,
      string activityType
   )
   {
      var path = PublicParticipantTeamFlag.GetPath(
         isTeamSport,
         activityType,
         "pl"
      );

      Assert.Null(path);
   }
}
