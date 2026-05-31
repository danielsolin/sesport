using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SESport.Core.AIActivitySearch;

public sealed class OpenAiResponsesActivitySearchClient
   : IActivitySearchModelClient
{
   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   )
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   private readonly HttpClient httpClient;
   private readonly OpenAiResponsesActivitySearchClientOptions options;

   public OpenAiResponsesActivitySearchClient(
      HttpClient httpClient,
      OpenAiResponsesActivitySearchClientOptions options
   )
   {
      this.httpClient = httpClient;
      this.options = options;

      if (!string.IsNullOrWhiteSpace(options.ApiKey))
      {
         httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.ApiKey);
      }
   }

   public async Task<ActivitySearchModelResult> SearchAsync(
      ActivitySearchRequest request,
      CancellationToken cancellationToken
   )
   {
      var response = await httpClient.PostAsJsonAsync(
         new Uri(options.BaseAddress, "responses"),
         CreateRequestPayload(request),
         JsonOptions,
         cancellationToken
      );

      var rawResponse = await response.Content.ReadAsStringAsync(
         cancellationToken
      );

      if (!response.IsSuccessStatusCode)
      {
         throw new HttpRequestException(
            $"AI activity search failed with {(int)response.StatusCode}: " +
            rawResponse
         );
      }

      var rawContent = ExtractOutputText(rawResponse);
      var proposals = ParseProposals(rawContent);

      return new ActivitySearchModelResult(
         rawContent,
         rawResponse,
         proposals
      );
   }

   private object CreateRequestPayload(ActivitySearchRequest request)
   {
      var tools = request.AllowWebSearch
         ? new object[] { new { type = "web_search" } }
         : [];

      return new
      {
         model = options.Model,
         input = CreatePrompt(request),
         tools,
         tool_choice = request.AllowWebSearch ? "auto" : null
      };
   }

   private static string CreatePrompt(ActivitySearchRequest request)
   {
      return $$"""
      You are finding current sports activity proposals for SESport.

      Search for concrete upcoming or very recent sport-related activities
      connected to this Sweden-relevant tracked entity. Prefer official
      schedules, federation pages, competition pages, team pages, and reliable
      news sources. Do not invent events. If evidence is weak, return an empty
      proposals array.

      Entity:
      - watchlistId: {{request.Entity.WatchlistId.Value}}
      - name: {{request.Entity.Name}}
      - type: {{request.Entity.Type}}
      - sport: {{request.Entity.Sport.Name}}
      - Sweden connection: {{request.Entity.SwedenConnection}}
      - current status: {{request.Entity.CurrentRelationshipOrStatus}}
      - likely activity types: {{string.Join(", ",
         request.Entity.LikelyActivityTypes)}}
      - suggested evidence sources: {{request.Entity.SuggestedEvidenceSources}}
      - notes: {{request.Entity.Notes}}

      Search date: {{request.SearchDate:yyyy-MM-dd}}
      Maximum proposals: {{request.MaxProposals}}

      Return only JSON with this shape:
      {
        "proposals": [
          {
            "title": "Sweden vs Finland",
            "description": "Short factual explanation.",
            "activityType": "Match",
            "activityDate": "2026-06-01",
            "localStartTime": "19:00",
            "timeZoneId": "Europe/Stockholm",
            "context": "Competition or surrounding context",
            "entityRole": "CompetesIn",
            "entityExplanation": "Why this entity is connected.",
            "confidence": 0.85,
            "evidence": [
              {
                "sourceName": "Source name",
                "uri": "https://example.test/source",
                "title": "Source page title",
                "summary": "What the source supports.",
                "rawExcerpt": "Short excerpt if available."
              }
            ]
          }
        ]
      }

      Use only these activityType values when possible:
      Match, Race, Tournament, Stage, Championship, Qualification,
      RosterAnnouncement, Transfer, Ranking, CoachingRole,
      OtherSportingActivity.

      Use only these entityRole values when possible:
      CompetesIn, PlaysForContext, SelectedForRoster, TransferSubject,
      CoachingRole, RecurringEventEdition, RelatedOrganization, Other.
      """;
   }

   private static string ExtractOutputText(string rawResponse)
   {
      using var document = JsonDocument.Parse(rawResponse);
      var root = document.RootElement;

      if (
         root.TryGetProperty("output_text", out var outputText) &&
         outputText.ValueKind == JsonValueKind.String
      )
      {
         return outputText.GetString() ?? "";
      }

      if (!root.TryGetProperty("output", out var output))
      {
         return rawResponse;
      }

      foreach (var item in output.EnumerateArray())
      {
         if (!item.TryGetProperty("content", out var content))
         {
            continue;
         }

         foreach (var contentItem in content.EnumerateArray())
         {
            if (
               contentItem.TryGetProperty("text", out var text) &&
               text.ValueKind == JsonValueKind.String
            )
            {
               return text.GetString() ?? "";
            }
         }
      }

      return rawResponse;
   }

   private static IReadOnlyCollection<ActivityProposalDraft> ParseProposals(
      string rawContent
   )
   {
      var content = StripJsonFence(rawContent);
      var document = JsonSerializer.Deserialize<ActivitySearchResponseDto>(
         content,
         JsonOptions
      );

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
         dto.ActivityType ?? "OtherSportingActivity",
         date,
         localStartTime,
         dto.TimeZoneId ?? "Europe/Stockholm",
         dto.Context,
         dto.EntityRole ?? "Other",
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
