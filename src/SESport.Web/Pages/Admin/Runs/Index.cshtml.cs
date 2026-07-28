using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.Core.AI;
using SESport.Core.Formatting;
using SESport.Data.Repositories;

namespace SESport.Web.Pages.Admin.Runs;

public class IndexModel(
   AiAdminRepository adminRepository,
   AiRepository repository,
   RunDatePreferenceStore datePreferenceStore
) : PageModel
{
   public const string JobFilterCookieName =
      "sesport.admin.runs.job";
   public const string StatusFilterCookieName =
      "sesport.admin.runs.status";
   private static readonly string[] ValidStatusIds =
   [
      AiJobRunStatusIds.Running,
      AiJobRunStatusIds.Pending,
      AiJobRunStatusIds.Completed,
      AiJobRunStatusIds.Failed,
      AiJobRunStatusIds.Archived
   ];

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
         Date = datePreferenceStore.ResolveOptionalDate(HttpContext, Date);
         JobId = ResolveJobId();
         StatusIds = ResolveStatusIds();

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
         WriteFilterCookies();
         StatusOptions = BuildStatusOptions(StatusIds);
         if(HasAnyFilter())
         {
            Runs = await repository.GetRunsAsync(
               Date,
               JobId,
               StatusIds,
               cancellationToken
            );
         }
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
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
         SESport.Core.Configuration.ExecutionEnvironment.Current,
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

   public static string FormatRunTargetLabel(AiRunListItem run)
   {
      var targetLabel = AiJobIds.GetTargetType(run.JobId) ==
         AiJobTargetType.Broadcast
         ? "B"
         : AiJobIds.GetTargetType(run.JobId) == AiJobTargetType.Person
            ? "P"
            : "A";
      var dateText = DateDisplay.Format(run.EventDate);

      return string.IsNullOrWhiteSpace(dateText)
         ? targetLabel
         : $"{targetLabel} {dateText}";
   }

   private string? ResolveJobId()
   {
      if(Request.Query.ContainsKey(RouteKeys.JobId))
      {
         return string.IsNullOrWhiteSpace(JobId) ? null : JobId.Trim();
      }

      return Request.Cookies.TryGetValue(
         JobFilterCookieName,
         out var cookieValue
      )
         ? string.IsNullOrWhiteSpace(cookieValue) ? null : cookieValue.Trim()
         : JobId;
   }

   private string[] ResolveStatusIds()
   {
      if(Request.Query.ContainsKey(RouteKeys.Status))
      {
         return NormalizeStatusIds(StatusIds);
      }

      if(!Request.Cookies.TryGetValue(
         StatusFilterCookieName,
         out var cookieValue
      ))
      {
         return NormalizeStatusIds(StatusIds);
      }

      return NormalizeStatusIds(
         cookieValue.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries |
               StringSplitOptions.TrimEntries
         )
      );
   }

   private void WriteFilterCookies()
   {
      WriteCookie(JobFilterCookieName, JobId ?? string.Empty);
      WriteCookie(
         StatusFilterCookieName,
         string.Join(",", NormalizeStatusIds(StatusIds))
      );
   }

   private bool HasAnyFilter()
   {
      return Date is not null ||
         !string.IsNullOrWhiteSpace(JobId) ||
         StatusIds is { Length: > 0 };
   }

   private void WriteCookie(string name, string value)
   {
      Response.Cookies.Append(
         name,
         value,
         new CookieOptions
         {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            IsEssential = true,
            Path = "/Admin",
            SameSite = SameSiteMode.Lax
         }
      );
   }

   public Dictionary<string, string> GetDetailsRouteValues(Guid id)
   {
      var routeValues = GetFilterRouteValues();
      routeValues["id"] = id.ToString();
      return routeValues;
   }

   public Dictionary<string, string> GetDeleteRouteValues(Guid id)
   {
      return GetDetailsRouteValues(id);
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
         .Where(statusId => ValidStatusIds.Any(validStatusId =>
            string.Equals(
               validStatusId,
               statusId,
               StringComparison.OrdinalIgnoreCase
            )))
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList()
         ?? [];

      return normalizedStatusIds.ToArray();
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
