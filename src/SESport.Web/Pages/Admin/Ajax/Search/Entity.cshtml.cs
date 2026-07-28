using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data.Models;
using SESport.Data.Repositories;

namespace SESport.Web.Pages.Admin.Ajax.Search;

public sealed class EntityModel(AdminRepository repository) : PageModel
{
   public async Task<IActionResult> OnGetAsync(
      string? term,
      string? sortColumn,
      CancellationToken cancellationToken,
      bool? sortAsc = null,
      bool includeAll = false,
      bool organizationOnly = false,
      string[]? entityTypeIds = null,
      string[]? sportIds = null,
      Guid? excludeEntityId = null,
      int? maxResults = null,
      DateOnly? date = null,
      bool includeRelatedEntityNames = true
   )
   {
      term = term?.Trim() ?? string.Empty;
      var normalizedEntityTypeIds = NormalizeEntityTypeIds(entityTypeIds);
      var normalizedSportIds = NormalizeEntityTypeIds(sportIds);
      var normalizedMaxResults = maxResults is > 0 ? maxResults : null;

      if(term == string.Empty &&
         normalizedEntityTypeIds.Count == 0 &&
         normalizedSportIds.Count == 0 &&
         !includeAll)
      {
         return new JsonResult(new { results = Array.Empty<object>() });
      }

      var results = term == string.Empty
         ? await repository.GetEntitiesAsync(
            cancellationToken,
            organizationOnly,
            normalizedEntityTypeIds,
            excludeEntityId,
            normalizedMaxResults,
            activityDate: date,
            sportIds: normalizedSportIds
         )
         : await repository.SearchEntitiesAsync(
            term,
            cancellationToken,
            organizationOnly,
            normalizedEntityTypeIds,
            excludeEntityId,
            normalizedMaxResults,
            activityDate: date,
            includeRelatedEntityNames: includeRelatedEntityNames,
            sportIds: normalizedSportIds
         );
      results = SortEntities(
         results,
         NormalizeSortColumn(sortColumn),
         sortAsc ?? true
      );

      return new JsonResult(new { results });
   }

   private static IReadOnlyList<string> NormalizeEntityTypeIds(
      IEnumerable<string>? entityTypeIds
   )
   {
      if(entityTypeIds is null)
      {
         return [];
      }

      return entityTypeIds
         .SelectMany(value => value.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries |
               StringSplitOptions.TrimEntries
         ))
         .Where(value => value != string.Empty)
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();
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
