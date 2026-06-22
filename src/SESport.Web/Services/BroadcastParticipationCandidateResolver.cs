using System.Globalization;
using System.Text;

using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Data;

namespace SESport.Web.Services;

public static class BroadcastParticipationCandidateResolver
{
   private const int MaxCandidateNames = 5;

   public static string CreateCandidatesText(
      BroadcastActivitySource broadcast,
      IReadOnlyCollection<EntityOption> candidates
   )
   {
      if(string.IsNullOrWhiteSpace(broadcast.Title) || candidates.Count == 0)
      {
         return string.Empty;
      }

      var requiredGenderId = DetermineGenderId(
         broadcast.Title,
         broadcast.Categories
      );
      var normalizedTitle = NormalizeText(
         $"{broadcast.Title} {string.Join(' ', broadcast.Categories)}"
      );

      var eligibleCandidates = candidates
         .Where(candidate =>
            IsGenderCompatible(candidate.PersonGenderId, requiredGenderId)
         )
         .ToList();

      var primaryCandidatesText = CreatePrimaryCandidatesText(
         normalizedTitle,
         eligibleCandidates
      );

      if(!string.IsNullOrWhiteSpace(primaryCandidatesText))
      {
         return primaryCandidatesText;
      }

      return CreateFallbackCandidatesText(
         normalizedTitle,
         eligibleCandidates
      );
   }

   private static string CreatePrimaryCandidatesText(
      string normalizedTitle,
      IReadOnlyCollection<EntityOption> candidates
   )
   {
      var matches = candidates
         .Select(candidate => CreateMatch(normalizedTitle, candidate))
         .Where(match => match is not null)
         .Select(match => match!)
         .ToList();

      return CreateCandidatesText(matches);
   }

   private static string CreateFallbackCandidatesText(
      string normalizedTitle,
      IReadOnlyCollection<EntityOption> candidates
   )
   {
      var fallbackMatches = candidates
         .Where(candidate =>
            MatchValue(normalizedTitle, candidate.Sport) is not null
         )
         .Select(candidate =>
            new CandidateMatch(
               candidate.Id,
               candidate.Name.Trim(),
               0,
               candidate.WatchPrioritySortOrder
            )
         )
         .ToList();

      fallbackMatches = OrderCandidateMatches(fallbackMatches)
         .Take(MaxCandidateNames)
         .ToList();

      return CreateCandidatesText(fallbackMatches);
   }

   private static string CreateCandidatesText(
      IEnumerable<CandidateMatch> matches
   )
   {
      var orderedMatches = OrderCandidateMatches(matches);

      return orderedMatches.Count == 0
         ? string.Empty
         : string.Join(
            Environment.NewLine,
            orderedMatches.Select(match => $"  - {match.Name}")
         );
   }

