using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data.Repositories;

namespace SESport.Web.Pages.Admin.Ajax.Update;

public sealed class EntityFieldModel(AdminRepository repository)
   : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      Guid id,
      string field,
      string? value,
      CancellationToken cancellationToken
   )
   {
      field = NormalizeField(field);
      value = value?.Trim();

      if(id == Guid.Empty)
      {
         return BadRequest(new { error = "Entity ID is required." });
      }

      if(string.IsNullOrWhiteSpace(field))
      {
         return BadRequest(new { error = "Field is required." });
      }

      if(!string.Equals(field, "watchpriority", StringComparison.Ordinal))
      {
         return BadRequest(new { error = "Unsupported field." });
      }

      try
      {
         var priorities = await repository.GetReferenceRowsAsync(
            "entity-watch-priorities",
            cancellationToken
         );
         var requestedWatchPriorityId =
            string.IsNullOrWhiteSpace(value) ? string.Empty : value;

         if(string.IsNullOrWhiteSpace(requestedWatchPriorityId))
         {
            return BadRequest(
               new { error = "Watch priority is required." }
            );
         }

         if(!priorities.Any(priority =>
            string.Equals(
               priority.Id,
               requestedWatchPriorityId,
               StringComparison.Ordinal
            )))
         {
            return BadRequest(new { error = "Select a valid watch priority." });
         }

         var updated = await repository.UpdateEntityWatchPriorityAsync(
            id,
            requestedWatchPriorityId,
            cancellationToken
         );

         if(!updated)
         {
            return NotFound();
         }

         var displayValue = priorities.First(priority =>
            string.Equals(
               priority.Id,
               requestedWatchPriorityId,
               StringComparison.Ordinal
            )).Label;

         return new JsonResult(new
         {
            updated = true,
            field = "watch-priority",
            value = requestedWatchPriorityId,
            displayValue
         });
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         return this.UnexpectedJsonError(exception);
      }
   }

   private static string NormalizeField(string? field)
   {
      return field?
         .Trim()
         .Replace("-", string.Empty, StringComparison.Ordinal)
         .Replace("_", string.Empty, StringComparison.Ordinal)
         .ToLowerInvariant() ?? string.Empty;
   }
}
