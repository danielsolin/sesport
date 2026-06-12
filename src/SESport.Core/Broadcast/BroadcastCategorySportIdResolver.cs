using System.Globalization;
using System.Text;

namespace SESport.Core.Broadcast;

public static class BroadcastCategorySportIdResolver
{
   public static string? ResolveSportId(
      IEnumerable<string> categories
   )
   {
      var normalizedCategories = categories
         .Select(NormalizeCategoryKey)
         .Where(category => !string.IsNullOrWhiteSpace(category))
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();

      foreach(var category in normalizedCategories)
      {
         if(TryGetSpecificSportId(category, out var sportId))
         {
            return sportId;
         }
      }

      foreach(var category in normalizedCategories)
      {
         if(TryGetGenericSportId(category, out var sportId))
         {
            return sportId;
         }
      }

      return null;
   }

   private static bool TryGetSpecificSportId(
      string category,
      out string sportId
   )
   {
      switch(category)
      {
         case "golf":
            sportId = "golf";
            return true;
         case "fotboll":
            sportId = "football";
            return true;
         case "ishockey":
         case "ishockeyvm":
            sportId = "ice-hockey";
            return true;
         case "basket":
            sportId = "basketball";
            return true;
         case "dart":
            sportId = "darts";
            return true;
         case "friidrott":
            sportId = "athletics";
            return true;
         case "maraton":
         case "terranglopning":
            sportId = "athletics-road-running";
            return true;
         case "handboll":
            sportId = "handball";
            return true;
         case "segling":
            sportId = "sailing";
            return true;
         case "speedway":
            sportId = "speedway";
            return true;
         case "tennis":
            sportId = "tennis";
            return true;
         case "volleyball":
            sportId = "volleyball";
            return true;
         case "formel1":
         case "formele":
         case "motocross":
         case "motorcykel":
         case "motorsport":
            sportId = "motorsport";
            return true;
         case "djursport":
         case "galoppsport":
         case "hoppning":
         case "ridsport":
            sportId = "equestrian";
            return true;
      }

      sportId = string.Empty;
      return false;
   }

   private static bool TryGetGenericSportId(
      string category,
      out string sportId
   )
   {
      switch(category)
      {
         case "baseball":
         case "bollsport":
         case "cycling":
         case "cykling":
         case "extremsport":
         case "faktning":
         case "fysisksport":
         case "fysisksporter":
         case "kampsport":
         case "klattring":
         case "livesport":
         case "malsport":
         case "mountainbike":
         case "multisportlopp":
         case "racketsport":
         case "sporttavlingar":
         case "triathlon":
         case "tyngdlyftning":
         case "varldscupen":
         case "vattensport":
            sportId = "multi-sport";
            return true;
      }

      sportId = string.Empty;
      return false;
   }

   private static string NormalizeCategoryKey(string value)
   {
      var normalized = value.Normalize(NormalizationForm.FormD);
      var builder = new StringBuilder(normalized.Length);

      foreach(var character in normalized)
      {
         if(CharUnicodeInfo.GetUnicodeCategory(character) ==
            UnicodeCategory.NonSpacingMark)
         {
            continue;
         }

         if(char.IsLetterOrDigit(character))
         {
            builder.Append(char.ToLowerInvariant(character));
         }
      }

      return builder.ToString();
   }
}
