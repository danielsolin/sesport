using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Activities;

public class EditModel(ActivityRepository repository) : PageModel
{
   [BindProperty]
   public ActivityEditModel Activity { get; set; } = new();

   public IReadOnlyList<EntityOption> Entities { get; private set; } = [];

   public IReadOnlyList<LookupOption> ActivityTypes { get; private set; } = [];

   public IReadOnlyList<LookupOption> Sports { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      Guid? id,
      CancellationToken cancellationToken
   )
   {
      await LoadEntitiesAsync(cancellationToken);

      if (id is null)
      {
         return Page();
      }

      var activity = await repository.GetForEditAsync(
         id.Value,
         cancellationToken
      );

      if (activity is null)
      {
         return NotFound();
      }

      Activity = activity;
      return Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      ValidateActivity();

      if (!ModelState.IsValid)
      {
         await LoadEntitiesAsync(cancellationToken);
         return Page();
      }

      var id = await repository.SaveAsync(Activity, cancellationToken);
      return RedirectToPage("./Edit", new { id });
   }

   private async Task LoadEntitiesAsync(CancellationToken cancellationToken)
   {
      try
      {
         Entities = await repository.GetEntityOptionsAsync(cancellationToken);
         ActivityTypes = await repository.GetActivityTypeOptionsAsync(
            cancellationToken
         );
         Sports = await repository.GetSportOptionsAsync(cancellationToken);
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   private void ValidateActivity()
   {
      if (string.IsNullOrWhiteSpace(Activity.Title))
      {
         ModelState.AddModelError("Activity.Title", "Title is required.");
      }

      if (string.IsNullOrWhiteSpace(Activity.SportId))
      {
         ModelState.AddModelError(
            "Activity.SportId",
            "Sport is required."
         );
      }

      if (Activity.EntityId is null)
      {
         ModelState.AddModelError(
            "Activity.EntityId",
            "Entity is required."
         );
      }

      if (Activity.ActivityDate is null)
      {
         ModelState.AddModelError(
            "Activity.ActivityDate",
            "Activity date is required."
         );
      }
   }
}
