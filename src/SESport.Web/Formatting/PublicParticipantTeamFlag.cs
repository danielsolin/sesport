using SESport.Core.Configuration;
using SESport.Core.Domain;

namespace SESport.Web.Formatting;

public static class PublicParticipantTeamFlag
{
   public static string? GetPath(
      bool isTeamSport,
      string activityType,
      string? teamCountryId,
      bool hasPrimaryCountryTeam,
      bool isNationalTeamActivity
   )
   {
      if(!isTeamSport || !string.Equals(
         activityType,
         nameof(ActivityType.Match),
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return null;
      }

      var normalizedTeamCountryId = teamCountryId?.Trim().ToLowerInvariant();
      if(string.IsNullOrWhiteSpace(normalizedTeamCountryId) ||
         normalizedTeamCountryId is CountryIds.Europe or CountryIds.International)
      {
         return null;
      }

      var isPrimaryCountryTeam = string.Equals(
         normalizedTeamCountryId,
         PrimaryCountry.Id,
         StringComparison.Ordinal
      );
      var showPrimaryCountryTeam =
         isPrimaryCountryTeam && isNationalTeamActivity;
      var showForeignTeam = !isPrimaryCountryTeam && hasPrimaryCountryTeam;
      if(!showPrimaryCountryTeam && !showForeignTeam)
      {
         return null;
      }

      return PublicCountryFlag.GetPath(normalizedTeamCountryId);
   }
}
