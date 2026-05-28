namespace SESport.Sources.Iihf;

public static class IihfCountryCodes
{
   private static readonly IReadOnlyDictionary<string, string> NamesByCode =
      new Dictionary<string, string>
      {
         ["AUT"] = "Austria",
         ["CAN"] = "Canada",
         ["CZE"] = "Czechia",
         ["DEN"] = "Denmark",
         ["FIN"] = "Finland",
         ["GBR"] = "Great Britain",
         ["GER"] = "Germany",
         ["HUN"] = "Hungary",
         ["ITA"] = "Italy",
         ["LAT"] = "Latvia",
         ["NOR"] = "Norway",
         ["SLO"] = "Slovenia",
         ["SUI"] = "Switzerland",
         ["SVK"] = "Slovakia",
         ["SWE"] = "Sweden",
         ["USA"] = "United States"
      };

   public static string GetName(string code)
   {
      return NamesByCode[code];
   }
}
