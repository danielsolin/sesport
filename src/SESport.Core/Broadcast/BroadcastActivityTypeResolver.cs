using System.Globalization;
using System.Text;
using SESport.Core.Domain;

namespace SESport.Core.Broadcast;

public static class BroadcastActivityTypeResolver
{
   public static ActivityType? ResolveActivityType(
      string title,
      string? description,
      IReadOnlyCollection<string> categories
   )
   {
      var normalizedCategories = categories
         .Select(NormalizeToken)
         .Where(token => !string.IsNullOrWhiteSpace(token))
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();

      var normalizedText = NormalizeText($"{title} {description}");

      if(ContainsAny(normalizedCategories, normalizedText, ["golf"]))
      {
         return ActivityType.Tournament;
      }

      if(
         ContainsAny(
            normalizedCategories,
            normalizedText,
            ["qualifier", "qualification", "kval"]
         )
      )
      {
         return ActivityType.Qualification;
      }

      if(
         ContainsAny(
            normalizedCategories,
            normalizedText,
            [
               "championship",
               "world championship",
               "world cup",
               "vm",
               "em"
            ]
         )
      )
      {
         return ActivityType.Championship;
      }

      if(ContainsAny(normalizedCategories, normalizedText, ["stage", "etapp"]))
      {
         return ActivityType.Stage;
      }

      return null;
   }

   private static bool ContainsAny(
      IReadOnlyCollection<string> normalizedCategories,
      string normalizedText,
      IReadOnlyCollection<string> tokens
   )
   {
      return tokens.Any(token =>
         normalizedCategories.Contains(token, StringComparer.OrdinalIgnoreCase) ||
         ContainsTextToken(normalizedText, token)
      );
   }

   private static bool ContainsTextToken(string normalizedText, string token)
   {
      if(token.Contains(' '))
      {
         return normalizedText.Contains(token, StringComparison.OrdinalIgnoreCase);
      }

      return normalizedText
         .Split(' ', StringSplitOptions.RemoveEmptyEntries)
         .Contains(token, StringComparer.OrdinalIgnoreCase);
   }

   private static string NormalizeText(string value)
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
         else if(char.IsWhiteSpace(character))
         {
            builder.Append(' ');
         }
      }

      return string.Join(
         " ",
         builder
            .ToString()
            .Split(
               ' ',
               StringSplitOptions.RemoveEmptyEntries
                  | StringSplitOptions.TrimEntries
            )
      );
   }

   private static string NormalizeToken(string value)
   {
      var normalized = NormalizeText(value);
      return normalized.Replace(" ", string.Empty);
   }
}
