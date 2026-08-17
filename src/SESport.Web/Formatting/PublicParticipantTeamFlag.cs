using SESport.Core.Domain;

namespace SESport.Web.Formatting;

public static class PublicParticipantTeamFlag
{
   private static readonly IReadOnlySet<string> AvailableCountryIds =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
         PrimaryCountry.Id,
         "be",
         "de",
         "it",
         "no",
         "pl",
         "pt",
         "uk",
         "us"
      };

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

      if(!hasPrimaryCountryTeam ||
         string.IsNullOrWhiteSpace(teamCountryId) ||
         string.Equals(
            teamCountryId.Trim(),
            PrimaryCountry.Id,
            StringComparison.OrdinalIgnoreCase
         ))
      {
         return null;
      }

      var normalizedCountryId = teamCountryId.Trim().ToLowerInvariant();

      return normalizedCountryId.Length == 2 &&
         normalizedCountryId.All(char.IsAsciiLetter) &&
         AvailableCountryIds.Contains(normalizedCountryId)
         ? $"/images/flags/{normalizedCountryId}.svg"
         : null;
   }
}
