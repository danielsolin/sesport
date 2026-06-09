using SESport.Data;

namespace SESport.Web.Pages.Admin.Activities;

internal static class ActivityEntityFilter
{
   internal static IReadOnlyList<EntityOption> FilterPersonEntities(
      IEnumerable<EntityOption> entities
   )
   {
      return entities
         .Where(entity => entity.Type == "Person")
         .ToList();
   }
}
