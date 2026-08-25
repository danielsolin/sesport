using Npgsql;

using SESport.Core.AI;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Core.Sources;
using SESport.Data.Models;
using System.Text.RegularExpressions;

namespace SESport.Data.Repositories;

public sealed class ActivityRepository(NpgsqlDataSource dataSource)
{
   private readonly ActivityQueryRepository queries = new(dataSource);
   private readonly ActivityGroupQueryRepository groupQueries =
      new(dataSource);
   private readonly ActivityParticipantRepository participants =
      new(dataSource);
   private readonly ActivityMutationRepository mutations = new(dataSource);

   public Task<IReadOnlyList<ActivityListItem>> GetActivitiesAsync(
      DateOnly date,
      string? status,
      IReadOnlyCollection<string> sportIds,
      CancellationToken cancellationToken
   ) =>
      queries.GetActivitiesAsync(
         date,
         status,
         sportIds,
         cancellationToken
      );

   public Task<IReadOnlyList<ActivityListItem>> GetPublishedForDateAsync(
      DateOnly date,
      CancellationToken cancellationToken,
      Guid? watchedByMemberId = null
   ) =>
      queries.GetPublishedForDateAsync(
         date,
         cancellationToken,
         watchedByMemberId
      );

   public Task<IReadOnlyList<ActivityListItem>>
      GetPublishedFutureForMemberWatchesAsync(
         Guid memberId,
         DateTimeOffset now,
         CancellationToken cancellationToken
      ) =>
      queries.GetPublishedFutureForMemberWatchesAsync(
         memberId,
         now,
         cancellationToken
      );

   public Task<IReadOnlyList<PublishedDateParticipantCount>>
      GetPublishedDateParticipantCountsFromAsync(
         DateOnly firstDate,
         CancellationToken cancellationToken
      ) =>
      queries.GetPublishedDateParticipantCountsFromAsync(
         firstDate,
         cancellationToken
      );

   public Task<IReadOnlyList<EntityOption>> GetEntityOptionsAsync(
      CancellationToken cancellationToken
   ) =>
      queries.GetEntityOptionsAsync(cancellationToken);

