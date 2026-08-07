using System.Globalization;
using System.Text;

using SESport.Core.Configuration;

namespace SESport.Core.Broadcast;

public static class BroadcastActivityMatchScorer
{
   public static int GetScore(
      string broadcastTitle,
      string activityTitle,
      DateTime broadcastLocalStart,
      DateTime activityLocalStart
   )
   {
      var normalizedBroadcastTitle = NormalizeMatchText(broadcastTitle);
      var normalizedActivityTitle = NormalizeMatchText(activityTitle);
      var hourDistance = Math.Abs(
         (broadcastLocalStart - activityLocalStart).TotalHours
      );

      if(hourDistance > ActivityGroupDefaults.MatchWindowHours)
      {
         return 0;
      }

      var titleScore = 0;

      if(!string.IsNullOrWhiteSpace(normalizedBroadcastTitle) &&
         !string.IsNullOrWhiteSpace(normalizedActivityTitle) &&
         string.Equals(
            normalizedBroadcastTitle,
            normalizedActivityTitle,
            StringComparison.Ordinal
         ))
      {
         titleScore = 30;
      }
      else if(!string.IsNullOrWhiteSpace(normalizedBroadcastTitle) &&
         !string.IsNullOrWhiteSpace(normalizedActivityTitle))
      {
         var broadcastTokens = GetMatchTokens(normalizedBroadcastTitle);
         var activityTokens = GetMatchTokens(normalizedActivityTitle);

         if(normalizedBroadcastTitle.Contains(
            normalizedActivityTitle,
            StringComparison.Ordinal
         ) || normalizedActivityTitle.Contains(
            normalizedBroadcastTitle,
            StringComparison.Ordinal
         ))
         {
            titleScore = 20;
         }
         else
         {
            var overlap = broadcastTokens.Intersect(activityTokens).Count();

            titleScore = Math.Min(15, overlap * 5);
         }
      }

      var timeScore = (int)Math.Round(
         (ActivityGroupDefaults.MatchWindowHours - hourDistance) * 60,
         MidpointRounding.AwayFromZero
      );

      return timeScore + titleScore + 1;
   }

   private static IReadOnlyCollection<string> GetMatchTokens(string value)
   {
      var normalized = NormalizeMatchText(value);

      if(string.IsNullOrWhiteSpace(normalized))
      {
         return [];
      }

      return normalized
         .Split(' ', StringSplitOptions.RemoveEmptyEntries)
         .Where(token => !IsYearToken(token))
         .Distinct(StringComparer.Ordinal)
         .ToArray();
   }

   private static bool IsYearToken(string token)
   {
      if(token.Length != 4 ||
         !int.TryParse(
            token,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var year
         ))
      {
         return false;
      }

      return year is >= 1900 and <= 2100;
   }

   private static string NormalizeMatchText(string value)
   {
      var normalized = value.Normalize(NormalizationForm.FormD);
      var builder = new StringBuilder();
      var lastWasSeparator = false;

      foreach(var character in normalized)
      {
         var category = CharUnicodeInfo.GetUnicodeCategory(character);

         if(category == UnicodeCategory.NonSpacingMark)
         {
            continue;
         }

         if(char.IsLetterOrDigit(character))
         {
            builder.Append(char.ToLowerInvariant(character));
            lastWasSeparator = false;
            continue;
         }

         if(!lastWasSeparator)
         {
            builder.Append(' ');
            lastWasSeparator = true;
         }
      }

      return builder
         .ToString()
         .Normalize(NormalizationForm.FormC)
         .Trim();
   }
}
