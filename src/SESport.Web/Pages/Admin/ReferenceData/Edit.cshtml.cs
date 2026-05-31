using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.ReferenceData;

public class EditModel(AdminRepository repository) : PageModel
{
   [BindProperty]
   public ReferenceEditModel Row { get; set; } = new();

   public ReferenceTableInfo? CurrentTable { get; private set; }

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      string table,
      string? id,
      CancellationToken cancellationToken
   )
   {
      CurrentTable = await repository.GetReferenceTableInfoAsync(
         table,
         cancellationToken
      );

      if (CurrentTable is null)
      {
         return NotFound();
      }

      if (string.IsNullOrWhiteSpace(id))
      {
         return Page();
      }

      Row = await repository.GetReferenceForEditAsync(
         table,
         id,
         cancellationToken
      ) ?? new ReferenceEditModel();

      return Row.OriginalId is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnPostAsync(
      string table,
      CancellationToken cancellationToken
   )
   {
      CurrentTable = await repository.GetReferenceTableInfoAsync(
         table,
         cancellationToken
      );

      if (CurrentTable is null)
      {
         return NotFound();
      }

      ValidateRow();

      if (!ModelState.IsValid)
      {
         return Page();
      }

      try
      {
         await repository.SaveReferenceAsync(
            table,
            Row,
            cancellationToken
         );
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
         return Page();
      }

      return RedirectToPage("./Index", new { table });
   }

   private void ValidateRow()
   {
      if (string.IsNullOrWhiteSpace(Row.Id))
      {
         ModelState.AddModelError("Row.Id", "ID is required.");
      }

      if (string.IsNullOrWhiteSpace(Row.Label))
      {
         ModelState.AddModelError("Row.Label", "Label is required.");
      }
   }
}
