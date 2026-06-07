using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.Text;
using System.Text.Json;
using SESport.AI.Abstractions;
using SESport.AI.Models;
using SESport.Data;

namespace SESport.Web.Pages.Admin.Activities;

public class EditModel(
   ActivityRepository repository,
   TvSportRepository tvSportRepository,
   IAiJobRunner aiJobRunner
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

      if(id is null)
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

      if(activity is null)
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

      if(!ModelState.IsValid)
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

      if(ReturnUrl is not null)
      {
         return LocalRedirect(ReturnUrl);
      }

      return RedirectToPage("./Index");
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

      var result = await aiJobRunner.RunAsync(
         new AiJobRequest(
            "generate-activity-teaser",
            await CreateTeaserInputJsonAsync(cancellationToken),
            Activity.Id?.ToString()
         ),
         cancellationToken
      );

      if(!string.IsNullOrWhiteSpace(result.ErrorMessage))
      {
         return BadRequest(new
         {
            error = result.ErrorMessage,
            prompt = result.Prompt
         });
      }

      var teaser = ExtractGeneratedTeaser(result.OutputText);

      if(teaser is null)
      {
         return BadRequest(new
         {
            error = "The model returned invalid teaser JSON.",
            prompt = result.Prompt,
            teaser = result.OutputText,
            teaserPreview = CreateTeaserPreview(result.OutputText)
         });
      }

      var validationError = ValidateGeneratedTeaser(teaser);

      if(validationError is not null)
      {
         var teaserPreview = CreateTeaserPreview(teaser);

         return BadRequest(new
         {
            error = $"{validationError} Preview: \"{teaserPreview}\"",
            prompt = result.Prompt,
            teaser,
            teaserPreview
         });
      }

      return new JsonResult(new
      {
         prompt = result.Prompt,
         teaser
      });
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
               Text = FormatEntityLabel(entity),
               Selected = selectedIds.Contains(entity.Id)
            })
            .ToList();

         ActivityTypes = await repository.GetActivityTypeOptionsAsync(
            cancellationToken
         );
         Sports = await repository.GetSportOptionsAsync(cancellationToken);
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   private static string? ValidateGeneratedTeaser(string teaser)
   {
      if(string.IsNullOrWhiteSpace(teaser))
      {
         return "The model returned an empty teaser.";
      }

      return null;
   }

   private static string? ExtractGeneratedTeaser(string outputText)
   {
      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(
            root.TryGetProperty("teaser", out var teaser) &&
            teaser.ValueKind == JsonValueKind.String
         )
         {
            return teaser.GetString();
         }
      }
      catch(JsonException)
      {
      }

      return outputText;
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

   private async Task<string> CreateTeaserInputJsonAsync(
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

      return JsonSerializer.Serialize(
         new
         {
            title = Activity.Title,
            description = Activity.Description,
            activity_type = Activity.ActivityType,
            sport = sportName,
            activity_date = Activity.ActivityDate?.ToString("yyyy-MM-dd"),
            local_start_time = Activity.LocalStartTime?.ToString("HH:mm"),
            time_zone_id = Activity.TimeZoneId,
            entities = entityNames,
            related_entities = Array.Empty<string>()
         }
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
      Activity.TvChannelName = firstBroadcast.ChannelName;
      Activity.Title = firstBroadcast.Title;
      Activity.Description = CreatePrefillDescription(broadcasts);
      Activity.ActivityType = "Match";
      Activity.IsPublished = true;
      var sportId = GetSportId(broadcasts);
      if(!string.IsNullOrWhiteSpace(sportId))
      {
         Activity.SportId = sportId;
      }
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

   private static string? GetSportId(
      IReadOnlyList<TvSportBroadcastActivitySource> broadcasts
   )
   {
      var categories = broadcasts
         .SelectMany(broadcast => broadcast.Categories)
         .Select(NormalizeCategoryKey)
         .Where(category => !string.IsNullOrWhiteSpace(category))
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();

      foreach(var category in categories)
      {
         if(TryGetSpecificSportId(category, out var sportId))
         {
            return sportId;
         }
      }

      foreach(var category in categories)
      {
         if(TryGetGenericSportId(category, out var sportId))
         {
            return sportId;
         }
      }

      return null;
   }

   private static bool TryGetSpecificSportId(
      string category,
      out string sportId
   )
   {
      switch(category)
      {
         case "golf":
            sportId = "golf";
            return true;
         case "fotboll":
            sportId = "football";
            return true;
         case "ishockey":
         case "ishockeyvm":
            sportId = "ice-hockey";
            return true;
         case "basket":
            sportId = "basketball";
            return true;
         case "dart":
            sportId = "darts";
            return true;
         case "friidrott":
            sportId = "athletics";
            return true;
         case "maraton":
         case "terranglopning":
            sportId = "athletics-road-running";
            return true;
         case "handboll":
            sportId = "handball";
            return true;
         case "segling":
            sportId = "sailing";
            return true;
         case "speedway":
            sportId = "speedway";
            return true;
         case "tennis":
            sportId = "tennis";
            return true;
         case "volleyball":
            sportId = "volleyball";
            return true;
         case "formel1":
         case "formele":
         case "motocross":
         case "motorcykel":
         case "motorsport":
            sportId = "motorsport";
            return true;
         case "djursport":
         case "galoppsport":
         case "hoppning":
         case "ridsport":
            sportId = "equestrian";
            return true;
      }

      sportId = string.Empty;
      return false;
   }

   private static bool TryGetGenericSportId(
      string category,
      out string sportId
   )
   {
      switch(category)
      {
         case "baseball":
         case "bollsport":
         case "cykling":
         case "extremsport":
         case "faktning":
         case "fysisksport":
         case "fysisksporter":
         case "kampsport":
         case "klattring":
         case "livesport":
         case "malsport":
         case "mountainbike":
         case "multisportlopp":
         case "racketsport":
         case "sporttavlingar":
         case "triathlon":
         case "tyngdlyftning":
         case "varldscupen":
         case "vattensport":
            sportId = "multi-sport";
            return true;
      }

      sportId = string.Empty;
      return false;
   }

   private static string NormalizeCategoryKey(string value)
   {
      var normalized = value.Normalize(NormalizationForm.FormD);
      var builder = new StringBuilder(normalized.Length);

      foreach(var character in normalized)
      {
         if(CharUnicodeInfo.GetUnicodeCategory(character) ==
            UnicodeCategory.NonSpacingMark)
         {
            continue;
         }

         if(char.IsLetterOrDigit(character))
         {
            builder.Append(char.ToLowerInvariant(character));
         }
      }

      return builder.ToString();
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

   private static string FormatEntityLabel(EntityOption entity)
   {
      if(entity.Type != "Person" ||
         string.IsNullOrWhiteSpace(entity.Organization))
      {
         return $"{entity.Name} ({entity.Type}/{entity.Sport})";
      }

      return $"{entity.Name} ({entity.Type}/{entity.Sport}/" +
         $"{entity.Organization})";
   }
}
