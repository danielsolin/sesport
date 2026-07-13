using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.AI;
using SESport.Data.AI;

namespace SESport.Web.Pages.Admin.Config.Ai.Providers;

public class EditModel(AiAdminRepository repository) : PageModel
{
   [BindProperty]
   public AiProviderEditModel Provider { get; set; } = new();

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      string? id,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(id))
      {
         return Page();
      }

      Provider = await repository.GetProviderForEditAsync(
         id,
         cancellationToken
      ) ?? new AiProviderEditModel();

      return Provider.OriginalId is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      ValidateProvider();

      if(!ModelState.IsValid)
      {
         return Page();
      }

      try
      {
         await repository.SaveProviderAsync(Provider, cancellationToken);
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
         return Page();
      }

      return RedirectToPage("./Index");
   }

   private void ValidateProvider()
   {
      if(string.IsNullOrWhiteSpace(Provider.Id))
      {
         ModelState.AddModelError("Provider.Id", "ID is required.");
      }

      if(string.IsNullOrWhiteSpace(Provider.Label))
      {
         ModelState.AddModelError("Provider.Label", "Label is required.");
      }

      if(string.IsNullOrWhiteSpace(Provider.Kind))
      {
         ModelState.AddModelError("Provider.Kind", "Kind is required.");
      }

      ValidateJson("Provider.RequestOptionsJson", Provider.RequestOptionsJson);
   }

   private void ValidateJson(string fieldName, string? json)
   {
      if(string.IsNullOrWhiteSpace(json))
      {
         return;
      }

      try
      {
         JsonDocument.Parse(json);
      }
      catch(JsonException)
      {
         ModelState.AddModelError(fieldName, "Must be valid JSON.");
      }
   }
}
