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
      var orderedName = OrderCommaSeparatedName(trimmed);

      return NameWordRegex().Replace(
         orderedName,
         match => LooksShoutedWord(match.Value)
            ? FormatShoutedWord(match.Value)
            : match.Value
      );
   }

   private static string OrderCommaSeparatedName(string value)
   {
      var commaIndex = value.IndexOf(',');

      if(commaIndex <= 0 ||
         commaIndex != value.LastIndexOf(',') ||
         commaIndex == value.Length - 1)
      {
         return value;
      }

      var lastName = value[..commaIndex].Trim();
      var firstNames = value[(commaIndex + 1)..].Trim();

      if(!ContainsLetter(lastName) || !ContainsLetter(firstNames))
      {
         return value;
      }

      return $"{firstNames} {lastName}";
   }

   private static bool ContainsLetter(string value)
   {
      return value.Any(char.IsLetter);
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
