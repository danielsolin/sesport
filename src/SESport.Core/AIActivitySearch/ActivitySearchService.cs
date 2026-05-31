using System.Security.Cryptography;
using System.Text;
using SESport.Core.Ingestion;

namespace SESport.Core.AIActivitySearch;

public sealed class ActivitySearchService(
   IActivitySearchModelClient modelClient
)
{
   private static readonly Source Source = new(
      new SourceId("source:ai-activity-search"),
      "AI activity search"
   );

   public async Task<ActivitySearchResult> SearchAsync(
      ActivitySearchRequest request,
      CancellationToken cancellationToken
   )
   {
      var modelResult = await modelClient.SearchAsync(
         request,
         cancellationToken
      );
      var proposals = modelResult.Proposals
         .Select(draft => ToActivityProposal(request, draft, modelResult))
         .ToList();

      return new ActivitySearchResult(
         request.Entity,
         proposals,
         modelResult.RawContent,
         modelResult.RawResponse
      );
   }

   private static ActivityProposal ToActivityProposal(
      ActivitySearchRequest request,
      ActivityProposalDraft draft,
      ActivitySearchModelResult modelResult
   )
   {
      var stableKey = CreateStableKey(request, draft);
      var activityTime = CreateActivityTime(draft);
      var evidence = draft.Evidence.Count > 0
         ? draft.Evidence.Select(ToEvidence).ToList()
         : [CreateFallbackEvidence(request, draft)];

      return new ActivityProposal(
         new ActivityProposalId($"activity-proposal:ai:{stableKey}"),
         ActivityProposalProducerType.AiSearch,
         Source,
         new ExternalEntityId($"ai:{stableKey}"),
         $"ai:{stableKey}",
         draft.Title,
         draft.Description,
         modelResult.RawContent,
         ParseActivityType(draft.ActivityType),
         request.Entity.Sport,
         draft.Context,
         activityTime,
         [
            new ActivityProposalEntityLink(
               ToEntityId(request.Entity.WatchlistId.Value),
               ParseEntityRole(draft.EntityRole),
               draft.EntityExplanation,
               request.Entity.Name,
               draft.Confidence
            )
         ],
         evidence,
         draft.Confidence,
         ActivityProposalStatus.Pending,
         null,
         null
      );
   }

   private static ActivityTime CreateActivityTime(
      ActivityProposalDraft draft
   )
   {
      if (draft.LocalStartTime is null)
      {
         return ActivityTime.OnDate(
            draft.ActivityDate,
            "AI search did not provide a start time.",
            draft.TimeZoneId
         );
      }

      var startsAt = new DateTimeOffset(
         draft.ActivityDate,
         draft.LocalStartTime.Value,
         TimeSpan.Zero
      );

      return ActivityTime.Scheduled(startsAt, draft.TimeZoneId);
   }

   private static ActivityProposalEvidence ToEvidence(
      ActivityProposalEvidenceDraft draft
   )
   {
      var source = new Source(
         new SourceId(CreateSourceId(draft.SourceName)),
         draft.SourceName ?? "AI web search result"
      );

      return new ActivityProposalEvidence(
         source,
         draft.Uri,
         draft.Title,
         DateTimeOffset.UtcNow,
         draft.Summary,
         draft.RawExcerpt
      );
   }

   private static ActivityProposalEvidence CreateFallbackEvidence(
      ActivitySearchRequest request,
      ActivityProposalDraft draft
   )
   {
      return new ActivityProposalEvidence(
         Source,
         null,
         draft.Title,
         DateTimeOffset.UtcNow,
         $"AI search proposed this activity for {request.Entity.Name}.",
         null
      );
   }

   private static ActivityType ParseActivityType(string value)
   {
      return Enum.TryParse<ActivityType>(
         value,
         ignoreCase: true,
         out var activityType
      )
         ? activityType
         : ActivityType.OtherSportingActivity;
   }

   private static ActivityEntityRole ParseEntityRole(string value)
   {
      return Enum.TryParse<ActivityEntityRole>(
         value,
         ignoreCase: true,
         out var entityRole
      )
         ? entityRole
         : ActivityEntityRole.Other;
   }

   private static string CreateStableKey(
      ActivitySearchRequest request,
      ActivityProposalDraft draft
   )
   {
      var parts = string.Join(
         "|",
         request.Entity.WatchlistId.Value,
         draft.ActivityDate.ToString("yyyy-MM-dd"),
         draft.Title.Trim().ToUpperInvariant()
      );
      var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(parts));

      return Convert.ToHexString(bytes)[..24].ToLowerInvariant();
   }

   private static EntityId ToEntityId(string stableKey)
   {
      var bytes = Encoding.UTF8.GetBytes(stableKey);
      var hash = MD5.HashData(bytes);

      return new EntityId(new Guid(hash));
   }

   private static string CreateSourceId(string? sourceName)
   {
      if (string.IsNullOrWhiteSpace(sourceName))
      {
         return "source:ai-web-search";
      }

      var normalized = sourceName.Trim().ToLowerInvariant();
      var builder = new StringBuilder("source:ai:");

      foreach (var character in normalized)
      {
         builder.Append(char.IsLetterOrDigit(character) ? character : '-');
      }

      return builder.ToString();
   }
}
