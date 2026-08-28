using System.Globalization;
using System.Text;

namespace SESport.Core.Broadcast;

public static class BroadcastActivityTypeResolver
{
   public static ActivityType? ResolveActivityType(
      string title,
      string? description,
      IReadOnlyCollection<string> categories,
      string? sportId = null
   )
   {
      var normalizedTitle = NormalizeText(title);
      var normalizedCategories = categories
         .Select(NormalizeText)
         .Where(token => !string.IsNullOrWhiteSpace(token))
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();

      var normalizedText = NormalizeText($"{title} {description}");
      var normalizedSportId = sportId?.Trim().ToLowerInvariant() ??
         string.Empty;

      if(ContainsTitleToken(normalizedTitle, ["qualification", "kval"]))
      {
         return ActivityType.Qualification;
      }

      if(ContainsTitleToken(
         normalizedTitle,
         ["practice", "träning", "traning"]
      ))
      {
         return ActivityType.Practice;
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

      if(ContainsAny(normalizedCategories, normalizedText, ["stage", "etapp"]))
      {
         return ActivityType.Stage;
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

      if(ContainsAny(normalizedCategories, normalizedText, ["golf"]))
      {
         return ActivityType.Tournament;
      }

      if(
         IsRaceSport(normalizedSportId) ||
         ContainsAny(
            normalizedCategories,
            normalizedText,
            [
               SportIds.Motorsport,
               SportIds.Motocross,
               SportIds.Rally,
               SportIds.Speedway,
               SportIds.Cycling,
               "cykel",
               "mountainbike",
               "mountain bike"
            ]
         )
      )
      {
         return ActivityType.Race;
      }

      if(IsMatchSport(normalizedSportId))
      {
         return ActivityType.Match;
      }

      if(IsAthleticsSport(normalizedSportId))
      {
         return ActivityType.Event;
      }

      return null;
   }

   private static bool IsAthleticsSport(string normalizedSportId)
   {
      return normalizedSportId == SportIds.Athletics;
   }

   private static bool IsRaceSport(string normalizedSportId)
   {
      return normalizedSportId is
         SportIds.Cycling or
         SportIds.Motocross or
         SportIds.Motorsport or
         SportIds.Rally or
         SportIds.Speedway;
   }

   private static bool IsMatchSport(string normalizedSportId)
   {
      return normalizedSportId is
         SportIds.Basketball or
         SportIds.BeachVolleyball or
         SportIds.Darts or
         SportIds.Football or
         SportIds.Handball or
         SportIds.IceHockey or
         SportIds.TableTennis or
         SportIds.Tennis or
         SportIds.Volleyball;
   }

   private static bool ContainsTitleToken(
      string normalizedTitle,
      IReadOnlyCollection<string> tokens
   )
   {
      return tokens.Any(token =>
         ContainsTextToken(normalizedTitle, token)
      );
   }

   private static bool ContainsAny(
      IReadOnlyCollection<string> normalizedCategories,
      string normalizedText,
      IReadOnlyCollection<string> tokens
   )
   {
      return tokens.Any(token =>
         normalizedCategories.Any(category =>
            ContainsTextToken(category, token)
         ) ||
         ContainsTextToken(normalizedText, token)
      );
   }

   private static bool ContainsTextToken(string normalizedText, string token)
   {
      var normalizedToken = NormalizeText(token);

      if(normalizedToken.Contains(' '))
      {
         return normalizedText.Contains(
            normalizedToken,
            StringComparison.OrdinalIgnoreCase
         );
      }

      return normalizedText
         .Split(' ', StringSplitOptions.RemoveEmptyEntries)
         .Contains(normalizedToken, StringComparer.OrdinalIgnoreCase);
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

}
