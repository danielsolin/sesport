using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.Core.AI;
using SESport.Core.Domain;
using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Activities;

public class EditModel(
   ActivityEditPageService editService,
   FactRepository factRepository,
   SourceReferenceRepository sourceRepository,
   ActivityParticipantAiResultRepository resultRepository
) : PageModel
{
   [BindProperty]
   public ActivityEditModel Activity { get; set; } = new();

   [BindProperty]
   public string? ReturnUrl { get; set; }

   [BindProperty]
   public string? SelectedAiJobId { get; set; } =
      AiJobIds.FindParticipantsStart;

   public IReadOnlyList<SelectListItem> ActivityAiJobOptions { get; } =
   [
      new(AiJobIds.FindActivityGroupFacts, AiJobIds.FindActivityGroupFacts),
      new(AiJobIds.FindParticipantsResult, AiJobIds.FindParticipantsResult),
      new(AiJobIds.FindParticipantsStart, AiJobIds.FindParticipantsStart)
   ];

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

   public IReadOnlyList<ActivityParticipantAiResultSetRecord> AiResults
   {
      get;
      private set;
   } = [];

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

   [TempData]
   public string? AiJobError { get; set; }

   [TempData]
   public string? AiJobMessage { get; set; }

   public async Task<IActionResult> OnGetAsync(
      Guid? id,
      List<Guid>? broadcastIds,
      Guid? participationRunId,
      string? returnUrl,
      CancellationToken cancellationToken,
      [FromQuery(Name = RouteKeys.ClearParticipants)]
      bool clearParticipants = false
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
               cancellationToken,
               clearParticipants
            );
         await LoadEntitiesAsync(
            organizationEntityId,
            Activity.LinkedEntityIds ?? [],
            cancellationToken,
            Activity.SportId
         );
         await LoadParticipantsAsync(cancellationToken);
         await LoadOtherGroupDescriptionsAsync(cancellationToken);
         AiResults = [];
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
      AiResults = await resultRepository.GetForActivityAsync(
         Activity.Id.Value,
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

   public async Task<IActionResult> OnPostRunAiJobAsync(
      Guid id,
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

      var jobId = SelectedAiJobId;
      if(!IsSupportedActivityAiJob(jobId))
      {
         AiJobError = "Select a valid activity AI job.";
         return RedirectToEdit(id, returnUrl);
      }

      var selectedJobId = jobId!;

      if(string.IsNullOrWhiteSpace(activity.Title))
      {
         AiJobError = selectedJobId switch
         {
            AiJobIds.FindActivityGroupFacts =>
               "Title is required before finding facts.",
            AiJobIds.FindParticipantsStart =>
               "Title is required before finding a start time.",
            _ => "Title is required before finding results."
         };
         return RedirectToEdit(id, returnUrl);
      }

      if(selectedJobId == AiJobIds.FindActivityGroupFacts &&
         activity.ActivityGroupId is null)
      {
         AiJobError =
            "Assign the activity to an ActivityGroup before finding " +
            "group facts.";
         return RedirectToEdit(id, returnUrl);
      }

      var runId = await editService.QueueActivityAiJobAsync(
         selectedJobId,
         activity,
         cancellationToken
      );
      AiJobMessage = $"{selectedJobId} queued. Run ID: {runId}.";

      return RedirectToEdit(id, returnUrl);
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

   public async Task<IActionResult> OnPostDeleteSourceAsync(
      Guid id,
      Guid sourceId,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      var source = await sourceRepository.GetAsync(
         sourceId,
         cancellationToken
      );

      if(source is null ||
         source.CorrelationType != SourceCorrelationTypes.Activity ||
         source.CorrelationId != id.ToString())
      {
         return NotFound();
      }

      await sourceRepository.DeleteAsync(sourceId, cancellationToken);

      return RedirectToPage(
         "./Edit",
         new
         {
            id,
            returnUrl = GetLocalReturnUrl(returnUrl)
         }
      );
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

   public static IHtmlContent RenderJsonBlock(string? value)
   {
      var encodedJson = System.Net.WebUtility.HtmlEncode(
         SESport.Web.Pages.Admin.Runs.DetailsModel.FormatJson(value)
      );

      return new HtmlString(
         $"<pre class=\"prompt-details-pre\">{encodedJson}</pre>"
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

      var returnUrl = GetLocalReturnUrl(ReturnUrl);
      if(returnUrl is not null)
      {
         return LocalRedirect(returnUrl);
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
         || IsAjaxReturnUrl(returnUrl)
         || !Url.IsLocalUrl(returnUrl))
      {
         return null;
      }

      return returnUrl;
   }

   internal static bool IsAjaxReturnUrl(string returnUrl)
   {
      var queryIndex = returnUrl.IndexOf('?', StringComparison.Ordinal);
      var path = queryIndex >= 0
         ? returnUrl[..queryIndex]
         : returnUrl;

      return path.Equals("/Admin/Ajax", StringComparison.OrdinalIgnoreCase)
         || path.StartsWith(
            "/Admin/Ajax/",
            StringComparison.OrdinalIgnoreCase
         );
   }

   private bool IsSupportedActivityAiJob(string? jobId)
   {
      return jobId is not null && ActivityAiJobOptions.Any(
         option => string.Equals(
            option.Value,
            jobId,
            StringComparison.Ordinal
         )
      );
   }

   public static bool TryNormalizeSourceUrl(
      string? sourceUrl,
      out string normalizedUrl
   )
   {
      return SourceUrlNormalizer.TryNormalize(
         sourceUrl,
         out normalizedUrl
      );
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
