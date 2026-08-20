using SESport.Core.Configuration;
using SESport.Core.Domain;

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
         "us",
         CountryIds.Europe,
         CountryIds.International
      };

   public static string? GetPath(string? countryId)
   {
      if(string.IsNullOrWhiteSpace(countryId))
      {
         return null;
      }

      var normalizedCountryId = countryId.Trim().ToLowerInvariant();
      var isTwoLetterCountryId = normalizedCountryId.Length == 2 &&
         normalizedCountryId.All(char.IsAsciiLetter);
      var isInternationalCountryId = string.Equals(
         normalizedCountryId,
         CountryIds.International,
         StringComparison.Ordinal
      );

      return (isTwoLetterCountryId || isInternationalCountryId) &&
         AvailableCountryIds.Contains(normalizedCountryId)
         ? $"/images/flags/{normalizedCountryId}.svg"
         : null;
   }
}
