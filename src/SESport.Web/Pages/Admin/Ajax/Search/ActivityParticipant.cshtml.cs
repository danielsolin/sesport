using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data.Models;
using SESport.Web.Pages.Admin.Activities;

namespace SESport.Web.Pages.Admin.Ajax.Search;

public sealed class ActivityParticipantModel(
   ActivityEditPageService editService
) : PageModel
{
   public async Task<IActionResult> OnGetAsync(
      Guid? organizationEntityId,
      string? term,
      Guid[]? excludedEntityIds,
      Guid[]? selectedEntityIds,
      string? format,
      CancellationToken cancellationToken
   )
   {
      term = term?.Trim() ?? string.Empty;

      if(string.Equals(
            format?.Trim(),
            "participant-selection",
            StringComparison.OrdinalIgnoreCase
         ))
      {
         var participants = await editService.LoadParticipantsAsync(
            new ActivityEditModel
            {
               LinkedEntityIds = selectedEntityIds?.ToList() ?? []
            },
            cancellationToken
         );

         return Partial(
            "/Pages/Admin/Activities/_ActivityParticipantSelection.cshtml",
            new ActivityParticipantSelectionViewModel(participants, null)
         );
      }

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

      if(string.Equals(
            format?.Trim(),
            "participant-suggestions",
            StringComparison.OrdinalIgnoreCase
         ))
      {
         return Partial("_ParticipantSuggestions", results);
      }

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
