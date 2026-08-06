using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SESport.Web.Pages.Admin.Ajax.Update;

public sealed class ActivityParticipantAiResultValueModel(
   ActivityParticipantAiResultRepository repository
) : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      Guid id,
      string field,
      string? value,
      CancellationToken cancellationToken
   )
   {
      field = NormalizeField(field);
      value = NormalizeValue(value);

      if(id == Guid.Empty)
      {
         return BadRequest(new { error = "Result value ID is required." });
      }

      if(!string.Equals(field, "value", StringComparison.Ordinal))
      {
         return BadRequest(new { error = "Unsupported field." });
      }

      try
      {
         var updated = await repository.UpdateValueAsync(
            id,
            value,
            cancellationToken
         );

         if(!updated)
         {
            return NotFound();
         }

         return new JsonResult(new
         {
            updated = true,
            field = "value",
            value = value ?? string.Empty,
            displayValue = value ?? "-"
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

   private static string? NormalizeValue(string? value)
   {
      var normalized = value?.Trim();

      return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
   }
}
