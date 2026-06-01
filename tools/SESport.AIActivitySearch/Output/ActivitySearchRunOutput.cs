using SESport.Core.AIActivitySearch;
using SESport.Core.Domain;
using SESport.Core.Identifiers;
using SESport.Core.Ingestion;
using SESport.Core.Sources;

namespace SESport.Tools.AIActivitySearch.Output;

internal sealed record ActivitySearchRunOutput(
   DateTimeOffset StartedAt,
   DateTimeOffset CompletedAt,
   string? RunDirectory,
   string ClientMode,
   string BaseAddress,
   string Model,
   string ApiKeySource,
   bool AllowWebSearch,
   string WebSearchToolType,
   string? LmStudioPluginId,
   DateOnly SearchDate,
   DateOnly WindowStart,
   DateOnly WindowEnd,
   int MaxProposals,
   bool WriteToDatabase,
   IReadOnlyCollection<ActivitySearchRunItemOutput> Items,
   IReadOnlyCollection<ActivitySearchResultOutput> Results
)
{
   public static ActivitySearchRunOutput Create(
      ToolOptions options,
      string? runDirectory,
      DateTimeOffset startedAt,
      DateTimeOffset completedAt,
      IReadOnlyCollection<ActivitySearchResult> results,
      IReadOnlyCollection<ActivitySearchRunItemOutput> items
   )
   {
      return new ActivitySearchRunOutput(
         startedAt,
         completedAt,
         runDirectory,
         options.ClientMode,
         options.EffectiveBaseAddress.ToString(),
         options.Model,
         options.ApiKeySource ?? "not configured",
         options.AllowWebSearch,
         options.WebSearchToolType,
         options.LmStudioPluginId,
         options.SearchDate,
         options.SearchDate.AddDays(-options.LookBackDays),
         options.SearchDate.AddDays(options.LookAheadDays),
         options.MaxProposals,
         options.WriteToDatabase,
         items,
         results.Select(result => ActivitySearchResultOutput.From(
            result,
            options.IncludeRaw
         )).ToList()
      );
   }
}

internal sealed record ActivitySearchRunItemOutput(
   string EntityId,
   string EntityName,
   string Status,
   int? ProposalCount,
   int? PersistedProposalCount,
   string? ResultPath,
   string? FailurePath,
   string? ErrorType,
   string? ErrorMessage,
   DateTimeOffset StartedAt,
   DateTimeOffset CompletedAt,
   double DurationSeconds
)
{
   public static ActivitySearchRunItemOutput Completed(
      ActivitySearchEntity entity,
      int proposalCount,
      int persistedProposalCount,
      string? resultPath,
      DateTimeOffset startedAt,
      DateTimeOffset completedAt
   )
   {
      return new ActivitySearchRunItemOutput(
         entity.WatchlistId.Value,
         entity.Name,
         "completed",
         proposalCount,
         persistedProposalCount,
         resultPath,
         null,
         null,
         null,
         startedAt,
         completedAt,
         GetDurationSeconds(startedAt, completedAt)
      );
   }

   public static ActivitySearchRunItemOutput Failed(
      ActivitySearchEntity entity,
      string? failurePath,
      Exception exception,
      DateTimeOffset startedAt,
      DateTimeOffset completedAt
   )
   {
      return new ActivitySearchRunItemOutput(
         entity.WatchlistId.Value,
         entity.Name,
         "failed",
         null,
         null,
         null,
         failurePath,
         exception.GetType().Name,
         exception.Message,
         startedAt,
         completedAt,
         GetDurationSeconds(startedAt, completedAt)
      );
   }

   private static double GetDurationSeconds(
      DateTimeOffset startedAt,
      DateTimeOffset completedAt
   )
   {
      return Math.Round((completedAt - startedAt).TotalSeconds, 3);
   }
}

internal sealed record ActivitySearchFailureOutput(
   ActivitySearchEntity Entity,
   string ErrorType,
   string ErrorMessage,
   string? StackTrace,
   DateTimeOffset StartedAt,
   DateTimeOffset CompletedAt,
   double DurationSeconds
)
{
   public static ActivitySearchFailureOutput From(
      ActivitySearchEntity entity,
      Exception exception,
      DateTimeOffset startedAt,
      DateTimeOffset completedAt
   )
   {
      return new ActivitySearchFailureOutput(
         entity,
         exception.GetType().FullName ?? exception.GetType().Name,
         exception.Message,
         exception.ToString(),
         startedAt,
         completedAt,
         Math.Round((completedAt - startedAt).TotalSeconds, 3)
      );
   }
}

internal sealed record ActivitySearchResultOutput(
   ActivitySearchEntity Entity,
   IReadOnlyCollection<ActivityProposalOutput> Proposals,
   string? RawContent,
   string? RawResponse
)
{
   public static ActivitySearchResultOutput From(
      ActivitySearchResult result,
      bool includeRaw
   )
   {
      return new ActivitySearchResultOutput(
         result.Entity,
         result.Proposals.Select(proposal => ActivityProposalOutput.From(
            proposal,
            includeRaw
         )).ToList(),
         includeRaw ? result.RawContent : null,
         includeRaw ? result.RawResponse : null
      );
   }
}

internal sealed record ActivityProposalOutput(
   ActivityProposalId Id,
   ActivityProposalProducerType ProducerType,
   string? Producer,
   Source Source,
   ExternalEntityId? ExternalId,
   string Fingerprint,
   string Title,
   string? Description,
   string? RawContent,
   ActivityType Type,
   ImportedSport Sport,
   string? Context,
   ActivityTime Time,
   IReadOnlyCollection<ActivityProposalEntityLink> EntityLinks,
   IReadOnlyCollection<ActivityProposalEvidence> Evidence,
   decimal? Confidence,
   ActivityProposalStatus Status,
   ActivityProposalRejectReason? RejectReason,
   string? RejectComment,
   ActivityProposalGroupId? GroupId,
   ActivityId? ActivityId
)
{
   public static ActivityProposalOutput From(
      ActivityProposal proposal,
      bool includeRaw
   )
   {
      return new ActivityProposalOutput(
         proposal.Id,
         proposal.ProducerType,
         proposal.Producer,
         proposal.Source,
         proposal.ExternalId,
         proposal.Fingerprint,
         proposal.Title,
         proposal.Description,
         includeRaw ? proposal.RawContent : null,
         proposal.Type,
         proposal.Sport,
         proposal.Context,
         proposal.Time,
         proposal.EntityLinks,
         proposal.Evidence,
         proposal.Confidence,
         proposal.Status,
         proposal.RejectReason,
         proposal.RejectComment,
         proposal.GroupId,
         proposal.ActivityId
      );
   }
}