   private static List<CandidateMatch> OrderCandidateMatches(
      IEnumerable<CandidateMatch> matches
   )
   {
      return matches
         .OrderBy(match => match.WatchPrioritySortOrder)
         .ThenByDescending(match => match.Score)
         .ThenBy(match => match.Name, StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   private static CandidateMatch? CreateMatch(
      string normalizedTitle,
      EntityOption candidate
   )
   {
      var nameMatch = MatchValue(normalizedTitle, candidate.Name);
      var aliasMatch = string.IsNullOrWhiteSpace(candidate.AliasName)
         ? null
         : MatchValue(normalizedTitle, candidate.AliasName);
      var organizationMatch = candidate.Organization
         .Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries
               | StringSplitOptions.TrimEntries
         )
         .Select(organization => MatchValue(normalizedTitle, organization))
         .Where(match => match is not null)
         .Select(match => match!)
         .OrderByDescending(match => match.Score)
         .FirstOrDefault();

      if(nameMatch is null &&
         aliasMatch is null &&
         organizationMatch is null)
      {
         return null;
      }

      var score = Math.Max(
         nameMatch?.Score ?? 0,
         Math.Max(
            aliasMatch?.Score ?? 0,
            organizationMatch?.Score ?? 0
         )
      );

      return new CandidateMatch(
         candidate.Id,
         candidate.Name.Trim(),
         score,
         candidate.WatchPrioritySortOrder
      );
   }

   private static CandidateMatch? MatchValue(
      string normalizedTitle,
      string value
   )
   {
      var normalizedValue = NormalizeText(value);

      if(string.IsNullOrWhiteSpace(normalizedValue))
      {
         return null;
      }

      foreach(var pattern in CreatePatternVariants(normalizedValue))
      {
         if(string.Equals(
            normalizedTitle,
            pattern,
            StringComparison.OrdinalIgnoreCase
         ))
         {
            return new CandidateMatch(
               Guid.Empty,
               value.Trim(),
               3000 + pattern.Length,
               0
            );
         }

         if(normalizedTitle.Contains(
            pattern,
            StringComparison.OrdinalIgnoreCase
         ))
         {
            return new CandidateMatch(
               Guid.Empty,
               value.Trim(),
               2000 + pattern.Length,
               0
            );
         }

         if(pattern.Contains(
            normalizedTitle,
            StringComparison.OrdinalIgnoreCase
         ))
         {
            return new CandidateMatch(
               Guid.Empty,
               value.Trim(),
               1000 + normalizedTitle.Length,
               0
            );
         }
      }

      return null;
   }

   private static IEnumerable<string> CreatePatternVariants(string value)
   {
      yield return value;

      var lastSpaceIndex = value.LastIndexOf(' ');

      if(lastSpaceIndex < 0)
      {
         if(value.Equals("tour", StringComparison.OrdinalIgnoreCase))
         {
            yield return "world tour";
            yield return "touren";
         }

         if(value.Equals("series", StringComparison.OrdinalIgnoreCase))
         {
            yield return "world series";
            yield return "seriesen";
         }

         yield break;
      }

      var prefix = value[..lastSpaceIndex];
      var lastWord = value[(lastSpaceIndex + 1)..];

      if(lastWord.Equals("tour", StringComparison.OrdinalIgnoreCase))
      {
         yield return $"{prefix} world tour";
         yield return $"{prefix} touren";
      }

      if(lastWord.Equals("series", StringComparison.OrdinalIgnoreCase))
      {
         yield return $"{prefix} world series";
         yield return $"{prefix} seriesen";
      }
   }

   private static string? DetermineGenderId(
      string title,
      IReadOnlyCollection<string> categories
   )
   {
      var normalizedText = NormalizeText(
         $"{title} {string.Join(' ', categories)}"
      );

      if(ContainsAnyToken(
         normalizedText,
         [
            "dam",
            "damer",
            "damernas",
            "damallsvenskan",
            "women",
            "womens",
            "female",
            "ladies"
         ]
      ))
      {
         return PersonGenderIds.Female;
      }

      if(ContainsAnyToken(
         normalizedText,
         [
            "herr",
            "herrar",
            "herrarnas",
            "herrallsvenskan",
            "men",
            "mens",
            "male",
            "gentlemen"
         ]
      ))
      {
         return PersonGenderIds.Male;
      }

      return null;
   }

   private static bool IsGenderCompatible(
      string? candidateGenderId,
      string? requiredGenderId
   )
   {
      if(requiredGenderId is null)
      {
         return true;
      }

      return string.Equals(
         candidateGenderId,
         requiredGenderId,
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static bool ContainsAnyToken(
      string normalizedText,
      IReadOnlyCollection<string> tokens
   )
   {
      var paddedText = $" {normalizedText} ";

      return tokens.Any(token =>
         paddedText.Contains(
            $" {token} ",
            StringComparison.OrdinalIgnoreCase
         )
      );
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

   private sealed record CandidateMatch(
      Guid Id,
      string Name,
      int Score,
      int WatchPrioritySortOrder
   );
}
