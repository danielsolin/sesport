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

   public IReadOnlyList<SelectListItem> OrganizationEntities
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<LookupOption> ActivityTypes { get; private set; } = [];

   public IReadOnlyList<LookupOption> Sports { get; private set; } = [];

   public IReadOnlyList<ActivityParticipantListItem> Participants
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      Guid? id,
      List<Guid>? broadcastIds,
      Guid? participationRunId,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      ReturnUrl = GetLocalReturnUrl(returnUrl);
      Guid? organizationEntityId = null;

      if(id is null)
      {
         organizationEntityId =
            await editService.PrefillFromBroadcastsAsync(
               Activity,
               broadcastIds ?? [],
               participationRunId,
               cancellationToken
            );
         await LoadEntitiesAsync(
            organizationEntityId,
            Activity.LinkedEntityIds ?? [],
            cancellationToken,
            Activity.SportId
         );
         await LoadParticipantsAsync(cancellationToken);
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
         Activity.OrganizationEntityId,
         Activity.LinkedEntityIds ?? [],
         cancellationToken,
         Activity.SportId
      );
      await LoadParticipantsAsync(cancellationToken);

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

   public async Task<IActionResult> OnPostDeleteParticipantAsync(
      Guid id,
      Guid entityId,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      await editService.DeleteParticipantAsync(id, entityId, cancellationToken);

      return RedirectToPage(
         "./Edit",
         new
         {
            id,
            returnUrl = GetLocalReturnUrl(returnUrl)
         }
      );
   }

   public async Task<IActionResult> OnPostAddParticipantAsync(
      Guid id,
      Guid entityId,
      Guid? organizationEntityId,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      if(organizationEntityId is not null)
      {
         await editService.AddParticipantAsync(
            id,
            entityId,
            organizationEntityId.Value,
            cancellationToken
         );
      }

      return RedirectToPage(
         "./Edit",
         new
         {
            id,
            returnUrl = GetLocalReturnUrl(returnUrl)
         }
      );
   }

   private async Task LoadEntitiesAsync(
      Guid? organizationEntityId,
      IEnumerable<Guid> selectedEntityIds,
      CancellationToken cancellationToken,
      string? sportId = null
   )
   {
      var options = await editService.LoadOptionsAsync(
         selectedEntityIds,
         organizationEntityId,
         cancellationToken,
         sportId
      );

      Entities = options.Entities;
      OrganizationEntities = options.OrganizationEntities;
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
            Activity.OrganizationEntityId,
            Activity.LinkedEntityIds ?? [],
            cancellationToken,
            Activity.SportId
         );
         await LoadParticipantsAsync(cancellationToken);
         return Page();
      }

      await editService.SaveAsync(Activity, cancellationToken);

      if(ReturnUrl is not null)
      {
         return LocalRedirect(ReturnUrl);
      }

      return RedirectToPage("./Index");
   }

   private async Task LoadParticipantsAsync(
      CancellationToken cancellationToken
   )
   {
      Participants = await editService.LoadParticipantsAsync(
         Activity,
         cancellationToken
      );
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
