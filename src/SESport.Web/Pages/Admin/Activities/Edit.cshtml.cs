using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.Core.Domain;
using SESport.Core.Sources;
using SESport.Data;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Activities;

public class EditModel(
   ActivityEditPageService editService,
   FactRepository factRepository,
   SourceReferenceRepository sourceRepository
) : PageModel
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

   public IReadOnlyList<FactRecord> Facts { get; private set; } = [];

   public IReadOnlyList<string> OtherGroupDescriptions
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   [TempData]
   public string? SourceError { get; set; }

   [TempData]
   public string? SourceMessage { get; set; }

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
         await LoadOtherGroupDescriptionsAsync(cancellationToken);
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
      await LoadFactsAsync(cancellationToken);
      await LoadOtherGroupDescriptionsAsync(cancellationToken);

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

   public async Task<IActionResult> OnPostDeleteFactAsync(
      Guid id,
      Guid factId,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      await factRepository.DeleteForActivityAsync(
         factId,
         id,
         cancellationToken
      );

      return RedirectToPage(
         "./Edit",
         new
         {
            id,
            returnUrl = GetLocalReturnUrl(returnUrl)
         }
      );
   }

   public async Task<IActionResult> OnPostAddSourceAsync(
      Guid id,
      string? sourceUrl,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      var activity = await editService.LoadActivityAsync(
         id,
         cancellationToken
      );

      if(activity is null)
      {
         return NotFound();
      }

      if(!TryNormalizeSourceUrl(sourceUrl, out var normalizedUrl))
      {
         SourceError = "Enter a valid HTTP or HTTPS URL.";
         return RedirectToEdit(id, returnUrl);
      }

      await sourceRepository.CreateAsync(
         SourceCorrelationTypes.Activity,
         id.ToString(),
         SourceKinds.ActivityEvidence,
         normalizedUrl,
         null,
         null,
         DateTimeOffset.UtcNow,
         cancellationToken
      );
      SourceMessage = "Source added.";

      return RedirectToEdit(id, returnUrl);
   }

   public async Task<IActionResult> OnPostSetParticipantActiveAsync(
      Guid id,
      Guid entityId,
      bool isActive,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      await editService.SetParticipantActiveAsync(
         id,
         entityId,
         isActive,
         cancellationToken
      );

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
         await LoadFactsAsync(cancellationToken);
         await LoadOtherGroupDescriptionsAsync(cancellationToken);
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

   private async Task LoadFactsAsync(CancellationToken cancellationToken)
   {
      Facts = Activity.Id is null
         ? []
         : await factRepository.GetForActivityAsync(
            Activity.Id.Value,
            cancellationToken
         );
   }

   private async Task LoadOtherGroupDescriptionsAsync(
      CancellationToken cancellationToken
   )
   {
      OtherGroupDescriptions = await editService
         .LoadOtherGroupDescriptionsAsync(Activity, cancellationToken);
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

   public static bool TryNormalizeSourceUrl(
      string? sourceUrl,
      out string normalizedUrl
   )
   {
      normalizedUrl = string.Empty;
      var trimmedUrl = sourceUrl?.Trim();

      if(!Uri.TryCreate(
         trimmedUrl,
         UriKind.Absolute,
         out var parsedUrl
      ))
      {
         return false;
      }

      if(parsedUrl.Scheme != Uri.UriSchemeHttp
         && parsedUrl.Scheme != Uri.UriSchemeHttps)
      {
         return false;
      }

      normalizedUrl = parsedUrl.AbsoluteUri;
      return true;
   }

   private IActionResult RedirectToEdit(Guid id, string? returnUrl)
   {
      return RedirectToPage(
         "./Edit",
         new
         {
            id,
            returnUrl = GetLocalReturnUrl(returnUrl)
         }
      );
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

      if(Activity.LocalEndTime is not null &&
         Activity.LocalStartTime is null)
      {
         ModelState.AddModelError(
            "Activity.LocalEndTime",
            "Start is required when end is set."
         );
      }

      if(Activity.LocalEndTime is not null &&
         Activity.LocalEndTime == Activity.LocalStartTime)
      {
         ModelState.AddModelError(
            "Activity.LocalEndTime",
            "End must differ from start."
         );
      }
   }
}
