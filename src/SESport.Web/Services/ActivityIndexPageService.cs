using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.Core.Domain;
using SESport.Data;

namespace SESport.Web.Services;

public sealed class ActivityIndexPageService(
   ActivityRepository repository,
   AdminDatePreferenceStore datePreferenceStore,
   ILogger<ActivityIndexPageService> logger
)
{
   private const string TimeSortColumn = "Time";
   private const string GroupSortColumn = "Group";
   private const string ActivitySortColumn = "Activity";
   private const string EntitiesSortColumn = "Entities";
   private const string StatusSortColumn = "Status";

   public async Task<ActivityIndexViewModel> BuildAsync(
      HttpContext httpContext,
      DateOnly? date,
      string? status,
      IEnumerable<string> selectedSports,
      string? sortColumn,
      bool sortAsc,
      CancellationToken cancellationToken
   )
   {
      var normalizedSortColumn = NormalizeSortColumn(sortColumn);
      var normalizedStatus = NormalizeStatus(status)
         ?? ActivityListStatusIds.All;
      var selectedDate = ResolveSelectedDate(httpContext, date, status);
      var normalizedSports = NormalizeSelectedSports(selectedSports);

      try
      {
         var sports = await repository.GetSportOptionsAsync(cancellationToken);
         IReadOnlyList<SelectListItem> sportOptions =
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

         var activities = await repository.GetActivitiesAsync(
            selectedDate,
            normalizedStatus,
            normalizedSports,
            cancellationToken
         );

         return new ActivityIndexViewModel(
            selectedDate,
            normalizedStatus,
            normalizedSortColumn,
            sortAsc,
            normalizedSports.Count == 0
               ? new List<string> { string.Empty }
               : normalizedSports,
            SortActivities(activities, normalizedSortColumn, sortAsc),
            sportOptions,
            null
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         logger.LogError(
            exception,
            "Unable to build the activity index page."
         );

         return new ActivityIndexViewModel(
            selectedDate,
            normalizedStatus,
            normalizedSortColumn,
            sortAsc,
            normalizedSports.Count == 0
               ? new List<string> { string.Empty }
               : normalizedSports,
            [],
            [],
            PageModelErrorExtensions.UnexpectedErrorMessage
         );
      }
   }

   public DateOnly GetRouteDate(DateOnly? date, string? status)
   {
      if(date is not null)
      {
         return date.Value;
      }

      return status switch
      {
         ActivityListStatusIds.Tomorrow =>
            SportDay.Tomorrow(DateTimeOffset.UtcNow).StartDate,
         _ => SportDay.Today(DateTimeOffset.UtcNow).StartDate
      };
   }

   public string NormalizeStatusOrDefault(string? status)
   {
      return NormalizeStatus(status) ?? ActivityListStatusIds.All;
   }

   public string NormalizeSortColumnOrDefault(string? sortColumn)
   {
      return NormalizeSortColumn(sortColumn);
   }

   public List<string> NormalizeSelectedSportsOrDefault(
      IEnumerable<string> values
   )
   {
      var normalizedSports = NormalizeSelectedSports(values);
      return normalizedSports.Count == 0 ? [string.Empty] : normalizedSports;
   }

   public DateOnly ResolveSelectedDate(
      HttpContext httpContext,
      DateOnly? date,
      string? status
   )
   {
      if(date is not null)
      {
         return date.Value;
      }

      if(status == ActivityListStatusIds.Today)
      {
         return SportDay.Today(DateTimeOffset.UtcNow).StartDate;
      }

      if(status == ActivityListStatusIds.Tomorrow)
      {
         return SportDay.Tomorrow(DateTimeOffset.UtcNow).StartDate;
      }

      return datePreferenceStore.ResolveDate(httpContext, date);
   }

   public string? NormalizeStatus(string? status)
   {
      if(status is
         ActivityPublicationStatusIds.Draft or
         ActivityPublicationStatusIds.Published)
      {
         return status;
      }

      return status is ActivityListStatusIds.All or "" or null
         ? ActivityListStatusIds.All
         : null;
   }

   public string NormalizeSortColumn(string? sortColumn)
   {
      return sortColumn switch
      {
         GroupSortColumn => GroupSortColumn,
         ActivitySortColumn => ActivitySortColumn,
         EntitiesSortColumn => EntitiesSortColumn,
         StatusSortColumn => StatusSortColumn,
         _ => TimeSortColumn
      };
   }

   public List<string> NormalizeSelectedSports(
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

   private static IReadOnlyList<ActivityListItem> SortActivities(
      IEnumerable<ActivityListItem> activities,
      string sortColumn,
      bool sortAsc
   )
   {
      return sortColumn switch
      {
         GroupSortColumn => OrderByDirection(
            activities,
            activity => activity.ActivityGroupTitle ?? string.Empty,
            sortAsc
         ),
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
         _ => OrderByStartTime(
            activities,
            sortAsc
         )
      };
   }

   private static IReadOnlyList<ActivityListItem> OrderByStartTime(
      IEnumerable<ActivityListItem> activities,
      bool sortAsc
   )
   {
      var activitiesWithUntimedLast = activities
         .OrderBy(activity => activity.StartsAt is null);
      var sortedActivities = sortAsc
         ? activitiesWithUntimedLast.ThenBy(activity => activity.StartsAt)
         : activitiesWithUntimedLast.ThenByDescending(
            activity => activity.StartsAt
         );

      return sortedActivities
         .ThenBy(activity => activity.Title, StringComparer.OrdinalIgnoreCase)
         .ToList();
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

public sealed record ActivityIndexViewModel(
   DateOnly SelectedDate,
   string Status,
   string SortColumn,
   bool SortAsc,
   IReadOnlyList<string> SelectedSports,
   IReadOnlyList<ActivityListItem> Activities,
   IReadOnlyList<SelectListItem> SportOptions,
   string? LoadError
);
