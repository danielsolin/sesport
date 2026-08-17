using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Ajax.Create;

public sealed class ParticipantEntityModel(
   AdminRepository adminRepository,
   IAiAutomationService automationService,
   IHostApplicationLifetime applicationLifetime
)
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

         if(string.Equals(
               template.EntityTypeId,
               TrackedEntityTypeIds.Person,
               StringComparison.OrdinalIgnoreCase
            ) && template.Id is not null)
         {
            await automationService.HandlePersonCreatedAsync(
               template.Id.Value,
               applicationLifetime.ApplicationStopping
            );
         }
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
      entity.AliasName = null;
      entity.Birthdate = null;
      entity.Height = null;
      entity.Weight = null;
      entity.FormativeClub = null;
   }
}
