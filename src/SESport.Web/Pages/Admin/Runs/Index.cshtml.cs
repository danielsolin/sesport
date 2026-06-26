using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.AI.Models;
using SESport.AI.Persistence;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Runs;

public class IndexModel(
   AiAdminRepository adminRepository,
   AiRepository repository
) : PageModel
{
   public IReadOnlyList<AiRunListItem> Runs { get; private set; } = [];

   public IReadOnlyList<AiJobListItem> Jobs { get; private set; } = [];

   public IReadOnlyList<SelectListItem> JobOptions { get; private set; } = [];

   public IReadOnlyList<string> ExecutionEnvironmentValues { get; private set; }
      = [];

   public IReadOnlyList<SelectListItem> StatusOptions { get; private set; } =
      [];

   [BindProperty(SupportsGet = true, Name = RouteKeys.Date)]
   public DateOnly? Date { get; set; }

   [BindProperty(SupportsGet = true, Name = RouteKeys.JobId)]
   public string? JobId { get; set; }

   [BindProperty(SupportsGet = true, Name = RouteKeys.Status)]
   public string[]? StatusIds { get; set; } =
      AiJobRunStatusIds.DefaultRunListStatuses;

   public string DateText => Date is null
      ? string.Empty
      : DateDisplay.Format(Date.Value);

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Jobs = await adminRepository.GetJobsAsync(cancellationToken);
         JobOptions =
         [
            new SelectListItem(
               "All jobs",
               string.Empty,
               string.IsNullOrWhiteSpace(JobId)
            ),
            .. Jobs.Select(job =>
               new SelectListItem(
                  job.Label,
                  job.Id,
                  string.Equals(job.Id, JobId, StringComparison.Ordinal)
               )
            )
         ];
         ExecutionEnvironmentValues =
            await repository.GetExecutionEnvironmentOptionsAsync(
               cancellationToken
            );
         StatusIds = NormalizeStatusIds(StatusIds);
         StatusOptions = BuildStatusOptions(StatusIds);
         Runs = await repository.GetRunsAsync(
            Date,
            JobId,
            StatusIds,
            cancellationToken
         );
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   public async Task<IActionResult> OnPostDeleteAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteRunAsync(id, cancellationToken);

      return RedirectToPage(
         "./Index",
         GetFilterRouteValues()
      );
   }

   public IReadOnlyList<SelectListItem> GetExecutionEnvironmentOptions(
      string? selectedExecutionEnvironment
   )
   {
      return DetailsModel.BuildExecutionEnvironmentOptions(
         ExecutionEnvironmentValues,
         selectedExecutionEnvironment,
         SESport.AI.ExecutionEnvironment.Current,
         includeUnsetOption: false
      );
   }

   public Dictionary<string, string> GetFilterRouteValues()
   {
      var routeValues = new Dictionary<string, string>();

      if(Date is not null)
      {
         routeValues[RouteKeys.Date] = DateDisplay.Format(Date.Value);
      }

      if(!string.IsNullOrWhiteSpace(JobId))
      {
         routeValues[RouteKeys.JobId] = JobId;
      }

      AddStatusRouteValues(routeValues, StatusIds);
      return routeValues;
   }

   public Dictionary<string, string> GetDetailsRouteValues(Guid id)
   {
      var routeValues = GetFilterRouteValues();
      routeValues["id"] = id.ToString();
      return routeValues;
   }

   private static IReadOnlyList<SelectListItem> BuildStatusOptions(
      IReadOnlyCollection<string> selectedStatusIds
   )
   {
      return
      [
         new SelectListItem(
            "Running",
            AiJobRunStatusIds.Running,
            selectedStatusIds.Any(statusId =>
               string.Equals(
                  statusId,
                  AiJobRunStatusIds.Running,
                  StringComparison.OrdinalIgnoreCase
               ))
         ),
         new SelectListItem(
            "Pending",
            AiJobRunStatusIds.Pending,
            selectedStatusIds.Any(statusId =>
               string.Equals(
                  statusId,
                  AiJobRunStatusIds.Pending,
                  StringComparison.OrdinalIgnoreCase
               ))
         ),
         new SelectListItem(
            "Completed",
            AiJobRunStatusIds.Completed,
            selectedStatusIds.Any(statusId =>
               string.Equals(
                  statusId,
                  AiJobRunStatusIds.Completed,
                  StringComparison.OrdinalIgnoreCase
               ))
         ),
         new SelectListItem(
            "Failed",
            AiJobRunStatusIds.Failed,
            selectedStatusIds.Any(statusId =>
               string.Equals(
                  statusId,
                  AiJobRunStatusIds.Failed,
                  StringComparison.OrdinalIgnoreCase
               ))
         ),
         new SelectListItem(
            "Archived",
            AiJobRunStatusIds.Archived,
            selectedStatusIds.Any(statusId =>
               string.Equals(
                  statusId,
                  AiJobRunStatusIds.Archived,
                  StringComparison.OrdinalIgnoreCase
               ))
         )
      ];
   }

   private static string[] NormalizeStatusIds(
      IReadOnlyCollection<string>? statusIds
   )
   {
      var normalizedStatusIds = statusIds?
         .Where(statusId => !string.IsNullOrWhiteSpace(statusId))
         .Select(statusId => statusId.Trim())
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList()
         ?? [];

      return normalizedStatusIds.Count > 0
         ? normalizedStatusIds.ToArray()
         : AiJobRunStatusIds.DefaultRunListStatuses;
   }

   private static void AddStatusRouteValues(
      IDictionary<string, string> routeValues,
      IReadOnlyList<string>? statusIds
   )
   {
      var normalizedStatusIds = NormalizeStatusIds(statusIds);

      var index = 0;
      foreach(var statusId in normalizedStatusIds)
      {
         routeValues[$"{RouteKeys.Status}[{index}]"] = statusId;
         index++;
      }
   }
}
