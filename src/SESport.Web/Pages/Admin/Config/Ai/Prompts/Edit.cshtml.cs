using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Core.AI;
using System.Text.Json;

namespace SESport.Web.Pages.Admin.Config.Ai.Prompts;

public class EditModel(AiAdminRepository repository) : PageModel
{
   private static readonly JsonSerializerOptions IndentedJsonOptions = new()
   {
      WriteIndented = true
   };

   [BindProperty]
   public AiPromptEditModel Prompt { get; set; } = new();

   public IReadOnlyList<AiJobListItem> Jobs { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      string? id,
      CancellationToken cancellationToken
   )
   {
      await LoadJobsAsync(cancellationToken);

      if(string.IsNullOrWhiteSpace(id))
      {
         Prompt.Id = Guid.NewGuid().ToString();
         return Page();
      }

      Prompt = await repository.GetPromptForEditAsync(
         id,
         cancellationToken
      ) ?? new AiPromptEditModel();

      Prompt.OutputSchemaJson = FormatJson(Prompt.OutputSchemaJson);

      if(!string.IsNullOrWhiteSpace(Prompt.RequestOptionsJson))
      {
         Prompt.RequestOptionsJson = FormatJson(Prompt.RequestOptionsJson)!;
      }

      return Prompt.OriginalId is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      await LoadJobsAsync(cancellationToken);
      ValidatePrompt();

      if(!ModelState.IsValid)
      {
         return Page();
      }

      try
      {
         await repository.SavePromptAsync(Prompt, cancellationToken);
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
         return Page();
      }

      return RedirectToPage("./Index");
   }

   private async Task LoadJobsAsync(CancellationToken cancellationToken)
   {
      Jobs = await repository.GetJobsAsync(cancellationToken);
   }

   private void ValidatePrompt()
   {
      if(string.IsNullOrWhiteSpace(Prompt.Id))
      {
         ModelState.AddModelError("Prompt.Id", "ID is required.");
      }

      if(string.IsNullOrWhiteSpace(Prompt.JobId))
      {
         ModelState.AddModelError("Prompt.JobId", "Job is required.");
      }

      if(Prompt.Version < 1)
      {
         ModelState.AddModelError("Prompt.Version", "Version is required.");
      }

      if(Prompt.MaxToolRounds is not null && Prompt.MaxToolRounds < 1)
      {
         ModelState.AddModelError(
            "Prompt.MaxToolRounds",
            "Max tool rounds must be at least 1."
         );
      }

      if(Prompt.MinToolRounds is not null && Prompt.MinToolRounds < 1)
      {
         ModelState.AddModelError(
            "Prompt.MinToolRounds",
            "Min tool rounds must be at least 1."
         );
      }

      if(Prompt.MinToolRounds is not null &&
         Prompt.MaxToolRounds is not null &&
         Prompt.MinToolRounds > Prompt.MaxToolRounds)
      {
         ModelState.AddModelError(
            "Prompt.MinToolRounds",
            "Min tool rounds cannot exceed max tool rounds."
         );
      }

      if(string.IsNullOrWhiteSpace(Prompt.SystemPrompt))
      {
         ModelState.AddModelError(
            "Prompt.SystemPrompt",
            "System prompt is required."
         );
      }

      if(string.IsNullOrWhiteSpace(Prompt.UserPromptTemplate))
      {
         ModelState.AddModelError(
            "Prompt.UserPromptTemplate",
            "User prompt template is required."
         );
      }

      ValidateJson("Prompt.OutputSchemaJson", Prompt.OutputSchemaJson);
      ValidateJson("Prompt.RequestOptionsJson", Prompt.RequestOptionsJson);
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
}
