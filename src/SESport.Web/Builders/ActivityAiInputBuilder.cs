using System.Text.Json;

using SESport.Core.Formatting;
using SESport.Data.Models;

namespace SESport.Web.Builders;

public sealed class ActivityAiInputBuilder(
   ActivityRepository activityRepository,
   AdminRepository adminRepository
)
{
   public async Task<string> BuildAsync(
      ActivityEditModel activity,
      CancellationToken cancellationToken,
      string? promptTitle = null
   )
   {
      var selectedIds = (activity.LinkedEntityIds ?? []).ToHashSet();
      var entities = await activityRepository.GetEntityOptionsAsync(
         cancellationToken
      );
      var participantEntities = entities
         .Where(entity => selectedIds.Contains(entity.Id))
         .ToList();
      var participantNames = participantEntities
         .Select(entity => entity.Name)
         .ToList();
      var sportName = (await activityRepository.GetSportOptionsAsync(
         cancellationToken
      ))
         .FirstOrDefault(sport => sport.Id == activity.SportId)
         ?.Label ?? activity.SportId;
      var organizationName = await GetOrganizationNameAsync(
         activity.OrganizationEntityId,
         cancellationToken
      );

      return JsonSerializer.Serialize(
         new
         {
            event_name = activity.Title,
            title = promptTitle ?? activity.Title,
            type = organizationName,
            description = activity.Description,
            activity_type = activity.ActivityType,
            sport = sportName,
            activity_date = DateDisplay.Format(activity.ActivityDate),
            local_start_time = activity.LocalStartTime?.ToString(
               DateDisplay.TimeOnlyMinutesFormat
            ),
            local_end_time = activity.LocalEndTime?.ToString(
               DateDisplay.TimeOnlyMinutesFormat
            ),
            time_zone_id = activity.TimeZoneId,
            participants = CreatePromptListText(participantNames),
            participant_entities = participantEntities.Select(entity => new
            {
               id = entity.Id,
               name = entity.Name,
               alias_name = entity.AliasName
            }),
            related_entities = Array.Empty<string>()
         }
      );
   }

   public async Task<string> BuildActivityGroupAsync(
      Guid activityGroupId,
      CancellationToken cancellationToken
   )
   {
      var group = await activityRepository.GetActivityGroupForEditAsync(
         activityGroupId,
         cancellationToken
      );

      if(group is null || group.StartDate is null || group.EndDate is null)
      {
         throw new InvalidOperationException(
            $"Activity group '{activityGroupId}' was not found."
         );
      }

      var activities = await activityRepository.GetActivitiesForGroupEditAsync(
         activityGroupId,
         cancellationToken
      );
      var sportName = (await activityRepository.GetSportOptionsAsync(
         cancellationToken
      ))
         .FirstOrDefault(sport => sport.Id == group.SportId)
         ?.Label ?? group.SportId;
      var descriptions = activities
         .Select(activity => activity.Description?.Trim())
         .Where(description => !string.IsNullOrWhiteSpace(description))
         .Distinct(StringComparer.Ordinal)
         .ToList();

      return JsonSerializer.Serialize(
         new
         {
            event_name = group.Title,
            title = group.Title,
            type = string.Empty,
            description = string.Join(
               Environment.NewLine,
               descriptions
            ),
            activity_type = string.Empty,
            sport = sportName,
            activity_date = FormatDateRange(
               group.StartDate.Value,
               group.EndDate.Value
            ),
            activity_group_id = activityGroupId,
            activity_count = activities.Count,
            activities = activities.Select(activity => new
            {
               title = activity.Title,
               date = DateDisplay.Format(activity.ActivityDate),
               description = activity.Description
            }),
            participants = string.Empty,
            participant_entities = Array.Empty<object>(),
            related_entities = Array.Empty<string>()
         }
      );
   }

   private async Task<string> GetOrganizationNameAsync(
      Guid? organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      if(organizationEntityId is null)
      {
         return string.Empty;
      }

      var entity = await adminRepository.GetEntityForEditAsync(
         organizationEntityId.Value,
         cancellationToken
      );
      return entity?.CanonicalName ?? string.Empty;
   }

   private static string CreatePromptListText(
      IReadOnlyList<string> values
   )
   {
      return values.Count == 0
         ? string.Empty
         : string.Join(
            Environment.NewLine,
            values.Select(value => $"  - {value}")
      );
   }

   private static string FormatDateRange(
      DateOnly startDate,
      DateOnly endDate
   )
   {
      return startDate == endDate
         ? DateDisplay.Format(startDate)
         : $"{DateDisplay.Format(startDate)} - " +
            DateDisplay.Format(endDate);
   }
}
