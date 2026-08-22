using Microsoft.AspNetCore.WebUtilities;
using SESport.Core.Broadcast;

namespace SESport.Web.Pages.Admin.Broadcasts;

public sealed record BroadcastParticipationRunsViewModel(
   Guid BroadcastId,
   string? OrganizationSportName,
   string? ActivityUrlBase,
   string? CheckParticipationUrl,
   string? RunFieldUrl,
   string? ParticipantCreateUrl,
   string SearchUrlBase,
   IReadOnlyList<BroadcastParticipationCheckDisplay> Checks,
   bool IsOpen,
   bool IsPending,
   bool ClearParticipants
)
{
   public BroadcastParticipationCheckDisplay? LatestCheck =>
      Checks.FirstOrDefault();

   public BroadcastParticipationCheckDisplay? SummaryCheck => Checks
      .OrderBy(check => GetSummaryPriority(check.Participation))
      .FirstOrDefault();

   public string? ParticipationRunId => LatestCheck?.RunId.ToString();

   public string? ParticipationStatusId => LatestCheck?.StatusId;

   public bool IsFinal => !IsPending && LatestCheck is not null &&
      !string.Equals(
         LatestCheck.StatusId,
         "running",
         StringComparison.OrdinalIgnoreCase
      ) && !string.Equals(
         LatestCheck.StatusId,
         "pending",
         StringComparison.OrdinalIgnoreCase
      );

   public string ActivityUrl(Guid? runId)
   {
      if(string.IsNullOrWhiteSpace(ActivityUrlBase))
      {
         return string.Empty;
      }

      var url = ActivityUrlBase!;

      if(runId is not null && runId != Guid.Empty)
      {
         url = QueryHelpers.AddQueryString(
            url,
            RouteKeys.ParticipationRunId,
            runId.Value.ToString()
         );
      }

      if(ClearParticipants)
      {
         url = QueryHelpers.AddQueryString(
            url,
            RouteKeys.ClearParticipants,
            "true"
         );
      }

      return url;
   }

   public string SummaryText
   {
      get
      {
         var check = SummaryCheck;

         if(check is null)
         {
            return IsPending ? "Queued" : "Not checked yet";
         }

         var participation = check.Participation?.Trim() ?? string.Empty;

         if(string.Equals(
               participation,
               "yes",
               StringComparison.OrdinalIgnoreCase
            ))
         {
            return $"YES: {check.Participants.Count}";
         }

         return !string.IsNullOrWhiteSpace(check.SummaryText)
            ? check.SummaryText
            : FormatStatus(check.StatusId);
      }
   }

   public string SummaryBadgeClass
   {
      get
      {
         var participation = SummaryCheck?.Participation?.Trim();

         return participation?.ToLowerInvariant() switch
         {
            "yes" => "tool-trace-badge tool-trace-badge-result",
            "no" => "tool-trace-badge tool-trace-badge-temperature",
            "unknown" => "tool-trace-badge tool-trace-badge-count",
            _ => string.Empty
         };
      }
   }

   public static string FormatStatus(string? statusId) =>
      statusId?.Trim().ToLowerInvariant() switch
      {
         "running" => "Running",
         "pending" => "Queued",
         "completed" => "Completed",
         "failed" => "Failed",
         _ when !string.IsNullOrWhiteSpace(statusId) => statusId.Trim(),
         _ => "Not checked yet"
      };

   private static int GetSummaryPriority(string? participation) =>
      participation?.Trim().ToLowerInvariant() switch
      {
         "yes" => 0,
         "no" => 1,
         "unknown" => 2,
         _ => int.MaxValue
      };
}
