using System.Text.Json;
using SESport.Core.Domain;

namespace SESport.AI.ActivitySearch;

internal static class ActivitySearchResponseParser
{
   public static IReadOnlyCollection<ActivityProposalDraft> ParseProposals(
      string rawContent,
      JsonSerializerOptions jsonOptions
   )
   {
      var content = ExtractJsonObject(StripJsonFence(rawContent));

      if (content is null)
      {
         return [];
      }

      ActivitySearchResponseDto? document;

      try
      {
         document = JsonSerializer.Deserialize<ActivitySearchResponseDto>(
            content,
            jsonOptions
         );
      }
      catch (JsonException)
      {
         return [];
      }

      if (document?.Proposals is null)
      {
         return [];
      }

      return document.Proposals
         .Where(proposal => !string.IsNullOrWhiteSpace(proposal.Title))
         .Select(ToDraft)
         .ToList();
   }

   private static ActivityProposalDraft ToDraft(
      ActivityProposalDraftDto dto
   )
   {
      var date = DateOnly.Parse(dto.ActivityDate);
      var localStartTime = string.IsNullOrWhiteSpace(dto.LocalStartTime)
         ? (TimeOnly?)null
         : TimeOnly.Parse(dto.LocalStartTime);
      var evidence = dto.Evidence?
         .Where(item => !string.IsNullOrWhiteSpace(item.Summary))
         .Select(ToEvidenceDraft)
         .ToList() ?? [];

      return new ActivityProposalDraft(
         dto.Title,
         dto.Description,
         dto.ActivityType ?? ActivityType.OtherSportingActivity.ToString(),
         date,
         localStartTime,
         dto.TimeZoneId ?? SportDay.TimeZoneId,
         dto.Context,
         dto.EntityRole ?? ActivityEntityRole.Other.ToString(),
         dto.EntityExplanation ?? "AI search connected this activity.",
         dto.Confidence,
         evidence
      );
   }

   private static ActivityProposalEvidenceDraft ToEvidenceDraft(
      ActivityProposalEvidenceDraftDto dto
   )
   {
      var uri = Uri.TryCreate(dto.Uri, UriKind.Absolute, out var parsedUri)
         ? parsedUri
         : null;

      return new ActivityProposalEvidenceDraft(
         dto.SourceName,
         uri,
         dto.Title,
         dto.Summary ?? "AI search cited this source.",
         dto.RawExcerpt
      );
   }

   private static string StripJsonFence(string value)
   {
      var trimmed = value.Trim();

      if (!trimmed.StartsWith("```"))
      {
         return trimmed;
      }

      var firstNewLine = trimmed.IndexOf('\n');
      var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);

      if (firstNewLine < 0 || lastFence <= firstNewLine)
      {
         return trimmed;
      }

      return trimmed[(firstNewLine + 1)..lastFence].Trim();
   }

   private static string? ExtractJsonObject(string value)
   {
      var start = value.IndexOf('{');
      var end = value.LastIndexOf('}');

      if (start < 0 || end <= start)
      {
         return null;
      }

      return value[start..(end + 1)];
   }

   private sealed record ActivitySearchResponseDto(
      IReadOnlyCollection<ActivityProposalDraftDto>? Proposals
   );

   private sealed record ActivityProposalDraftDto(
      string Title,
      string? Description,
      string? ActivityType,
      string ActivityDate,
      string? LocalStartTime,
      string? TimeZoneId,
      string? Context,
      string? EntityRole,
      string? EntityExplanation,
      decimal? Confidence,
      IReadOnlyCollection<ActivityProposalEvidenceDraftDto>? Evidence
   );

   private sealed record ActivityProposalEvidenceDraftDto(
      string? SourceName,
      string? Uri,
      string? Title,
      string? Summary,
      string? RawExcerpt
   );
}
