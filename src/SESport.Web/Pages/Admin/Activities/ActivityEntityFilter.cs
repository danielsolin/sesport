using SESport.Data;
using SESport.Core.Domain;

namespace SESport.Web.Pages.Admin.Activities;

internal static class ActivityEntityFilter
{
   internal static IReadOnlyList<EntityOption> FilterPersonEntities(
      IEnumerable<EntityOption> entities
   )
   {
      return entities
         .Where(entity => entity.Type == TrackedEntityTypeIds.Person)
         .ToList();
   }
}
