using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Activities;

public class IndexModel(
   ActivityRepository repository,
   AdminDatePreferenceStore datePreferenceStore
) : PageModel
{
   public const string LegacyTodayStatus = "Today";
   public const string LegacyTomorrowStatus = "Tomorrow";
   public const string AllStatus = "All";
   public const string DraftStatus = "Draft";
   public const string PublishedStatus = "Published";

   public const string TimeSortColumn = "Time";
   public const string ActivitySortColumn = "Activity";
   public const string EntitiesSortColumn = "Entities";
   public const string StatusSortColumn = "Status";

   [BindProperty(SupportsGet = true, Name = "date")]
   public DateOnly? Date { get; set; }

   [BindProperty(SupportsGet = true, Name = "status")]
   public string? Status { get; set; } = AllStatus;

   [BindProperty(SupportsGet = true)]
   public List<string> SelectedSports { get; set; } = [];

   [BindProperty(SupportsGet = true)]
   public string SortColumn { get; set; } = TimeSortColumn;

   [BindProperty(SupportsGet = true)]
   public bool SortAsc { get; set; } = true;

   public string DateText => DateDisplay.Format(SelectedDate);

   public DateOnly SelectedDate { get; private set; }

   public IReadOnlyList<ActivityListItem> Activities { get; private set; } =
      [];

   public IReadOnlyList<SelectListItem> SportOptions { get; private set; } =
      [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      SortColumn = NormalizeSortColumn(SortColumn);
      NormalizeFilters();
      await LoadAsync(cancellationToken);
   }

   public bool GetNextSortAsc(string sortColumn) =>
      string.Equals(SortColumn, sortColumn, StringComparison.Ordinal)
         ? !SortAsc
         : true;

   public string GetSortIndicator(string sortColumn)
   {
      if(!string.Equals(SortColumn, sortColumn, StringComparison.Ordinal))
      {
         return string.Empty;
      }

      return SortAsc ? "▲" : "▼";
   }

   public Dictionary<string, string?> GetSortRouteValues(string sortColumn)
   {
      var routeValues = GetCurrentRouteValues();
      routeValues["sortColumn"] = sortColumn;
      routeValues["sortAsc"] = GetNextSortAsc(sortColumn).ToString();

      return routeValues;
   }

   public string GetReturnUrl()
   {
      var routeValues = GetCurrentRouteValues();
      routeValues["sortColumn"] = SortColumn;
      routeValues["sortAsc"] = SortAsc.ToString();

      return Url.Page("./Index", routeValues) ?? "/Admin/Activities";
   }

   public async Task<IActionResult> OnPostDeleteAsync(
      Guid id,
      DateOnly? date,
      string? status,
      string? sortColumn,
      bool? sortAsc,
      List<string>? selectedSports,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteAsync(id, cancellationToken);

      var routeValues = GetRedirectRouteValues(
         date,
         status,
         sortColumn,
         sortAsc,
         selectedSports ?? SelectedSports
      );

      return RedirectToPage("./Index", routeValues);
   }

   private async Task LoadAsync(CancellationToken cancellationToken)
   {
      SelectedDate = datePreferenceStore.ResolveDate(HttpContext, Date);

      try
      {
         var normalizedSports = NormalizeSelectedSports(SelectedSports);
         SelectedSports = normalizedSports.Count == 0
            ? [string.Empty]
            : normalizedSports;

         var sports = await repository.GetSportOptionsAsync(cancellationToken);
         SportOptions =
         [
            new SelectListItem(
               "Alla",
               string.Empty,
               normalizedSports.Count == 0
            ),
            .. sports.Select(sport => new SelectListItem(
               sport.Label,
               sport.Id,
               normalizedSports.Contains(sport.Id)
            ))
         ];

         Activities = await repository.GetActivitiesAsync(
            SelectedDate,
            Status,
            normalizedSports,
            cancellationToken
         );
         Activities = SortActivities(Activities, SortColumn, SortAsc);
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   private void NormalizeFilters()
   {
      if(Date is null)
      {
         Date = Status switch
         {
            LegacyTodayStatus =>
               SportDay.Today(DateTimeOffset.UtcNow).StartDate,
            LegacyTomorrowStatus =>
               SportDay.Tomorrow(DateTimeOffset.UtcNow).StartDate,
            _ => Date
         };
      }

      Status = NormalizeStatus(Status) ?? AllStatus;

      var normalizedSports = NormalizeSelectedSports(SelectedSports);
      SelectedSports = normalizedSports.Count == 0
         ? [string.Empty]
         : normalizedSports;
   }

   private Dictionary<string, string?> GetCurrentRouteValues()
   {
      var routeValues = new Dictionary<string, string?>
      {
         ["date"] = DateText,
         ["status"] = Status ?? AllStatus
      };

      var normalizedSports = NormalizeSelectedSports(SelectedSports);

      for(var index = 0; index < normalizedSports.Count; index++)
      {
         routeValues[$"SelectedSports[{index}]"] = normalizedSports[index];
      }

      return routeValues;
   }

   private static Dictionary<string, object?> GetRedirectRouteValues(
      DateOnly? date,
      string? status,
      string? sortColumn,
      bool? sortAsc,
      IEnumerable<string> selectedSports
   )
   {
      var routeValues = new Dictionary<string, object?>
      {
         ["date"] = DateDisplay.Format(GetRouteDate(date, status)),
         ["status"] = NormalizeStatus(status) ?? AllStatus,
         ["sortColumn"] = NormalizeSortColumn(sortColumn),
         ["sortAsc"] = sortAsc ?? true
      };

      var normalizedSports = NormalizeSelectedSports(selectedSports);

      for(var index = 0; index < normalizedSports.Count; index++)
      {
         routeValues[$"SelectedSports[{index}]"] = normalizedSports[index];
      }

      return routeValues;
   }

   private static DateOnly GetRouteDate(DateOnly? date, string? status)
   {
      if(date is not null)
      {
         return date.Value;
      }

      return status switch
      {
         LegacyTomorrowStatus =>
            SportDay.Tomorrow(DateTimeOffset.UtcNow).StartDate,
         _ => SportDay.Today(DateTimeOffset.UtcNow).StartDate
      };
   }

   private static string? NormalizeStatus(string? status)
   {
      return status switch
      {
         DraftStatus => DraftStatus,
         PublishedStatus => PublishedStatus,
         AllStatus or "" or null => AllStatus,
         _ => null
      };
   }

   private static string NormalizeSortColumn(string? sortColumn) =>
      sortColumn switch
      {
         ActivitySortColumn => ActivitySortColumn,
         EntitiesSortColumn => EntitiesSortColumn,
         StatusSortColumn => StatusSortColumn,
         _ => TimeSortColumn
      };

   private static IReadOnlyList<ActivityListItem> SortActivities(
      IEnumerable<ActivityListItem> activities,
      string sortColumn,
      bool sortAsc
   )
   {
      return sortColumn switch
      {
         ActivitySortColumn => OrderByDirection(
            activities,
            activity => activity.Title,
            sortAsc
         ),
         EntitiesSortColumn => OrderByDirection(
            activities,
            activity => activity.RelatedPersonEntities,
            sortAsc
         ),
         StatusSortColumn => OrderByDirection(
            activities,
            activity => activity.PublicationStatus,
            sortAsc
         ),
         _ => OrderByDirection(
            activities,
            activity => activity.TimeText,
            sortAsc
         )
      };
   }

   private static IReadOnlyList<ActivityListItem> OrderByDirection(
      IEnumerable<ActivityListItem> activities,
      Func<ActivityListItem, string> keySelector,
      bool sortAsc
   )
   {
      var sortedActivities = sortAsc
         ? activities.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase)
         : activities.OrderByDescending(
            keySelector,
            StringComparer.OrdinalIgnoreCase
         );

      return sortedActivities
         .ThenBy(activity => activity.TimeText, StringComparer.Ordinal)
         .ThenBy(activity => activity.Title, StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   private static List<string> NormalizeSelectedSports(
      IEnumerable<string> values
   )
   {
      return values
         .Where(value => !string.IsNullOrWhiteSpace(value))
         .Select(value => value.Trim())
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
         .ToList();
   }
}
