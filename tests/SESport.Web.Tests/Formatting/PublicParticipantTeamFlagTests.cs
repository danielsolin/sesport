using SESport.Core.Configuration;
using SESport.Core.Domain;
using SESport.Web.Formatting;

namespace SESport.Core.Tests.Formatting;

public sealed class PublicParticipantTeamFlagTests
{
   [Fact]
   public void GetPathReturnsFlagForForeignTeamInPrimaryCountryMatch()
   {
      var path = PublicParticipantTeamFlag.GetPath(
         true,
         nameof(ActivityType.Match),
         "pl",
         true
      );

      Assert.Equal("/images/flags/pl.svg", path);
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

   [Fact]
   public void GetPathHidesPrimaryCountryTeam()
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
   public void GetPathHidesFlagOutsidePrimaryCountryTeamMatch(
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

   [Theory]
   [InlineData(CountryIds.Europe)]
   [InlineData(CountryIds.International)]
   [InlineData("unknown")]
   public void GetPathHidesUnsupportedTeamCountry(string countryId)
   {
      var path = PublicParticipantTeamFlag.GetPath(
         true,
         nameof(ActivityType.Match),
         countryId,
         true
      );

      Assert.Null(path);
   }
}
