using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Broadcast;
using SESport.Data;

namespace SESport.Web.Pages.Admin.Ajax.Create;

public sealed class ParticipantEntityModel(AdminRepository adminRepository)
   : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      string participantName,
      Guid templateEntityId,
      CancellationToken cancellationToken
   )
   {
      participantName = BroadcastParticipantNameFormatter.Format(
         participantName ?? string.Empty
      );

      if(string.IsNullOrWhiteSpace(participantName) ||
         templateEntityId == Guid.Empty)
      {
         return BadRequest(new
         {
            error = "Template entity missing."
         });
      }

      var template = await adminRepository.GetEntityCloneTemplateAsync(
         templateEntityId,
         cancellationToken
      );

      if(template is null)
      {
         return NotFound(new
         {
            error = "Template entity not found."
         });
      }

      template.CanonicalName = participantName;
      ClearPersonalData(template);

      try
      {
         await adminRepository.SaveEntityAsync(template, cancellationToken);
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         return this.UnexpectedJsonError(exception);
      }

      return new JsonResult(new
      {
         created = true,
         entityId = template.Id,
         canonicalName = template.CanonicalName,
         editUrl = Url.Page("/Admin/Entities/Edit", new { id = template.Id })
      });
   }

   internal static void ClearPersonalData(EntityEditModel entity)
   {
      entity.Birthdate = null;
      entity.Height = null;
      entity.Weight = null;
      entity.FormativeClub = null;
   }
}
