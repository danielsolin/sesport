using SESport.Core.Broadcast;
using SESport.Data;

namespace SESport.Web.Services;

public static class BroadcastParticipationCandidateResolver
{
   private const int MaxCandidates = 5;

   public static string CreateCandidatesText(
      string broadcastTitle,
      IReadOnlyCollection<EntityOption> candidates
   )
   {
      if(string.IsNullOrWhiteSpace(broadcastTitle) || candidates.Count == 0)
      {
         return string.Empty;
      }

      var normalizedTitle = BroadcastEntityFilter.NormalizeName(
         broadcastTitle
      );

      var matches = candidates
         .Select(candidate =>
            CreateMatch(normalizedTitle, candidate)
         )
         .Where(match => match is not null)
         .Select(match => match!)
         .OrderByDescending(match => match.Score)
         .ThenBy(match => match.Name, StringComparer.OrdinalIgnoreCase)
         .ThenBy(
            match => match.Hint ?? string.Empty,
            StringComparer.OrdinalIgnoreCase
         )
         .Take(MaxCandidates)
         .ToList();

      if(matches.Count == 0)
      {
         return string.Empty;
      }

      return string.Join(
         Environment.NewLine,
         matches.Select(FormatMatch)
      );
   }

   private static CandidateMatch? CreateMatch(
      string normalizedTitle,
      EntityOption candidate
   )
   {
      var nameMatch = MatchValue(
         normalizedTitle,
         candidate.Name,
         candidate.Name
      );
      var organizationMatch = candidate.Organization
         .Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries
               | StringSplitOptions.TrimEntries
         )
         .Select(organization =>
            MatchValue(
               normalizedTitle,
               candidate.Name,
               organization
            )
         )
         .Where(match => match is not null)
         .Select(match => match!)
         .OrderByDescending(match => match.Score)
         .FirstOrDefault();

      if(nameMatch is null && organizationMatch is null)
      {
         return null;
      }

      if(nameMatch is null)
      {
         return organizationMatch;
      }

      if(organizationMatch is null)
      {
         return nameMatch;
      }

      return organizationMatch.Score >= nameMatch.Score
         ? organizationMatch
         : nameMatch with { Hint = organizationMatch.Hint };
   }

   private static CandidateMatch? MatchValue(
      string normalizedTitle,
      string name,
      string value
   )
   {
      var normalizedValue = BroadcastEntityFilter.NormalizeName(value);

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
         return new CandidateMatch(
            name.Trim(),
            null,
            3000 + normalizedValue.Length
         );
      }

      if(normalizedTitle.Contains(
         normalizedValue,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return new CandidateMatch(
            name.Trim(),
            value.Trim(),
            2000 + normalizedValue.Length
         );
      }

      if(normalizedValue.Contains(
         normalizedTitle,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return new CandidateMatch(
            name.Trim(),
            null,
            1000 + normalizedTitle.Length
         );
      }

      return null;
   }

   private static string FormatMatch(CandidateMatch match)
   {
      return $"- {match.Name}";
   }

   private sealed record CandidateMatch(
      string Name,
      string? Hint,
      int Score
   );
}
