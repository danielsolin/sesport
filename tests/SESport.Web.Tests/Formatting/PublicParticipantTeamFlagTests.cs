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
         "pl",
         true
      );

      Assert.Equal("/images/flags/pl.svg", path);
   }

   [Theory]
   [InlineData("al")]
   [InlineData("at")]
   [InlineData("ba")]
   [InlineData("be")]
   [InlineData("bg")]
   [InlineData("ch")]
   [InlineData("cy")]
   [InlineData("cz")]
   [InlineData("de")]
   [InlineData("dk")]
   [InlineData("es")]
   [InlineData("fi")]
   [InlineData("fr")]
   [InlineData("gr")]
   [InlineData("hr")]
   [InlineData("hu")]
   [InlineData("il")]
   [InlineData("is")]
   [InlineData("it")]
   [InlineData("jp")]
   [InlineData("lt")]
   [InlineData("nl")]
   [InlineData("no")]
   [InlineData("pt")]
   [InlineData("ro")]
   [InlineData("rs")]
   [InlineData("sk")]
   [InlineData("tr")]
   [InlineData("uk")]
   [InlineData("us")]
   public void GetPathReturnsPathForKnownTeamCountry(string countryId)
   {
      var path = PublicParticipantTeamFlag.GetPath(
         true,
         nameof(ActivityType.Match),
         countryId,
         true
      );

      Assert.Equal($"/images/flags/{countryId}.svg", path);
   }

   [Fact]
   public void GetPathHidesCountriesWithoutLocalFlagAsset()
   {
      var path = PublicParticipantTeamFlag.GetPath(
         true,
         nameof(ActivityType.Match),
         "int",
         true
      );

      Assert.Null(path);
   }

   [Fact]
   public void GetPathHidesPrimaryCountryFlag()
   {
      var path = PublicParticipantTeamFlag.GetPath(
         true,
         nameof(ActivityType.Match),
         PrimaryCountry.Id,
         true
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
         "pl",
         true
      );

      Assert.Null(path);
   }

   [Fact]
   public void GetPathHidesForeignTeamWithoutPrimaryCountryOpponent()
   {
      var path = PublicParticipantTeamFlag.GetPath(
         true,
         nameof(ActivityType.Match),
         "pl",
         false
      );

      Assert.Null(path);
   }
}
