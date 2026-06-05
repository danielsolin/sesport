using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SESport.Core.AIActivityTeasers;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Activities;

public class EditModel(
   ActivityRepository repository,
   TvSportRepository tvSportRepository,
   IActivityTeaserGenerator teaserGenerator
) : PageModel
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
      List<Guid>? tvSportBroadcastIds,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      ReturnUrl = GetLocalReturnUrl(returnUrl);

      if (id is null)
      {
         await LoadEntitiesAsync([], cancellationToken);
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

   private async Task<IActionResult> SaveAsync(
      CancellationToken cancellationToken
   )
   {
      ValidateActivity();

      if (!ModelState.IsValid)
      {
         await LoadEntitiesAsync(
            Activity.LinkedEntityIds ?? [],
            cancellationToken
         );
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

      return RedirectToPage("./Index");
   }

   public async Task<IActionResult> OnPostGenerateTeaserAsync(
      CancellationToken cancellationToken
   )
   {
      if (string.IsNullOrWhiteSpace(Activity.Title))
      {
         return BadRequest(new
         {
            error = "Title is required before generating a teaser."
         });
      }

      try
      {
         var teaser = await teaserGenerator.GenerateAsync(
            await CreateTeaserRequestAsync(cancellationToken),
            cancellationToken
         );
         var validationError = ValidateGeneratedTeaser(teaser);

         if(validationError is not null)
         {
            var teaserPreview = CreateTeaserPreview(teaser);

            return BadRequest(new
            {
               error = $"{validationError} Preview: \"{teaserPreview}\"",
               teaser,
               teaserPreview
            });
         }

         return new JsonResult(new { teaser });
      }
      catch (Exception exception)
      {
         return BadRequest(new { error = exception.Message });
      }
   }

   private async Task LoadEntitiesAsync(
      IEnumerable<Guid> selectedEntityIds,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var selectedIds = selectedEntityIds.ToHashSet();
         var entities = await repository.GetEntityOptionsAsync(
            cancellationToken
         );

         Entities = entities
            .Select(entity => new SelectListItem
            {
               Value = entity.Id.ToString(),
               Text = $"{entity.Name} ({entity.Type}/{entity.Sport})",
               Selected = selectedIds.Contains(entity.Id)
            })
            .ToList();

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

   private async Task<ActivityTeaserRequest> CreateTeaserRequestAsync(
      CancellationToken cancellationToken
   )
   {
      var selectedIds = (Activity.LinkedEntityIds ?? []).ToHashSet();
      var entityNames = (await repository.GetEntityOptionsAsync(
         cancellationToken
      ))
         .Where(entity => selectedIds.Contains(entity.Id))
         .Select(entity => entity.Name)
         .ToList();
      var sportName = (await repository.GetSportOptionsAsync(
         cancellationToken
      ))
         .FirstOrDefault(sport => sport.Id == Activity.SportId)
         ?.Label ?? Activity.SportId;

      return new ActivityTeaserRequest(
         Activity.Title,
         Activity.Description,
         Activity.ActivityType,
         sportName,
         Activity.ActivityDate,
         Activity.LocalStartTime,
         Activity.TimeZoneId,
         entityNames,
         []
      );
   }

   private static string? ValidateGeneratedTeaser(string teaser)
   {
      var wordCount = teaser
         .Split(' ', StringSplitOptions.RemoveEmptyEntries)
         .Length;

      if(string.IsNullOrWhiteSpace(teaser))
      {
         return "The model returned an empty teaser.";
      }

      if(wordCount < 15 || wordCount > 25)
      {
         return
            $"The model returned {wordCount} words, but the teaser must " +
            "be 15 to 25 words.";
      }

      var promptMarkers = new[]
      {
         "Requirements:",
         "Activity:",
         "The user wants",
         "Okay,",
         "Let's tackle this",
         "Need to",
         "Let me check"
      };

      var marker = promptMarkers.FirstOrDefault(value =>
         teaser.Contains(value, StringComparison.OrdinalIgnoreCase)
      );

      if(marker is not null)
      {
         return
            $"The model returned prompt/instruction text instead of a " +
            $"teaser. Matched marker: \"{marker}\".";
      }

      return null;
   }

   private static string CreateTeaserPreview(string teaser)
   {
      var preview = teaser
         .ReplaceLineEndings(" ")
         .Trim();

      if(preview.Length <= 180)
      {
         return preview;
      }

      return preview[..180] + "...";
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

      if (Activity.LinkedEntityIds is null ||
         Activity.LinkedEntityIds.Count == 0)
      {
         ModelState.AddModelError(
            "Activity.LinkedEntityIds",
            "At least one entity is required."
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
