using SESport.Core.Configuration;
using SESport.Core.Domain;

namespace SESport.Web.Formatting;

public static class PublicParticipantTeamFlag
{
   public static string? GetPath(
      bool isTeamSport,
      string activityType,
      string? teamCountryId,
      bool hasPrimaryCountryTeam
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
      if(!hasPrimaryCountryTeam ||
         string.IsNullOrWhiteSpace(normalizedTeamCountryId) ||
         string.Equals(
            normalizedTeamCountryId,
            PrimaryCountry.Id,
            StringComparison.Ordinal
         ) ||
         normalizedTeamCountryId is CountryIds.Europe or CountryIds.International)
      {
         return null;
      }

      return PublicCountryFlag.GetPath(normalizedTeamCountryId);
   }
}
