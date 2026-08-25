using Npgsql;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class EntityRepository(NpgsqlDataSource dataSource)
{
   private readonly EntityQueryRepository queries = new(dataSource);
   private readonly EntityMutationRepository mutations = new(dataSource);

   public Task<IReadOnlyList<EntityListItem>> SearchEntitiesAsync(
      string? term,
      CancellationToken cancellationToken,
      bool broadcastOrganizationOnly = false,
      IReadOnlyCollection<string>? entityTypeIds = null,
      Guid? excludeEntityId = null,
      int? maxResults = null,
      DateOnly? activityDate = null,
      bool includeRelatedEntityNames = true,
      IReadOnlyCollection<string>? sportIds = null
   ) =>
      queries.SearchEntitiesAsync(
         term,
         cancellationToken,
         broadcastOrganizationOnly,
         entityTypeIds,
         excludeEntityId,
         maxResults,
         activityDate,
         includeRelatedEntityNames,
         sportIds
      );

   public Task<IReadOnlyList<EntityListItem>> GetEntitiesAsync(
      CancellationToken cancellationToken,
      bool broadcastOrganizationOnly = false,
      IReadOnlyCollection<string>? entityTypeIds = null,
      Guid? excludeEntityId = null,
      int? maxResults = null,
      DateOnly? activityDate = null,
      IReadOnlyCollection<string>? sportIds = null
   ) =>
      queries.GetEntitiesAsync(
         cancellationToken,
         broadcastOrganizationOnly,
         entityTypeIds,
         excludeEntityId,
         maxResults,
         activityDate,
         sportIds
      );

   public Task<IReadOnlyList<EntityLinkOption>> GetEntityLinkOptionsByIdsAsync(
      IReadOnlyCollection<Guid> ids,
      Guid? excludeEntityId,
      CancellationToken cancellationToken
   ) =>
      queries.GetEntityLinkOptionsByIdsAsync(
         ids,
         excludeEntityId,
         cancellationToken
      );

   public Task<EntityEditModel?> GetEntityForEditAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      queries.GetEntityForEditAsync(id, cancellationToken);

   public Task<EntityPrimaryThumbnail?> GetEntityPrimaryThumbnailAsync(
      Guid entityId,
      CancellationToken cancellationToken
   ) =>
      queries.GetEntityPrimaryThumbnailAsync(entityId, cancellationToken);

   public Task<IReadOnlyList<EntityActivityListItem>> GetEntityActivitiesAsync(
      Guid entityId,
      CancellationToken cancellationToken
   ) =>
      queries.GetEntityActivitiesAsync(entityId, cancellationToken);

   public Task<EntityEditModel?> GetEntityCloneTemplateAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      queries.GetEntityCloneTemplateAsync(id, cancellationToken);

   public Task<IReadOnlyList<EntityLinkOption>> GetEntityLinkOptionsAsync(
      Guid? excludeEntityId,
      CancellationToken cancellationToken
   ) =>
      queries.GetEntityLinkOptionsAsync(excludeEntityId, cancellationToken);

   public Task<IReadOnlyList<EntityLinkOption>>
      GetOrganizationEntityOptionsAsync(
         CancellationToken cancellationToken,
         string? sportId = null
      ) =>
      queries.GetOrganizationEntityOptionsAsync(
         cancellationToken,
         sportId
      );

   public Task<IReadOnlyList<EntityLinkOption>>
      GetBroadcastOrganizationLinkOptionsAsync(
         CancellationToken cancellationToken
      ) =>
      queries.GetBroadcastOrganizationLinkOptionsAsync(cancellationToken);

   public Task<IReadOnlyList<EntityLinkOption>>
      SearchBroadcastOrganizationLinkOptionsAsync(
         string term,
         CancellationToken cancellationToken
      ) =>
      queries.SearchBroadcastOrganizationLinkOptionsAsync(
         term,
         cancellationToken
      );

   public Task<IReadOnlyList<EntityNameOption>>
      GetPersonEntityNameOptionsAsync(
         CancellationToken cancellationToken
      ) =>
      queries.GetPersonEntityNameOptionsAsync(cancellationToken);

   public Task<IReadOnlyList<EntityNameOption>>
      GetPersonEntityNameOptionsAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      ) =>
      queries.GetPersonEntityNameOptionsAsync(
         organizationEntityId,
         cancellationToken
      );

   public Task<IReadOnlyList<EntityNameOption>>
      GetParticipantEntityNameOptionsAsync(
         CancellationToken cancellationToken
      ) =>
      queries.GetParticipantEntityNameOptionsAsync(cancellationToken);

   public Task<IReadOnlyList<EntityNameOption>>
      GetParticipantEntityNameOptionsAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      ) =>
      queries.GetParticipantEntityNameOptionsAsync(
         organizationEntityId,
         cancellationToken
      );

   public Task<IReadOnlyList<EntityNameOption>>
      GetBroadcastParticipantEntityNameOptionsAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      ) =>
      queries.GetBroadcastParticipantEntityNameOptionsAsync(
         organizationEntityId,
         cancellationToken
      );

   public Task<IReadOnlyList<LookupOption>> GetCountryOptionsAsync(
      CancellationToken cancellationToken
   ) =>
      queries.GetCountryOptionsAsync(cancellationToken);

   public Task<IReadOnlyList<LookupOption>> GetPersonGenderOptionsAsync(
      CancellationToken cancellationToken
   ) =>
      queries.GetPersonGenderOptionsAsync(cancellationToken);

   public Task ReplacePrimaryEntityImageAsync(
      Guid entityId,
      EntityImageReplacement replacement,
      CancellationToken cancellationToken
   ) =>
      mutations.ReplacePrimaryEntityImageAsync(
         entityId,
         replacement,
         cancellationToken
      );

   public Task DeletePrimaryEntityImageAsync(
      Guid entityId,
      CancellationToken cancellationToken
   ) =>
      mutations.DeletePrimaryEntityImageAsync(entityId, cancellationToken);

   public Task SaveEntityAsync(
      EntityEditModel model,
      CancellationToken cancellationToken
   ) =>
      mutations.SaveEntityAsync(model, cancellationToken);

   public Task<bool> UpdateEntityBioAsync(
      Guid entityId,
      string bio,
      CancellationToken cancellationToken
   ) =>
      mutations.UpdateEntityBioAsync(entityId, bio, cancellationToken);

   public Task<bool> UpdateEntityPersonFactsAsync(
      Guid entityId,
      DateOnly? birthdate,
      int? height,
      int? weight,
      string? formativeClub,
      CancellationToken cancellationToken
   ) =>
      mutations.UpdateEntityPersonFactsAsync(
         entityId,
         birthdate,
         height,
         weight,
         formativeClub,
         cancellationToken
      );

   public Task<bool> AddEntityLinkAsync(
      Guid sourceEntityId,
      Guid targetEntityId,
      CancellationToken cancellationToken
   ) =>
      mutations.AddEntityLinkAsync(
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );

   public Task EnsureEntityLinksAsync(
      IReadOnlyCollection<Guid> sourceEntityIds,
      Guid targetEntityId,
      CancellationToken cancellationToken
   ) =>
      mutations.EnsureEntityLinksAsync(
         sourceEntityIds,
         targetEntityId,
         cancellationToken
      );

   public Task<bool> RemoveEntityLinkAsync(
      Guid sourceEntityId,
      Guid targetEntityId,
      CancellationToken cancellationToken
   ) =>
      mutations.RemoveEntityLinkAsync(
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );

   public Task DeleteEntityAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      mutations.DeleteEntityAsync(id, cancellationToken);

   public Task<bool> UpdateEntityWatchPriorityAsync(
      Guid id,
      string watchPriorityId,
      CancellationToken cancellationToken
   ) =>
      mutations.UpdateEntityWatchPriorityAsync(
         id,
         watchPriorityId,
         cancellationToken
      );
}
