using SESport.Core.Configuration;

namespace SESport.Web.Formatting;

public static class PublicCountryFlag
{
   private static readonly IReadOnlySet<string> AvailableCountryIds =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
         PrimaryCountry.Id,
         "at",
         "be",
         "de",
         "it",
         "no",
         "pl",
         "pt",
         "uk",
         "us"
      };

   public static string? GetPath(string? countryId)
   {
      if(string.IsNullOrWhiteSpace(countryId))
      {
         return null;
      }

      var normalizedCountryId = countryId.Trim().ToLowerInvariant();

      return normalizedCountryId.Length == 2 &&
         normalizedCountryId.All(char.IsAsciiLetter) &&
         AvailableCountryIds.Contains(normalizedCountryId)
         ? $"/images/flags/{normalizedCountryId}.svg"
         : null;
   }
}
