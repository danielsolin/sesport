using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SESport.Web.Pages.Admin.Ajax.Search;

public sealed class ActivityParticipantModel(
   ActivityEditPageService editService
) : PageModel
{
   public async Task<IActionResult> OnGetAsync(
      Guid? organizationEntityId,
      string? term,
      Guid[]? excludedEntityIds,
      CancellationToken cancellationToken
   )
   {
      term = term?.Trim() ?? string.Empty;

      if(organizationEntityId is null)
      {
         return new JsonResult(new { results = Array.Empty<object>() });
      }

      var results = await editService.SearchParticipantCandidatesAsync(
         organizationEntityId.Value,
         term,
         excludedEntityIds ?? [],
         cancellationToken
      );

      return new JsonResult(new
      {
         results = results.Select(participant => new
         {
            id = participant.Id,
            text = participant.Name,
            relatedOrganizations = participant.RelatedOrganizations,
            watchPriority = participant.WatchPriority,
            gender = participant.Gender,
            alias = participant.Alias
         })
      });
   }
}
