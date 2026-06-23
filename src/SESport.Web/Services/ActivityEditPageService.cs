using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using SESport.AI.Interfaces;
using SESport.AI.Models;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data;

namespace SESport.Web.Services;

public sealed class ActivityEditPageService(
   ActivityRepository repository,
   BroadcastRepository broadcastRepository,
   BroadcastParticipationService participationService,
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
         var entities = await GetSelectableEntitiesAsync(cancellationToken);

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
      Guid? participationRunId,
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

      var firstBroadcast = broadcasts[0];
      var localStart = BroadcastRepository.ToLocal(firstBroadcast.StartsAt);
      var participationCheck =
         await participationService.GetParticipationCheckAsync(
            firstBroadcast.Id,
            participationRunId,
            cancellationToken
         );
      var selectableEntities = participationCheck is null
         ? []
         : await GetSelectableEntitiesAsync(cancellationToken);

      activity.BroadcastIds = [firstBroadcast.Id];
      activity.TvChannelName = firstBroadcast.ChannelName;
      activity.Title = BroadcastActivityPrefillBuilder.CreateActivityTitle(
         firstBroadcast,
         selectableEntities,
         participationCheck
      );
      activity.Description = firstBroadcast.Description;
      activity.ActivityType =
         BroadcastActivityTypeResolver.ResolveActivityType(
            firstBroadcast.Title,
            firstBroadcast.Description,
            firstBroadcast.Categories
         )?.ToString() ?? ActivityType.Match.ToString();
      activity.IsPublished = true;
      activity.EvidenceComment = BroadcastActivityPrefillBuilder
         .CreateEvidenceComment(firstBroadcast, participationCheck);

      var sportId = BroadcastCategorySportIdResolver.ResolveSportId(
         broadcasts.SelectMany(broadcast => broadcast.Categories)
      );

      if(!string.IsNullOrWhiteSpace(sportId))
      {
         activity.SportId = sportId;
      }

      activity.ActivityDate = DateOnly.FromDateTime(localStart.DateTime);
      activity.LocalStartTime = TimeOnly.FromDateTime(localStart.DateTime);
      activity.TimeZoneId = SportDay.TimeZoneId;
      activity.EvidenceTitle = firstBroadcast.Title;

      if(participationCheck is not null)
      {
         activity.LinkedEntityIds =
            BroadcastActivityPrefillBuilder.SelectLinkedEntityIds(
               selectableEntities,
               participationCheck
            ).ToList();
      }
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

   private async Task<
      IReadOnlyList<BroadcastEntityOption>
   > GetSelectableEntitiesAsync(
      CancellationToken cancellationToken
   )
   {
      var entities = await repository.GetEntityOptionsAsync(cancellationToken);

      return entities.Select(ToBroadcastEntityOption).ToList();
   }

   private async Task<string> CreateTeaserInputJsonAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken
   )
   {
      var selectedIds = (activity.LinkedEntityIds ?? []).ToHashSet();
      var entityNames = await GetSelectableEntitiesAsync(
         cancellationToken
      );

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
      return BroadcastActivityPrefillBuilder.NormalizeBroadcastIds(ids)
         .ToList();
   }

   private static string FormatEntityLabel(BroadcastEntityOption entity)
   {
      var name = string.IsNullOrWhiteSpace(entity.AliasName)
         ? entity.Name
         : $"{entity.Name} [aka {entity.AliasName}]";

      if(entity.Type == TrackedEntityTypeIds.Person &&
         !string.IsNullOrWhiteSpace(entity.Organization))
      {
         return $"{name} ({FormatEntityTypeLabel(entity.Type)}/" +
            $"{entity.Sport}/{entity.Organization})";
      }

      return $"{name} ({FormatEntityTypeLabel(entity.Type)}/" +
         $"{entity.Sport})";
   }

   private static string FormatEntityTypeLabel(string entityTypeId)
   {
      return entityTypeId switch
      {
         TrackedEntityTypeIds.Person => "Person",
         TrackedEntityTypeIds.NationalTeam => "National team",
         TrackedEntityTypeIds.Organization => "Organization",
         _ => entityTypeId
      };
   }

   private static BroadcastEntityOption ToBroadcastEntityOption(
      EntityOption entity
   )
   {
      return new BroadcastEntityOption(
         entity.Id,
         entity.Name,
         entity.Type,
         entity.Sport,
         entity.Organization,
         entity.AliasName
      );
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
