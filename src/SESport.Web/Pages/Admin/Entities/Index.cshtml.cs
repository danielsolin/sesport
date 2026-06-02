using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Entities;

public class IndexModel(AdminRepository repository) : PageModel
{
   public const string NameSortColumn = "Name";
   public const string TypeSortColumn = "Type";
   public const string SportSortColumn = "Sport";
   public const string WatchSortColumn = "Watch";
   public const string StabilitySortColumn = "Stability";

   public string SearchUrl = "https://www.google.com/search?q=";

   public IReadOnlyList<EntityListItem> Entities { get; private set; } = [];

   public string SortColumn { get; private set; } = NameSortColumn;

   public bool SortAsc { get; private set; } = true;

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(
      string? sortColumn,
      bool sortAsc = true,
      CancellationToken cancellationToken = default
   )
   {
      SortColumn = NormalizeSortColumn(sortColumn);
      SortAsc = sortAsc;

      try
      {
         var entities = await repository.GetEntitiesAsync(cancellationToken);
         Entities = SortEntities(entities, SortColumn, SortAsc);
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
      if (!string.Equals(SortColumn, sortColumn, StringComparison.Ordinal))
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

   private static string NormalizeSortColumn(string? sortColumn) =>
      sortColumn switch
      {
         TypeSortColumn => TypeSortColumn,
         SportSortColumn => SportSortColumn,
         WatchSortColumn => WatchSortColumn,
         StabilitySortColumn => StabilitySortColumn,
         _ => NameSortColumn
      };

   private static IReadOnlyList<EntityListItem> SortEntities(
      IEnumerable<EntityListItem> entities,
      string sortColumn,
      bool sortAsc
   )
   {
      return sortColumn switch
      {
         TypeSortColumn => OrderByDirection(entities, entity => entity.EntityType, sortAsc),
         SportSortColumn => OrderByDirection(entities, entity => entity.Sport, sortAsc),
         WatchSortColumn => OrderByDirection(entities, entity => entity.WatchPriority, sortAsc),
         StabilitySortColumn => OrderByDirection(entities, entity => entity.Stability, sortAsc),
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
         : entities.OrderByDescending(keySelector, StringComparer.OrdinalIgnoreCase);

      return sortedEntities.ThenBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase).ToList();
   }
}
