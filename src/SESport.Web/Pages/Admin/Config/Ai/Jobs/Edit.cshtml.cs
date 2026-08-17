using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Core.AI;
using System.Text.Json;

namespace SESport.Web.Pages.Admin.Config.Ai.Jobs;

public class EditModel(AiAdminRepository repository) : PageModel
{
   private static readonly JsonSerializerOptions IndentedJsonOptions = new()
   {
      WriteIndented = true
   };

   [BindProperty]
   public AiJobEditModel Job { get; set; } = new();

   public IReadOnlyList<AiPromptListItem> Prompts { get; private set; } =
      [];

   public IReadOnlyList<AiProviderListItem> Providers { get; private set; } =
      [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      string? id,
      CancellationToken cancellationToken
   )
   {
      await LoadProvidersAsync(cancellationToken);

      if(string.IsNullOrWhiteSpace(id))
      {
         await LoadPromptsAsync(cancellationToken);
         return Page();
      }

      Job = await repository.GetJobForEditAsync(
         id,
         cancellationToken
      ) ?? new AiJobEditModel();

      Job.ToolsJson = PrettyPrintJson(Job.ToolsJson);
      Job.ConditionalToolsJson = PrettyPrintJson(Job.ConditionalToolsJson);

      await LoadPromptsAsync(cancellationToken);

      return Job.OriginalId is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      await LoadProvidersAsync(cancellationToken);
      await LoadPromptsAsync(cancellationToken);
      ValidateJob();

      if(!ModelState.IsValid)
      {
         return Page();
      }

      try
      {
         await repository.SaveJobAsync(Job, cancellationToken);
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
         return Page();
      }

      return RedirectToPage("./Index");
   }

   private async Task LoadProvidersAsync(CancellationToken cancellationToken)
   {
      Providers = await repository.GetProvidersAsync(cancellationToken);
   }

   private async Task LoadPromptsAsync(CancellationToken cancellationToken)
   {
      var jobId = string.IsNullOrWhiteSpace(Job.Id)
         ? Job.OriginalId
         : Job.Id.Trim();

      Prompts = string.IsNullOrWhiteSpace(jobId)
         ? []
         : await repository.GetJobPromptsAsync(jobId, cancellationToken);
   }

   private void ValidateJob()
   {
      if(string.IsNullOrWhiteSpace(Job.Id))
      {
         ModelState.AddModelError("Job.Id", "ID is required.");
      }

      if(string.IsNullOrWhiteSpace(Job.Label))
      {
         ModelState.AddModelError("Job.Label", "Label is required.");
      }

      if(string.IsNullOrWhiteSpace(Job.ProviderId))
      {
         ModelState.AddModelError("Job.ProviderId", "Provider is required.");
      }

      if(string.IsNullOrWhiteSpace(Job.OutputMode))
      {
         ModelState.AddModelError("Job.OutputMode", "Output mode is required.");
      }

      if(!string.IsNullOrWhiteSpace(Job.OutputMode)
         && Job.OutputMode is not (
            AiOutputModeIds.Text or
            AiOutputModeIds.JsonObject
         ))
      {
         ModelState.AddModelError(
            "Job.OutputMode",
            $"Output mode must be {AiOutputModeIds.Text} or " +
            $"{AiOutputModeIds.JsonObject}."
         );
      }

      ValidateJson("Job.ToolsJson", Job.ToolsJson);
      ValidateJson(
         "Job.ConditionalToolsJson",
         Job.ConditionalToolsJson
      );

      if(Job.RequiresWebSearch)
      {
         if(string.IsNullOrWhiteSpace(Job.ToolsJson))
         {
            ModelState.AddModelError(
               "Job.ToolsJson",
               "Tools JSON is required when web search is enabled."
            );
         }
      }

      if(!string.IsNullOrWhiteSpace(Job.ActivePromptId)
         && !Prompts.Any(prompt =>
            string.Equals(
               prompt.Id,
               Job.ActivePromptId,
               StringComparison.Ordinal
            )))
      {
         ModelState.AddModelError(
            "Job.ActivePromptId",
            "Active prompt must belong to this job."
         );
      }

   }

   private static string? PrettyPrintJson(string? json)
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
