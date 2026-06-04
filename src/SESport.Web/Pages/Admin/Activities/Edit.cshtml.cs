using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Activities;

public class EditModel(
   ActivityRepository repository,
   TvSportRepository tvSportRepository
) : PageModel
{
   [BindProperty]
   public ActivityEditModel Activity { get; set; } = new();

   [BindProperty]
   public string? ReturnUrl { get; set; }

   public IReadOnlyList<EntityOption> Entities { get; private set; } = [];

   public IReadOnlyList<LookupOption> ActivityTypes { get; private set; } = [];

   public IReadOnlyList<LookupOption> Sports { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      Guid? id,
      List<Guid>? tvSportBroadcastIds,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      ReturnUrl = GetLocalReturnUrl(returnUrl);
      await LoadEntitiesAsync(cancellationToken);

      if (id is null)
      {
         await PrefillFromTvSportBroadcastsAsync(
            tvSportBroadcastIds ?? [],
            cancellationToken
         );
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
      await tvSportRepository.HideAsync(
         NormalizeBroadcastIds(Activity.TvSportBroadcastIds),
         cancellationToken
      );

      if(ReturnUrl is not null
         && Activity.TvSportBroadcastIds.Count > 0)
      {
         return LocalRedirect(ReturnUrl);
      }

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

   private async Task PrefillFromTvSportBroadcastsAsync(
      IReadOnlyCollection<Guid> ids,
      CancellationToken cancellationToken
   )
   {
      var normalizedIds = NormalizeBroadcastIds(ids);

      if(normalizedIds.Count == 0)
      {
         return;
      }

      var broadcasts = await tvSportRepository.GetActivitySourcesAsync(
         normalizedIds,
         cancellationToken
      );

      if(broadcasts.Count == 0)
      {
         return;
      }

      var firstBroadcast = broadcasts.First();
      var localStart = TvSportRepository.ToLocal(firstBroadcast.StartsAt);

      Activity.TvSportBroadcastIds = broadcasts
         .Select(broadcast => broadcast.Id)
         .ToList();
      Activity.Title = firstBroadcast.Title;
      Activity.Description = CreatePrefillDescription(broadcasts);
      Activity.ActivityType = "Match";
      Activity.SportId = GetSportId(broadcasts);
      Activity.ActivityDate = DateOnly.FromDateTime(localStart.DateTime);
      Activity.LocalStartTime = TimeOnly.FromDateTime(localStart.DateTime);
      Activity.TimeZoneId = "Europe/Stockholm";
      Activity.EvidenceTitle = broadcasts.Count == 1
         ? firstBroadcast.Title
         : $"{broadcasts.Count} TV broadcasts";
      Activity.EvidenceComment = CreateEvidenceComment(broadcasts);
   }

   private static List<Guid> NormalizeBroadcastIds(
      IEnumerable<Guid> ids
   )
   {
      return ids
         .Where(id => id != Guid.Empty)
         .Distinct()
         .ToList();
   }

   private static string? CreatePrefillDescription(
      IReadOnlyList<TvSportBroadcastActivitySource> broadcasts
   )
   {
      return broadcasts
         .Select(broadcast => broadcast.Description)
         .FirstOrDefault(description =>
            !string.IsNullOrWhiteSpace(description)
         );
   }

   private static string GetSportId(
      IReadOnlyList<TvSportBroadcastActivitySource> broadcasts
   )
   {
      var categories = broadcasts
         .SelectMany(broadcast => broadcast.Categories)
         .ToList();

      if(categories.Any(category =>
         string.Equals(category, "Fotboll", StringComparison.OrdinalIgnoreCase)
      ))
      {
         return "football";
      }

      return "football";
   }

   private static string CreateEvidenceComment(
      IReadOnlyList<TvSportBroadcastActivitySource> broadcasts
   )
   {
      var rows = broadcasts.Select(broadcast =>
      {
         var localStart = TvSportRepository.ToLocal(broadcast.StartsAt);
         var localEnd = TvSportRepository.ToLocal(broadcast.EndsAt);

         return string.Join(
            " ",
            [
               $"{localStart:yyyy-MM-dd HH:mm}-{localEnd:HH:mm}",
               broadcast.ChannelName,
               broadcast.Title,
               broadcast.Description ?? string.Empty
            ]
         ).Trim();
      });

      return string.Join(Environment.NewLine, rows);
   }
}
