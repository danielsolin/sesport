using SESport.Core.Domain;

namespace SESport.Web.Formatting;

public static class PublicCountryName
{
    private static readonly Dictionary<string, string> Names =
       new(StringComparer.OrdinalIgnoreCase)
       {
          [PrimaryCountry.Id] = PrimaryCountry.LocalDisplayName,
          ["al"] = "Albanien",
          ["at"] = "Österrike",
          ["ba"] = "Bosnien-Hercegovina",
          ["be"] = "Belgien",
          ["bg"] = "Bulgarien",
          ["ch"] = "Schweiz",
          ["cy"] = "Cypern",
          ["cz"] = "Tjeckien",
          ["de"] = "Tyskland",
          ["dk"] = "Danmark",
          ["es"] = "Spanien",
          ["fi"] = "Finland",
          ["fr"] = "Frankrike",
          ["gr"] = "Grekland",
          ["hr"] = "Kroatien",
          ["hu"] = "Ungern",
          ["il"] = "Israel",
          ["is"] = "Island",
          ["it"] = "Italien",
          ["jp"] = "Japan",
          ["lt"] = "Litauen",
          ["nl"] = "Nederländerna",
          ["no"] = "Norge",
          ["pl"] = "Polen",
          ["pt"] = "Portugal",
          ["ro"] = "Rumänien",
          ["rs"] = "Serbien",
          ["sk"] = "Slovakien",
          ["tr"] = "Turkiet",
          ["uk"] = "Storbritannien",
          ["us"] = "USA",
          [CountryIds.Europe] = "Europa",
          [CountryIds.International] = "Internationellt"
       };

    public static string GetDisplayName(string? countryId)
    {
       if(string.IsNullOrWhiteSpace(countryId))
       {
          return string.Empty;
       }

       var normalizedCountryId = countryId.Trim().ToLowerInvariant();
       return Names.TryGetValue(normalizedCountryId, out var name)
          ? name
          : normalizedCountryId.ToUpperInvariant();
    }
}
