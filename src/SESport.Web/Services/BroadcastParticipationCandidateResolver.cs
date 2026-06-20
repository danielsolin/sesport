using System.Globalization;
using System.Text;

using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Data;

namespace SESport.Web.Services;

public static class BroadcastParticipationCandidateResolver
{
   private const int MaxCandidates = 5;

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

      var matches = candidates
         .Where(candidate =>
            IsGenderCompatible(candidate.PersonGenderId, requiredGenderId)
         )
         .Select(candidate => CreateMatch(normalizedTitle, candidate))
         .Where(match => match is not null)
         .Select(match => match!)
         .OrderByDescending(match => match.Score)
         .ThenBy(match => match.Name, StringComparer.OrdinalIgnoreCase)
         .Take(MaxCandidates)
         .ToList();

      if(matches.Count == 0)
      {
         return string.Empty;
      }

      return string.Join(
         Environment.NewLine,
         matches.Select(match => $"- {match.Name}")
      );
   }

   private static CandidateMatch? CreateMatch(
      string normalizedTitle,
      EntityOption candidate
   )
   {
      var nameMatch = MatchValue(normalizedTitle, candidate.Name);
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

      if(nameMatch is null && organizationMatch is null)
      {
         return null;
      }

      var score = Math.Max(
         nameMatch?.Score ?? 0,
         organizationMatch?.Score ?? 0
      );

      return new CandidateMatch(candidate.Name.Trim(), score);
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

      if(string.Equals(
         normalizedTitle,
         normalizedValue,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return new CandidateMatch(value.Trim(), 3000 + normalizedValue.Length);
      }

      if(normalizedTitle.Contains(
         normalizedValue,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return new CandidateMatch(value.Trim(), 2000 + normalizedValue.Length);
      }

      if(normalizedValue.Contains(
         normalizedTitle,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return new CandidateMatch(value.Trim(), 1000 + normalizedTitle.Length);
      }

      return null;
   }

   private static string? DetermineGenderId(
      string title,
      IReadOnlyCollection<string> categories
   )
   {
      var normalizedText = NormalizeText(
         $"{title} {string.Join(' ', categories)}"
      );

      if(ContainsAny(
         normalizedText,
         ["damer", "women", "female", "ladies"]
      ))
      {
         return PersonGenderIds.Female;
      }

      if(ContainsAny(
         normalizedText,
         ["herrar", "men", "male", "gentlemen"]
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

   private static bool ContainsAny(
      string normalizedText,
      IReadOnlyCollection<string> tokens
   )
   {
      return tokens.Any(token =>
         normalizedText.Contains(token, StringComparison.OrdinalIgnoreCase)
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
      string Name,
      int Score
   );
}
