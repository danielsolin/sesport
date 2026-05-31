using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Activities;

public class EditModel(ActivityRepository repository) : PageModel
{
   public static readonly IReadOnlyList<string> ActivityTypes =
   [
      "Match",
      "Race",
      "Tournament",
      "Stage",
      "Championship",
      "Qualification",
      "RosterAnnouncement",
      "Transfer",
      "Ranking",
      "CoachingRole",
      "OtherSportingActivity"
   ];

   public static readonly IReadOnlyList<string> TimeKinds =
   [
      "ExactStart",
      "DateRange",
      "ToBeDetermined"
   ];

   public static readonly IReadOnlyList<string> EntityRoles =
   [
      "CompetesIn",
      "PlaysForContext",
      "SelectedForRoster",
      "TransferSubject",
      "CoachingRole",
      "RecurringEventEdition",
      "RelatedOrganization",
      "Other"
   ];

   [BindProperty]
   public ActivityEditModel Activity { get; set; } = new();

   public IReadOnlyList<EntityOption> Entities { get; private set; } = [];

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
         ModelState.AddModelError("Activity.SportId", "Sport id is required.");
      }

      if (string.IsNullOrWhiteSpace(Activity.SportName))
      {
         ModelState.AddModelError(
            "Activity.SportName",
            "Sport name is required."
         );
      }

      if (string.IsNullOrWhiteSpace(Activity.CountryRelevanceExplanation))
      {
         ModelState.AddModelError(
            "Activity.CountryRelevanceExplanation",
            "Country relevance explanation is required."
         );
      }

      if (
         Activity.TimeKind == "ExactStart" &&
         string.IsNullOrWhiteSpace(Activity.StartsAtLocal)
      )
      {
         ModelState.AddModelError(
            "Activity.StartsAtLocal",
            "Exact start time is required."
         );
      }

      if (
         Activity.TimeKind == "DateRange" &&
         (Activity.StartsOn is null || Activity.EndsOn is null)
      )
      {
         ModelState.AddModelError(
            "Activity.StartsOn",
            "Date range start and end are required."
         );
      }
   }
}
