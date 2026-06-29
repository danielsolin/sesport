using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Data;

namespace SESport.Web.Pages.Admin.Ajax.Search;

public sealed class EntityModel(AdminRepository repository) : PageModel
{
   public async Task<IActionResult> OnGetAsync(
      string? term,
      string? sortColumn,
      CancellationToken cancellationToken,
      bool? sortAsc = null,
      bool includeAll = false,
      bool organizationOnly = false
   )
   {
      term = term?.Trim() ?? string.Empty;

      if(term == string.Empty && !includeAll)
      {
         return new JsonResult(new { results = Array.Empty<object>() });
      }

      var results = term == string.Empty
         ? await repository.GetEntitiesAsync(
            cancellationToken,
            organizationOnly
         )
         : await repository.SearchEntitiesAsync(
            term,
            cancellationToken,
            organizationOnly
         );
      results = SortEntities(
         results,
         NormalizeSortColumn(sortColumn),
         sortAsc ?? true
      );

      return new JsonResult(new { results });
   }

   private static string NormalizeSortColumn(string? sortColumn) =>
      sortColumn switch
      {
         "Type" => "Type",
         "Sport" => "Sport",
         "Watch" => "Watch",
         "Country" => "Country",
         "Related" => "Related",
         _ => "Name"
      };

   private static IReadOnlyList<EntityListItem> SortEntities(
      IEnumerable<EntityListItem> entities,
      string sortColumn,
      bool sortAsc
   )
   {
      return sortColumn switch
      {
         "Type" => OrderByDirection(
            entities,
            entity => entity.EntityType,
            sortAsc
         ),
         "Sport" => OrderByDirection(
            entities,
            entity => entity.Sport,
            sortAsc
         ),
         "Watch" => OrderByDirection(
            entities,
            entity => entity.WatchPriority,
            sortAsc
         ),
         "Country" => OrderByDirection(
            entities,
            entity => entity.Country,
            sortAsc
         ),
         "Related" => OrderByDirection(
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
