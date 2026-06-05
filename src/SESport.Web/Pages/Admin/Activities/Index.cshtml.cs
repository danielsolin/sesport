using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Activities;

public class IndexModel(ActivityRepository repository) : PageModel
{
   public const string TodayStatus = "Today";
   public const string AllStatus = "All";
   public const string DraftStatus = "Draft";
   public const string PublishedStatus = "Published";

   public const string TimeSortColumn = "Time";
   public const string ActivitySortColumn = "Activity";
   public const string EntitiesSortColumn = "Entities";
   public const string StatusSortColumn = "Status";

   [BindProperty(SupportsGet = true, Name = "status")]
   public string? Status { get; set; } = "Today";

   public string SortColumn { get; private set; } = TimeSortColumn;

   public bool SortAsc { get; private set; } = true;

   public IReadOnlyList<ActivityListItem> Activities { get; private set; } =
      [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(
      string? sortColumn,
      bool sortAsc = true,
      CancellationToken cancellationToken = default
   )
   {
      Status = NormalizeStatus(Status) ?? DraftStatus;
      SortColumn = NormalizeSortColumn(sortColumn);
      SortAsc = sortAsc;

      try
      {
         var activities = Status switch
         {
            TodayStatus => await repository.GetTodaysAsync(cancellationToken),
            DraftStatus => await repository.GetDraftsAsync(cancellationToken),
            PublishedStatus => await repository.GetPublishedAsync(
               cancellationToken
            ),
            _ => await repository.GetAllAsync(cancellationToken)
         };
         Activities = SortActivities(activities, SortColumn, SortAsc);
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
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

   public async Task<IActionResult> OnPostDeleteAsync(
      Guid id,
      string? status,
      string? sortColumn,
      bool? sortAsc,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteAsync(id, cancellationToken);
      var routeValues = GetRedirectRouteValues(
         status,
         sortColumn,
         sortAsc ?? true
      );

      return RedirectToPage("./Index", routeValues);
   }

   private static Dictionary<string, object?> GetRedirectRouteValues(
      string? status,
      string? sortColumn,
      bool sortAsc
   )
   {
      var routeValues = new Dictionary<string, object?>
      {
         ["status"] = NormalizeStatus(status) ?? DraftStatus,
         ["sortColumn"] = NormalizeSortColumn(sortColumn),
         ["sortAsc"] = sortAsc
      };

      return routeValues;
   }

   private Dictionary<string, string?> GetCurrentRouteValues()
   {
      var routeValues = new Dictionary<string, string?>
      {
         ["status"] = Status ?? DraftStatus
      };

      return routeValues;
   }

   private static string? NormalizeStatus(string? status)
   {
      return status switch
      {
         TodayStatus => TodayStatus,
         DraftStatus => DraftStatus,
         PublishedStatus => PublishedStatus,
         AllStatus or "" => AllStatus,
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
            activity => activity.EntitySummary,
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
}
