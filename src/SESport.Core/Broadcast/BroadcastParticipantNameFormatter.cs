using System.Globalization;
using System.Text.RegularExpressions;

namespace SESport.Core.Broadcast;

public static partial class BroadcastParticipantNameFormatter
{
   public static string Format(string value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return string.Empty;
      }

      var trimmed = WhitespaceRegex()
         .Replace(value.Trim(), " ");

      return NameWordRegex().Replace(
         trimmed,
         match => LooksShoutedWord(match.Value)
            ? FormatShoutedWord(match.Value)
            : match.Value
      );
   }

   private static bool LooksShoutedWord(string value)
   {
      var letterCount = 0;

      foreach(var character in value)
      {
         if(!char.IsLetter(character))
         {
            continue;
         }

         letterCount++;

         if(!char.IsUpper(character))
         {
            return false;
         }
      }

      return letterCount >= 2;
   }

   private static string FormatShoutedWord(string value)
   {
      var lowered = value.ToLowerInvariant();
      return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(lowered);
   }

   [GeneratedRegex(@"\s+")]
   private static partial Regex WhitespaceRegex();

   [GeneratedRegex(@"\b[\p{L}]+\b")]
   private static partial Regex NameWordRegex();
}
