using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.Core.Formatting;
using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Entities;

public class IndexModel(
   AdminRepository repository,
   EntityDatePreferenceStore datePreferenceStore
) : PageModel
{
   public const string FilterCookieName = "sesport.admin.entities.filter";
   public const string TypeFilterCookieName =
      "sesport.admin.entities.type-filter";
   public const string SportFilterCookieName =
      "sesport.admin.entities.sport-filter";
   public const string NameSortColumn = "Name";
   public const string TypeSortColumn = "Type";
   public const string SportSortColumn = "Sport";
   public const string WatchSortColumn = "Watch";
   public const string CountrySortColumn = "Country";
   public const string RelatedSortColumn = "Related";

   public IReadOnlyList<EntityListItem> Entities { get; private set; } = [];

   public IReadOnlyList<ReferenceRow> WatchPriorities
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<ReferenceRow> EntityTypes
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<ReferenceRow> Sports
   {
      get;
      private set;
   } = [];

   public string SortColumn { get; private set; } = NameSortColumn;

   public bool SortAsc { get; private set; } = true;

   public string Filter { get; private set; } = string.Empty;

   public bool HasFilter => !string.IsNullOrWhiteSpace(Filter);

   [BindProperty(SupportsGet = true, Name = RouteKeys.Date)]
   public DateOnly? Date { get; set; }

   public string? DateText => DateDisplay.Format(Date);

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(
      string? sortColumn,
      bool sortAsc = true,
      CancellationToken cancellationToken = default
   )
   {
      SortColumn = NormalizeSortColumn(sortColumn);
      SortAsc = sortAsc;
      Date = datePreferenceStore.ResolveOptionalDate(HttpContext, Date);
      Filter = Request.Cookies.TryGetValue(FilterCookieName, out var cookie)
         ? cookie?.Trim() ?? string.Empty
         : string.Empty;

      try
      {
         WatchPriorities = await repository.GetReferenceRowsAsync(
            "entity-watch-priorities",
            cancellationToken
         );
         EntityTypes = await repository.GetReferenceRowsAsync(
            "entity-types",
            cancellationToken
         );
         Sports = await repository.GetReferenceRowsAsync(
            "sports",
            cancellationToken
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
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

   public async Task<IActionResult> OnPostDeleteAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteEntityAsync(id, cancellationToken);
      return RedirectToPage("./Index");
   }

   public IReadOnlyList<SelectListItem> GetWatchPriorityOptions(
      string? selectedWatchPriorityId
   )
   {
      return WatchPriorities
         .Select(priority => new SelectListItem(
            priority.Label,
            priority.Id,
            string.Equals(
               priority.Id,
               selectedWatchPriorityId,
               StringComparison.Ordinal
            )
         ))
         .ToList();
   }

   public IReadOnlyList<SelectListItem> GetEntityTypeOptions()
   {
      return EntityTypes
         .Select(type => new SelectListItem(type.Label, type.Id))
         .ToList();
   }

   public IReadOnlyList<SelectListItem> GetSportOptions()
   {
      return Sports
         .Select(sport => new SelectListItem(sport.Label, sport.Id))
         .ToList();
   }

   internal static string NormalizeSortColumn(string? sortColumn) =>
      sortColumn switch
      {
         TypeSortColumn => TypeSortColumn,
         SportSortColumn => SportSortColumn,
         WatchSortColumn => WatchSortColumn,
         CountrySortColumn => CountrySortColumn,
         RelatedSortColumn => RelatedSortColumn,
         _ => NameSortColumn
      };

   internal static IReadOnlyList<EntityListItem> SortEntities(
      IEnumerable<EntityListItem> entities,
      string sortColumn,
      bool sortAsc
   )
   {
      return sortColumn switch
      {
         TypeSortColumn => OrderByDirection(
            entities,
            entity => entity.EntityType,
            sortAsc
         ),
         SportSortColumn => OrderByDirection(
            entities,
            entity => entity.Sport,
            sortAsc
         ),
         WatchSortColumn => OrderByDirection(
            entities,
            entity => entity.WatchPriority,
            sortAsc
         ),
         CountrySortColumn => OrderByDirection(
            entities,
            entity => entity.Country,
            sortAsc
         ),
         RelatedSortColumn => OrderByDirection(
            entities,
            entity => entity.RelatedEntityNames,
            sortAsc
         ),
         _ => OrderByDirection(entities, entity => entity.Name, sortAsc)
      };
   }

   private static IReadOnlyList<EntityListItem> OrderByDirection(
      IEnumerable<EntityListItem> entities,
      Func<EntityListItem, string> keySelector,
      bool sortAsc
   )
   {
      var sortedEntities = sortAsc
         ? entities.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase)
         : entities.OrderByDescending(
            keySelector,
            StringComparer.OrdinalIgnoreCase
         );

      return sortedEntities
         .ThenBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   private static IReadOnlyList<EntityListItem> OrderByDirection(
      IEnumerable<EntityListItem> entities,
      Func<EntityListItem, int> keySelector,
      bool sortAsc
   )
   {
      var sortedEntities = sortAsc
         ? entities.OrderBy(keySelector)
         : entities.OrderByDescending(keySelector);

      return sortedEntities
         .ThenBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
         .ToList();
   }
}