   public Task<IReadOnlyList<EntityOption>>
      GetPersonEntitiesForOrganizationAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      ) =>
      queries.GetPersonEntitiesForOrganizationAsync(
         organizationEntityId,
         cancellationToken
      );

   public Task<IReadOnlyList<EntityOption>>
      GetPersonEntitiesForPromptCandidatesAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      ) =>
      queries.GetPersonEntitiesForPromptCandidatesAsync(
         organizationEntityId,
         cancellationToken
      );

   public Task<IReadOnlyList<LookupOption>> GetActivityTypeOptionsAsync(
      CancellationToken cancellationToken
   ) =>
      queries.GetActivityTypeOptionsAsync(cancellationToken);

   public Task<IReadOnlyList<LookupOption>> GetSportOptionsAsync(
      CancellationToken cancellationToken
   ) =>
      queries.GetSportOptionsAsync(cancellationToken);

   public Task<bool> RequiresParticipantStartTimesAsync(
      string sportId,
      CancellationToken cancellationToken
   ) =>
      queries.RequiresParticipantStartTimesAsync(
         sportId,
         cancellationToken
      );

   public Task<IReadOnlyList<LookupOption>> SearchActivityGroupOptionsAsync(
      string? term,
      string? sportId,
      CancellationToken cancellationToken,
      Guid? organizationEntityId = null
   ) =>
      groupQueries.SearchActivityGroupOptionsAsync(
         term,
         sportId,
         cancellationToken,
         organizationEntityId
      );

   public Task<ActivityEditModel?> GetForEditAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      queries.GetForEditAsync(id, cancellationToken);

   public Task<IReadOnlyList<string>> GetOtherGroupDescriptionsAsync(
      Guid activityGroupId,
      Guid? excludedActivityId,
      CancellationToken cancellationToken
   ) =>
      groupQueries.GetOtherGroupDescriptionsAsync(
         activityGroupId,
         excludedActivityId,
         cancellationToken
      );

   public Task<Guid?> GetActivityGroupIdAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      groupQueries.GetActivityGroupIdAsync(id, cancellationToken);

   public Task<string?> GetActivityGroupTitleAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      groupQueries.GetActivityGroupTitleAsync(id, cancellationToken);

   public Task<ActivityGroupEditModel?> GetActivityGroupForEditAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      groupQueries.GetActivityGroupForEditAsync(id, cancellationToken);

   public Task<IReadOnlyList<ActivityGroupActivityListItem>>
      GetActivitiesForGroupEditAsync(
         Guid activityGroupId,
         CancellationToken cancellationToken
      ) =>
      groupQueries.GetActivitiesForGroupEditAsync(
         activityGroupId,
         cancellationToken
      );

   public Task<IReadOnlyList<ActivityGroupSourceListItem>>
      GetSourcesForGroupEditAsync(
         Guid activityGroupId,
         CancellationToken cancellationToken
      ) =>
      groupQueries.GetSourcesForGroupEditAsync(
         activityGroupId,
         cancellationToken
      );

   public Task<
      IReadOnlyDictionary<Guid, IReadOnlyList<ActivityGroupParticipant>>>
      GetActivityGroupParticipantsAsync(
         IReadOnlyCollection<Guid> activityGroupIds,
         CancellationToken cancellationToken
      ) =>
      groupQueries.GetActivityGroupParticipantsAsync(
         activityGroupIds,
         cancellationToken
      );

   public Task<IReadOnlyList<ActivityParticipantListItem>>
      GetParticipantsForEditAsync(
         Guid? activityId,
         IReadOnlyCollection<Guid> entityIds,
         CancellationToken cancellationToken
      ) =>
      participants.GetParticipantsForEditAsync(
         activityId,
         entityIds,
         cancellationToken
      );

   public Task DeleteParticipantAsync(
      Guid activityId,
      Guid entityId,
      CancellationToken cancellationToken
   ) =>
      participants.DeleteParticipantAsync(
         activityId,
         entityId,
         cancellationToken
      );

   public Task SetParticipantActiveAsync(
      Guid activityId,
      Guid entityId,
      bool isActive,
      CancellationToken cancellationToken
   ) =>
      participants.SetParticipantActiveAsync(
         activityId,
         entityId,
         isActive,
         cancellationToken
      );

   public Task AddParticipantAsync(
      Guid activityId,
      Guid entityId,
      Guid organizationEntityId,
      CancellationToken cancellationToken
   ) =>
      participants.AddParticipantAsync(
         activityId,
         entityId,
         organizationEntityId,
         cancellationToken
      );

   public Task<IReadOnlyList<ActivityParticipantListItem>>
      SearchParticipantCandidatesAsync(
         Guid organizationEntityId,
         string term,
         IReadOnlyCollection<Guid> excludedEntityIds,
         CancellationToken cancellationToken
      ) =>
      participants.SearchParticipantCandidatesAsync(
         organizationEntityId,
         term,
         excludedEntityIds,
         cancellationToken
      );

   public Task<bool> UpdateActivityGroupAsync(
      ActivityGroupEditModel model,
      CancellationToken cancellationToken
   ) =>
      mutations.UpdateActivityGroupAsync(model, cancellationToken);

   public Task<Guid> SaveAsync(
      ActivityEditModel model,
      CancellationToken cancellationToken
   ) =>
      mutations.SaveAsync(model, cancellationToken);

   public Task DeleteAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      mutations.DeleteAsync(id, cancellationToken);

   public Task<bool> UpdateTeaserAsync(
      Guid id,
      string teaser,
      CancellationToken cancellationToken
   ) =>
      mutations.UpdateTeaserAsync(id, teaser, cancellationToken);

   public Task<bool> UpdateEmptyTeaserAsync(
      Guid id,
      string teaser,
      CancellationToken cancellationToken
   ) =>
      mutations.UpdateEmptyTeaserAsync(id, teaser, cancellationToken);

   internal static string GetActivityOrganizationEntityIdSql(
      string activityAlias
   ) =>
      ActivityQueryRepository.GetActivityOrganizationEntityIdSql(
         activityAlias
      );

   internal static string GetLinkedOrganizationNamesLateralSql(
      string entityAlias
   ) =>
      ActivityQueryRepository.GetLinkedOrganizationNamesLateralSql(
         entityAlias
      );

   private static string? GetSportIconPath(string? iconId)
   {
      if(string.IsNullOrWhiteSpace(iconId))
      {
         return null;
      }

      var fileName = Regex.Replace(
            iconId.Trim().ToLowerInvariant(),
            "[^a-z0-9_-]+",
            "-"
         )
         .Trim('-');

      return $"/icons/sports/{fileName}.svg";
   }
}
