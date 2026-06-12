using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using SESport.AI.Abstractions;
using SESport.AI.Models;
using SESport.AI.Persistence;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data;
using SESport.Web.Formatting;
using SESport.Web.Pages.Admin.Activities;

namespace SESport.Web.Services;

public sealed class ActivityEditPageService(
   ActivityRepository repository,
   BroadcastRepository broadcastRepository,
   IAiJobRunner aiJobRunner
)
{
   private const string TeaserJobId = "generate-activity-teaser";

   public async Task<ActivityEditOptions> LoadOptionsAsync(
      IEnumerable<Guid> selectedEntityIds,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var selectedIds = selectedEntityIds.ToHashSet();
         var entities = await GetPersonEntitiesAsync(cancellationToken);

         var entityOptions = entities
            .Select(entity => new SelectListItem
            {
               Value = entity.Id.ToString(),
               Text = FormatEntityLabel(entity),
               Selected = selectedIds.Contains(entity.Id)
            })
            .ToList();

         var activityTypes = await repository.GetActivityTypeOptionsAsync(
            cancellationToken
         );
         var sports = await repository.GetSportOptionsAsync(cancellationToken);

         return new ActivityEditOptions(
            entityOptions,
            activityTypes,
            sports,
            null
         );
      }
      catch(Exception exception)
      {
         return new ActivityEditOptions([], [], [], exception.Message);
      }
   }

   public async Task<ActivityEditModel?> LoadActivityAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      return await repository.GetForEditAsync(id, cancellationToken);
   }

   public async Task SaveAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      _ = await repository.SaveAsync(activity, cancellationToken);
      await broadcastRepository.HideAsync(
         NormalizeBroadcastIds(activity.BroadcastIds),
         cancellationToken
      );
   }

   public async Task PrefillFromBroadcastsAsync(
      ActivityEditModel activity,
      IReadOnlyCollection<Guid> ids,
      CancellationToken cancellationToken
   )
   {
      var normalizedIds = NormalizeBroadcastIds(ids);

      if(normalizedIds.Count == 0)
      {
         return;
      }

      var broadcasts = await broadcastRepository.GetActivitySourcesAsync(
         normalizedIds,
         cancellationToken
      );

      if(broadcasts.Count == 0)
      {
         return;
      }

      var firstBroadcast = broadcasts.First();
      var localStart = BroadcastRepository.ToLocal(firstBroadcast.StartsAt);

      activity.BroadcastIds = broadcasts
         .Select(broadcast => broadcast.Id)
         .ToList();
      activity.TvChannelName = firstBroadcast.ChannelName;
      activity.Title = firstBroadcast.Title;
      activity.Description = CreatePrefillDescription(broadcasts);
      activity.ActivityType = ActivityType.Match.ToString();
      activity.IsPublished = true;

      var sportId = BroadcastCategorySportIdResolver.ResolveSportId(
         broadcasts
      );

      if(!string.IsNullOrWhiteSpace(sportId))
      {
         activity.SportId = sportId;
      }

      activity.ActivityDate = DateOnly.FromDateTime(localStart.DateTime);
      activity.LocalStartTime = TimeOnly.FromDateTime(localStart.DateTime);
      activity.TimeZoneId = SportDay.TimeZoneId;
      activity.EvidenceTitle = broadcasts.Count == 1
         ? firstBroadcast.Title
         : $"{broadcasts.Count} broadcasts";
      activity.EvidenceComment = CreateEvidenceComment(broadcasts);
   }

   public async Task<ActivityTeaserResult> GenerateTeaserAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      var result = await aiJobRunner.RunAsync(
         new AiJobRequest(
            TeaserJobId,
            await CreateTeaserInputJsonAsync(activity, cancellationToken),
            activity.Id?.ToString()
         ),
         cancellationToken
      );

      if(!string.IsNullOrWhiteSpace(result.ErrorMessage))
      {
         return new ActivityTeaserResult(
            result.Prompt,
            null,
            result.ErrorMessage,
            null,
            null
         );
      }

      var teaser = ExtractGeneratedTeaser(result.OutputText);

      if(teaser is null)
      {
         return new ActivityTeaserResult(
            result.Prompt,
            null,
            "The model returned invalid teaser JSON.",
            CreateTeaserPreview(result.OutputText),
            result.OutputText
         );
      }

      var validationError = ValidateGeneratedTeaser(teaser);

      if(validationError is not null)
      {
         return new ActivityTeaserResult(
            result.Prompt,
            teaser,
            $"{validationError} Preview: \"{CreateTeaserPreview(teaser)}\"",
            CreateTeaserPreview(teaser),
            teaser
         );
      }

      return new ActivityTeaserResult(result.Prompt, teaser, null, null, null);
   }

   private async Task<IReadOnlyList<EntityOption>> GetPersonEntitiesAsync(
      CancellationToken cancellationToken
   )
   {
      var entities = await repository.GetEntityOptionsAsync(cancellationToken);

      return ActivityEntityFilter.FilterPersonEntities(entities);
   }

   private async Task<string> CreateTeaserInputJsonAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      var selectedIds = (activity.LinkedEntityIds ?? []).ToHashSet();
      var entityNames = await GetPersonEntitiesAsync(cancellationToken);

      var selectedEntityNames = entityNames
         .Where(entity => selectedIds.Contains(entity.Id))
         .Select(entity => entity.Name)
         .ToList();

      var sportName = (await repository.GetSportOptionsAsync(
         cancellationToken
      ))
         .FirstOrDefault(sport => sport.Id == activity.SportId)
         ?.Label ?? activity.SportId;

      return JsonSerializer.Serialize(
         new
         {
            title = activity.Title,
            description = activity.Description,
            activity_type = activity.ActivityType,
            sport = sportName,
            activity_date = DateDisplay.Format(activity.ActivityDate),
            local_start_time = activity.LocalStartTime?.ToString("HH:mm"),
            time_zone_id = activity.TimeZoneId,
            entities = selectedEntityNames,
            related_entities = Array.Empty<string>()
         }
      );
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
      IReadOnlyList<BroadcastActivitySource> broadcasts
   )
   {
      return broadcasts
         .Select(broadcast => broadcast.Description)
         .FirstOrDefault(description =>
            !string.IsNullOrWhiteSpace(description)
         );
   }

   private static string CreateEvidenceComment(
      IReadOnlyList<BroadcastActivitySource> broadcasts
   )
   {
      var rows = broadcasts.Select(broadcast =>
      {
         var localStart = BroadcastRepository.ToLocal(broadcast.StartsAt);
         var localEnd = BroadcastRepository.ToLocal(broadcast.EndsAt);

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
      if(entity.Type != TrackedEntityTypeIds.Person ||
         string.IsNullOrWhiteSpace(entity.Organization))
      {
         return $"{entity.Name} ({entity.Type}/{entity.Sport})";
      }

      return $"{entity.Name} ({entity.Type}/{entity.Sport}/" +
         $"{entity.Organization})";
   }
}

public sealed record ActivityEditOptions(
   IReadOnlyList<SelectListItem> Entities,
   IReadOnlyList<LookupOption> ActivityTypes,
   IReadOnlyList<LookupOption> Sports,
   string? LoadError
);

public sealed record ActivityTeaserResult(
   string Prompt,
   string? Teaser,
   string? ErrorMessage,
   string? TeaserPreview,
   string? RawOutputText
);
