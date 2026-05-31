using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Sources;

public class EditModel(AdminRepository repository) : PageModel
{
   [BindProperty]
   public SourceEditModel Source { get; set; } = new();

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      string? id,
      CancellationToken cancellationToken
   )
   {
      if (string.IsNullOrWhiteSpace(id))
      {
         return Page();
      }

      Source = await repository.GetSourceForEditAsync(
         id,
         cancellationToken
      ) ?? new SourceEditModel();

      return Source.OriginalId is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      ValidateSource();

      if (!ModelState.IsValid)
      {
         return Page();
      }

      try
      {
         await repository.SaveSourceAsync(Source, cancellationToken);
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
         return Page();
      }

      return RedirectToPage("./Index");
   }

   private void ValidateSource()
   {
      if (string.IsNullOrWhiteSpace(Source.Id))
      {
         ModelState.AddModelError("Source.Id", "ID is required.");
      }

      if (string.IsNullOrWhiteSpace(Source.Name))
      {
         ModelState.AddModelError("Source.Name", "Name is required.");
      }
   }
}
