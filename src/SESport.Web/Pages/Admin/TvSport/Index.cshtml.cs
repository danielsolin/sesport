using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.TvSport;

public class IndexModel(TvSportRepository repository) : PageModel
{
   public const string ChannelSortColumn = "Channel";
   public const string TimeSortColumn = "Time";
   public const string BroadcastSortColumn = "Broadcast";
   public const string CategoriesSortColumn = "Categories";

   [BindProperty(SupportsGet = true, Name = "date")]
   public DateOnly? Date { get; set; }

   [BindProperty(SupportsGet = true, Name = "hideReplays")]
   public bool HideReplays { get; set; }

   [BindProperty(SupportsGet = true, Name = "showHidden")]
   public bool ShowHidden { get; set; }

   [BindProperty(SupportsGet = true)]
   public List<string> SelectedSports { get; set; } = [];

   [BindProperty(SupportsGet = true)]
   public string SortColumn { get; set; } = TimeSortColumn;

   [BindProperty(SupportsGet = true)]
   public bool SortAsc { get; set; } = true;

   public string DateText => SelectedDate.ToString("yyyy-MM-dd");

   public DateOnly SelectedDate { get; private set; }

   public IReadOnlyList<TvSportBroadcastListItem> Broadcasts
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<SelectListItem> SportOptions
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      SortColumn = NormalizeSortColumn(SortColumn);
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

   public async Task<IActionResult> OnPostHideAsync(
      Guid id,
      bool isHidden,
      CancellationToken cancellationToken
   )
   {
      if(isHidden)
      {
         await repository.ShowAsync(id, cancellationToken);
      }
      else
      {
         await repository.HideAsync(id, cancellationToken);
      }

      SortColumn = NormalizeSortColumn(SortColumn);

      if(WantsJsonResponse())
      {
         return new JsonResult(new { hidden = !isHidden });
      }

      var selectedDate = Date ?? DateOnly.FromDateTime(DateTime.Now.AddDays(1));
      var routeValues = new Dictionary<string, object?>
      {
         ["date"] = selectedDate.ToString("yyyy-MM-dd")
      };

      if(HideReplays)
      {
         routeValues["hideReplays"] = "true";
      }

      if(ShowHidden)
      {
         routeValues["showHidden"] = "true";
      }

      routeValues["sortColumn"] = SortColumn;
      routeValues["sortAsc"] = SortAsc;

      var normalizedSports = NormalizeSelectedSports(SelectedSports);

      for(var index = 0; index < normalizedSports.Count; index++)
      {
         routeValues[$"SelectedSports[{index}]"] = normalizedSports[index];
      }

      return RedirectToPage(routeValues);
   }

   public async Task<IActionResult> OnPostGenerateActivityAsync(
      List<Guid> tvSportBroadcastIds,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      var broadcastIds = NormalizeBroadcastIds(tvSportBroadcastIds);

      if(broadcastIds.Count == 0)
      {
         SortColumn = NormalizeSortColumn(SortColumn);
         await LoadAsync(cancellationToken);

         return Page();
      }

      var routeValues = new Dictionary<string, object?>();

      for(var index = 0; index < broadcastIds.Count; index++)
      {
         routeValues[$"tvSportBroadcastIds[{index}]"] =
            broadcastIds[index];
      }

      if(Url.IsLocalUrl(returnUrl))
      {
         routeValues["returnUrl"] = returnUrl;
      }

      return RedirectToPage("/Admin/Activities/Edit", routeValues);
   }

   private bool WantsJsonResponse()
   {
      return Request.Headers.Accept.Any(value =>
         value?.Contains(
            "application/json",
            StringComparison.OrdinalIgnoreCase
         ) == true
      );
   }

   private async Task LoadAsync(CancellationToken cancellationToken)
   {
      SelectedDate = Date ?? DateOnly.FromDateTime(DateTime.Now.AddDays(0));

      try
      {
         var normalizedSports = NormalizeSelectedSports(SelectedSports);
         SelectedSports = normalizedSports.Count == 0
            ? [string.Empty]
            : normalizedSports;
         var categories = await repository.GetCategoriesForDateAsync(
            SelectedDate,
            HideReplays,
            ShowHidden,
            cancellationToken
         );
         SportOptions =
         [
            new SelectListItem(
               "Alla",
               string.Empty,
               normalizedSports.Count == 0
            ),
            .. categories
            .Select(category => new TvSportCategoryOption(
               category,
               normalizedSports.Contains(category)
            ))
            .Select(option => new SelectListItem(
               option.Name,
               option.Name,
               option.IsSelected
            ))
         ];
         Broadcasts = await repository.GetByDateAsync(
            SelectedDate,
            HideReplays,
            ShowHidden,
            normalizedSports,
            cancellationToken
         );
         Broadcasts = SortBroadcasts(Broadcasts, SortColumn, SortAsc);
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   private Dictionary<string, string?> GetCurrentRouteValues()
   {
      var routeValues = new Dictionary<string, string?>
      {
         ["date"] = DateText
      };

      if(HideReplays)
      {
         routeValues["hideReplays"] = "true";
      }

      if(ShowHidden)
      {
         routeValues["showHidden"] = "true";
      }

      var normalizedSports = NormalizeSelectedSports(SelectedSports);

      for(var index = 0; index < normalizedSports.Count; index++)
      {
         routeValues[$"SelectedSports[{index}]"] = normalizedSports[index];
      }

      return routeValues;
   }

   private static string NormalizeSortColumn(string? sortColumn) =>
      sortColumn switch
      {
         ChannelSortColumn => ChannelSortColumn,
         BroadcastSortColumn => BroadcastSortColumn,
         CategoriesSortColumn => CategoriesSortColumn,
         _ => TimeSortColumn
      };

   private static IReadOnlyList<TvSportBroadcastListItem> SortBroadcasts(
      IEnumerable<TvSportBroadcastListItem> broadcasts,
      string sortColumn,
      bool sortAsc
   )
   {
      return sortColumn switch
      {
         ChannelSortColumn => OrderByDirection(
            broadcasts,
            broadcast => broadcast.ChannelName,
            sortAsc
         ),
         BroadcastSortColumn => OrderByDirection(
            broadcasts,
            broadcast => broadcast.Title,
            sortAsc
         ),
         CategoriesSortColumn => OrderByDirection(
            broadcasts,
            broadcast => broadcast.Categories,
            sortAsc
         ),
         _ => OrderByDirection(
            broadcasts,
            broadcast => broadcast.TimeText,
            sortAsc
         )
      };
   }

   private static IReadOnlyList<TvSportBroadcastListItem> OrderByDirection(
      IEnumerable<TvSportBroadcastListItem> broadcasts,
      Func<TvSportBroadcastListItem, string> keySelector,
      bool sortAsc
   )
   {
      var sortedBroadcasts = sortAsc
         ? broadcasts.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase)
         : broadcasts.OrderByDescending(
            keySelector,
            StringComparer.OrdinalIgnoreCase
         );

      return sortedBroadcasts
         .ThenBy(broadcast => broadcast.TimeText, StringComparer.Ordinal)
         .ThenBy(broadcast => broadcast.ChannelName)
         .ThenBy(broadcast => broadcast.Title)
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

   private static List<Guid> NormalizeBroadcastIds(IEnumerable<Guid> ids)
   {
      return ids
         .Where(id => id != Guid.Empty)
         .Distinct()
         .ToList();
   }
}
