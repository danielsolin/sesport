using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.AI;

namespace SESport.Web.Pages.Admin.Config.Ai.Providers;

public class EditModel(AiAdminRepository repository) : PageModel
{
   private static readonly JsonSerializerOptions IndentedJsonOptions = new()
   {
      WriteIndented = true
   };

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
      Provider.RequestOptionsJson = FormatJson(
         RemoveDedicatedCodexOptions(Provider.RequestOptionsJson)
      ) ?? "{}";

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
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
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

   private static string? FormatJson(string? json)
   {
      if(string.IsNullOrWhiteSpace(json))
      {
         return json;
      }

      try
      {
         using var document = JsonDocument.Parse(json);
         return JsonSerializer.Serialize(
            document.RootElement,
            IndentedJsonOptions
         );
      }
      catch(JsonException)
      {
         return json;
      }
   }

   private static string? RemoveDedicatedCodexOptions(string? json)
   {
      if(string.IsNullOrWhiteSpace(json))
      {
         return json;
      }

      try
      {
         var options = JsonNode.Parse(json) as JsonObject;
         if(options is null)
         {
            return json;
         }

         options.Remove("codex_profile");
         options.Remove("codex_system_instruction");
         return options.ToJsonString();
      }
      catch(JsonException)
      {
         return json;
      }
   }
}
