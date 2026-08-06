using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data;

namespace SESport.Web.Pages.Admin.Ajax.Update;

public sealed class EntityLinkModel(AdminRepository repository)
   : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      Guid id,
      string action,
      Guid linkedEntityId,
      CancellationToken cancellationToken
   )
   {
      action = NormalizeAction(action);

      if(id == Guid.Empty)
      {
         return BadRequest(new { error = "Entity ID is required." });
      }

      if(linkedEntityId == Guid.Empty)
      {
         return BadRequest(new { error = "Linked entity ID is required." });
      }

      if(id == linkedEntityId)
      {
         return BadRequest(
            new { error = "An entity cannot link to itself." }
         );
      }

      if(!IsSupportedAction(action))
      {
         return BadRequest(new { error = "Unsupported action." });
      }

      try
      {
         var isAddAction = IsAddAction(action);
         var changed = isAddAction
            ? await repository.AddEntityLinkAsync(
               id,
               linkedEntityId,
               cancellationToken
            )
            : await repository.RemoveEntityLinkAsync(
               id,
               linkedEntityId,
               cancellationToken
            );

         return new JsonResult(new
         {
            updated = true,
            action = isAddAction ? "add" : "remove",
            entityId = id,
            linkedEntityId,
            changed
         });
      }
      catch(EntityLinkEntityNotFoundException)
      {
         return NotFound(new { error = "Entity not found." });
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         return this.UnexpectedJsonError(exception);
      }
   }

   private static string NormalizeAction(string? action)
   {
      return action?
         .Trim()
         .Replace("-", string.Empty, StringComparison.Ordinal)
         .Replace("_", string.Empty, StringComparison.Ordinal)
         .ToLowerInvariant() ?? string.Empty;
   }

   private static bool IsAddAction(string action)
   {
      return action is "add" or "link";
   }

   private static bool IsSupportedAction(string action)
   {
      return action is "add" or "link" or "remove" or "unlink";
   }
}
