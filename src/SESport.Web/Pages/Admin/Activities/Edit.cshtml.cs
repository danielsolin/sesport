using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SESport.Data;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Activities;

public class EditModel(ActivityEditPageService editService) : PageModel
{
   [BindProperty]
   public ActivityEditModel Activity { get; set; } = new();

   [BindProperty]
   public string? ReturnUrl { get; set; }

   public IReadOnlyList<SelectListItem> Entities { get; private set; } = [];

   public IReadOnlyList<LookupOption> ActivityTypes { get; private set; } = [];

   public IReadOnlyList<LookupOption> Sports { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      Guid? id,
      List<Guid>? broadcastIds,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      ReturnUrl = GetLocalReturnUrl(returnUrl);

      if(id is null)
      {
         await LoadEntitiesAsync([], cancellationToken);
         await editService.PrefillFromBroadcastsAsync(
            Activity,
            broadcastIds ?? [],
            cancellationToken
         );
         return Page();
      }

      Activity = await editService.LoadActivityAsync(
         id.Value,
         cancellationToken
      ) ?? new ActivityEditModel();

      if(Activity.Id is null)
      {
         return NotFound();
      }

      await LoadEntitiesAsync(
         Activity.LinkedEntityIds ?? [],
         cancellationToken
      );

      return Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      return await SaveAsync(cancellationToken);
   }

   public async Task<IActionResult> OnPostSaveAsync(
      CancellationToken cancellationToken
   )
   {
      return await SaveAsync(cancellationToken);
   }

   public async Task<IActionResult> OnPostGenerateTeaserAsync(
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(Activity.Title))
      {
         return BadRequest(new
         {
            error = "Title is required before generating a teaser."
         });
      }

      var result = await editService.GenerateTeaserAsync(
         Activity,
         cancellationToken
      );

      if(!string.IsNullOrWhiteSpace(result.ErrorMessage))
      {
         if(string.Equals(
            result.ErrorMessage,
            "The model returned invalid teaser JSON.",
            StringComparison.Ordinal
         ))
         {
            return BadRequest(new
            {
               error = result.ErrorMessage,
               prompt = result.Prompt,
               teaser = result.RawOutputText,
               teaserPreview = result.TeaserPreview
            });
         }

         return BadRequest(new
         {
            error = result.ErrorMessage,
            prompt = result.Prompt
         });
      }

      return new JsonResult(new
      {
         prompt = result.Prompt,
         teaser = result.Teaser
      });
   }

   private async Task LoadEntitiesAsync(
      IEnumerable<Guid> selectedEntityIds,
      CancellationToken cancellationToken
   )
   {
      var options = await editService.LoadOptionsAsync(
         selectedEntityIds,
         cancellationToken
      );

      Entities = options.Entities;
      ActivityTypes = options.ActivityTypes;
      Sports = options.Sports;
      LoadError = options.LoadError;
   }

   private async Task<IActionResult> SaveAsync(
      CancellationToken cancellationToken
   )
   {
      ValidateActivity();

      if(!ModelState.IsValid)
      {
         await LoadEntitiesAsync(
            Activity.LinkedEntityIds ?? [],
            cancellationToken
         );
         return Page();
      }

      await editService.SaveAsync(Activity, cancellationToken);

      if(ReturnUrl is not null)
      {
         return LocalRedirect(ReturnUrl);
      }

      return RedirectToPage("./Index");
   }

   private string? GetLocalReturnUrl(string? returnUrl)
   {
      if(string.IsNullOrWhiteSpace(returnUrl)
         || !Url.IsLocalUrl(returnUrl))
      {
         return null;
      }

      return returnUrl;
   }

   private void ValidateActivity()
   {
      if(string.IsNullOrWhiteSpace(Activity.Title))
      {
         ModelState.AddModelError("Activity.Title", "Title is required.");
      }

      if(string.IsNullOrWhiteSpace(Activity.ActivityType))
      {
         ModelState.AddModelError(
            "Activity.ActivityType",
            "Activity type is required."
         );
      }

      if(string.IsNullOrWhiteSpace(Activity.SportId))
      {
         ModelState.AddModelError(
            "Activity.SportId",
            "Sport is required."
         );
      }

      if(Activity.LinkedEntityIds is null ||
         Activity.LinkedEntityIds.Count == 0)
      {
         ModelState.AddModelError(
            "Activity.LinkedEntityIds",
            "At least one entity is required."
         );
      }

      if(Activity.ActivityDate is null)
      {
         ModelState.AddModelError(
            "Activity.ActivityDate",
            "Activity date is required."
         );
      }
   }
}
