using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Dashboard;

public class IndexModel(
   DashboardRepository repository,
   TodoRepository todoRepository
) : PageModel
{
   public const string ParticipantMissingPersonDataLabel =
      "Participant missing birth date or formative club";

   public AdminDashboardSnapshot? Dashboard { get; private set; }

   public string? LoadError { get; private set; }

   public bool AiNeedsAttention =>
      Dashboard is not null
      && Dashboard.AiHealth.FailedLast25HoursCount > 0;

   public bool ImportNeedsAttention =>
      Dashboard is not null
      && (
         Dashboard.ImportHealth is null
         || !string.Equals(
            Dashboard.ImportHealth.Status,
            BroadcastImportRunStatus.Completed.ToString(),
            StringComparison.Ordinal
         )
      );

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Dashboard = await repository.GetAsync(
            DateTimeOffset.UtcNow,
            cancellationToken
         );
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   public async Task<IActionResult> OnPostCompleteTodoAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      await todoRepository.MarkDoneAsync(id, cancellationToken);
      return RedirectToPage();
   }

   public static IReadOnlyList<string> GetIssueLabels(
      DashboardActivityIssue issue
   )
   {
      var labels = new List<string>();

      if(issue.IsDraft)
      {
         labels.Add("Draft");
      }

      if(issue.IsMissingDescription)
      {
         labels.Add("Missing description");
      }

      if(issue.HasNoParticipants)
      {
         labels.Add("No participants");
      }

      if(issue.HasNoGroup)
      {
         labels.Add("No group");
      }

      if(issue.HasNoRelatedSource)
      {
         labels.Add(
            issue.HasNoGroup
               ? "No source"
               : "No source in group"
         );
      }

      if(issue.HasMissingParticipantStartTime)
      {
         labels.Add("Missing participant start times");
      }

      if(issue.HasParticipantMissingPersonData)
      {
         labels.Add(ParticipantMissingPersonDataLabel);
      }

      return labels;
   }

   public static string GetDateRowClass(DashboardDateSummary date)
   {
      if(
         date.DraftActivityCount > 0
         || date.UnreviewedBroadcastCount > 0
      )
      {
         return "dashboard-attention-row";
      }

      if(date.VisibleBroadcastCount > 0)
      {
         return
            "dashboard-attention-row dashboard-attention-row-light";
      }

      return string.Empty;
   }

   public Dictionary<string, string?> GetActivityDateRouteValues(
      DateOnly date
   )
   {
      return new Dictionary<string, string?>
      {
         [RouteKeys.Date] = date.ToString(DateDisplay.DateOnlyFormat),
         [RouteKeys.Status] = ActivityListStatusIds.All
      };
   }

   public Dictionary<string, string?> GetBroadcastDateRouteValues(
      DateOnly date
   )
   {
      return new Dictionary<string, string?>
      {
         [RouteKeys.Date] = date.ToString(DateDisplay.DateOnlyFormat)
      };
   }

   public Dictionary<string, string?> GetFailedRunRouteValues()
   {
      return new Dictionary<string, string?>
      {
         [RouteKeys.Date] = SportDay.GetLocalDate(
            DateTimeOffset.UtcNow
         ).ToString(DateDisplay.DateOnlyFormat),
         [RouteKeys.Status] = AiJobRunStatusIds.Failed
      };
   }
}
