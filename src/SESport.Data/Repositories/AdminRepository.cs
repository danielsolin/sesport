using Npgsql;

using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class AdminRepository(NpgsqlDataSource dataSource)
{
   private readonly AdminReferenceRepository references = new(dataSource);
   private readonly EntityRepository entities = new(dataSource);
   private readonly EntityMergeRepository merges = new(dataSource);

   public IReadOnlyList<ReferenceTableInfo> GetReferenceTables() =>
      references.GetReferenceTables();

   public Task<ReferenceTableInfo?> GetReferenceTableInfoAsync(
      string tableKey,
      CancellationToken cancellationToken
   ) =>
      references.GetReferenceTableInfoAsync(tableKey, cancellationToken);

   public Task<IReadOnlyList<ReferenceRow>> GetReferenceRowsAsync(
      string tableKey,
      CancellationToken cancellationToken
   ) =>
      references.GetReferenceRowsAsync(tableKey, cancellationToken);

   public Task<IReadOnlyList<BroadcastIgnoreRuleListItem>>
      GetBroadcastIgnoreRulesAsync(CancellationToken cancellationToken) =>
      references.GetBroadcastIgnoreRulesAsync(cancellationToken);

   public Task<IReadOnlyList<CountryReferenceRow>>
      GetCountryReferenceRowsAsync(CancellationToken cancellationToken) =>
      references.GetCountryReferenceRowsAsync(cancellationToken);

   public Task<IReadOnlyList<SportReferenceRow>>
      GetSportReferenceRowsAsync(CancellationToken cancellationToken) =>
      references.GetSportReferenceRowsAsync(cancellationToken);

   public Task<ReferenceEditModel?> GetReferenceForEditAsync(
      string tableKey,
      string id,
      CancellationToken cancellationToken
   ) =>
      references.GetReferenceForEditAsync(
         tableKey,
         id,
         cancellationToken
      );

   public Task<CountryReferenceEditModel?> GetCountryForEditAsync(
      string id,
      CancellationToken cancellationToken
   ) =>
      references.GetCountryForEditAsync(id, cancellationToken);

   public Task<SportReferenceEditModel?> GetSportForEditAsync(
      string id,
      CancellationToken cancellationToken
   ) =>
      references.GetSportForEditAsync(id, cancellationToken);

   public Task<BroadcastIgnoreRuleEditModel?>
      GetBroadcastIgnoreRuleForEditAsync(
         string kind,
         string value,
         string? sourceKey,
         CancellationToken cancellationToken
      ) =>
      references.GetBroadcastIgnoreRuleForEditAsync(
         kind,
         value,
         sourceKey,
         cancellationToken
      );

   public Task SaveReferenceAsync(
      string tableKey,
      ReferenceEditModel model,
      CancellationToken cancellationToken
   ) =>
      references.SaveReferenceAsync(tableKey, model, cancellationToken);

   public Task SaveCountryAsync(
      CountryReferenceEditModel model,
      CancellationToken cancellationToken
   ) =>
      references.SaveCountryAsync(model, cancellationToken);

   public Task SaveSportAsync(
      SportReferenceEditModel model,
      CancellationToken cancellationToken
   ) =>
      references.SaveSportAsync(model, cancellationToken);

   public Task SaveBroadcastIgnoreRuleAsync(
      BroadcastIgnoreRuleEditModel model,
      CancellationToken cancellationToken
   ) =>
      references.SaveBroadcastIgnoreRuleAsync(model, cancellationToken);

   public Task DeleteReferenceAsync(
      string tableKey,
      string id,
      CancellationToken cancellationToken
   ) =>
      references.DeleteReferenceAsync(tableKey, id, cancellationToken);

   public Task DeleteCountryAsync(
      string id,
      CancellationToken cancellationToken
   ) =>
      references.DeleteCountryAsync(id, cancellationToken);

   public Task DeleteSportAsync(
      string id,
      CancellationToken cancellationToken
   ) =>
      references.DeleteSportAsync(id, cancellationToken);

   public Task DeleteBroadcastIgnoreRuleAsync(
      string kind,
      string value,
      string? sourceKey,
      CancellationToken cancellationToken
   ) =>
      references.DeleteBroadcastIgnoreRuleAsync(
         kind,
         value,
         sourceKey,
         cancellationToken
      );

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
      entities.SearchEntitiesAsync(
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
      entities.GetEntitiesAsync(
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
      entities.GetEntityLinkOptionsByIdsAsync(
         ids,
         excludeEntityId,
         cancellationToken
      );

   public Task<EntityEditModel?> GetEntityForEditAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      entities.GetEntityForEditAsync(id, cancellationToken);

   public Task<EntityPrimaryThumbnail?> GetEntityPrimaryThumbnailAsync(
      Guid entityId,
      CancellationToken cancellationToken
   ) =>
      entities.GetEntityPrimaryThumbnailAsync(entityId, cancellationToken);

   public Task ReplacePrimaryEntityImageAsync(
      Guid entityId,
      EntityImageReplacement replacement,
      CancellationToken cancellationToken
   ) =>
      entities.ReplacePrimaryEntityImageAsync(
         entityId,
         replacement,
         cancellationToken
      );

   public Task DeletePrimaryEntityImageAsync(
      Guid entityId,
      CancellationToken cancellationToken
   ) =>
      entities.DeletePrimaryEntityImageAsync(entityId, cancellationToken);

   public Task<IReadOnlyList<EntityActivityListItem>> GetEntityActivitiesAsync(
      Guid entityId,
      CancellationToken cancellationToken
   ) =>
      entities.GetEntityActivitiesAsync(entityId, cancellationToken);

   public Task<EntityEditModel?> GetEntityCloneTemplateAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      entities.GetEntityCloneTemplateAsync(id, cancellationToken);

   public Task<IReadOnlyList<EntityLinkOption>> GetEntityLinkOptionsAsync(
      Guid? excludeEntityId,
      CancellationToken cancellationToken
   ) =>
      entities.GetEntityLinkOptionsAsync(excludeEntityId, cancellationToken);

   public Task<IReadOnlyList<EntityLinkOption>>
      GetOrganizationEntityOptionsAsync(
         CancellationToken cancellationToken,
         string? sportId = null
      ) =>
      entities.GetOrganizationEntityOptionsAsync(
         cancellationToken,
         sportId
      );

   public Task<IReadOnlyList<EntityLinkOption>>
      GetBroadcastOrganizationLinkOptionsAsync(
         CancellationToken cancellationToken
      ) =>
      entities.GetBroadcastOrganizationLinkOptionsAsync(cancellationToken);

   public Task<IReadOnlyList<EntityLinkOption>>
      SearchBroadcastOrganizationLinkOptionsAsync(
         string term,
         CancellationToken cancellationToken
      ) =>
      entities.SearchBroadcastOrganizationLinkOptionsAsync(
         term,
         cancellationToken
      );

   public Task<IReadOnlyList<EntityNameOption>>
      GetPersonEntityNameOptionsAsync(
         CancellationToken cancellationToken
      ) =>
      entities.GetPersonEntityNameOptionsAsync(cancellationToken);

   public Task<IReadOnlyList<EntityNameOption>>
      GetPersonEntityNameOptionsAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      ) =>
      entities.GetPersonEntityNameOptionsAsync(
         organizationEntityId,
         cancellationToken
      );

   public Task<IReadOnlyList<EntityNameOption>>
      GetParticipantEntityNameOptionsAsync(
         CancellationToken cancellationToken
      ) =>
      entities.GetParticipantEntityNameOptionsAsync(cancellationToken);

   public Task<IReadOnlyList<EntityNameOption>>
      GetParticipantEntityNameOptionsAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      ) =>
      entities.GetParticipantEntityNameOptionsAsync(
         organizationEntityId,
         cancellationToken
      );

   public Task<IReadOnlyList<EntityNameOption>>
      GetBroadcastParticipantEntityNameOptionsAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      ) =>
      entities.GetBroadcastParticipantEntityNameOptionsAsync(
         organizationEntityId,
         cancellationToken
      );

   public Task<IReadOnlyList<LookupOption>> GetCountryOptionsAsync(
      CancellationToken cancellationToken
   ) =>
      entities.GetCountryOptionsAsync(cancellationToken);

   public Task<IReadOnlyList<LookupOption>> GetPersonGenderOptionsAsync(
      CancellationToken cancellationToken
   ) =>
      entities.GetPersonGenderOptionsAsync(cancellationToken);

   public Task SaveEntityAsync(
      EntityEditModel model,
      CancellationToken cancellationToken
   ) =>
      entities.SaveEntityAsync(model, cancellationToken);

   public Task<bool> UpdateEntityBioAsync(
      Guid entityId,
      string bio,
      CancellationToken cancellationToken
   ) =>
      entities.UpdateEntityBioAsync(entityId, bio, cancellationToken);

   public Task<bool> UpdateEntityPersonFactsAsync(
      Guid entityId,
      DateOnly? birthdate,
      int? height,
      int? weight,
      string? formativeClub,
      CancellationToken cancellationToken
   ) =>
      entities.UpdateEntityPersonFactsAsync(
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
      entities.AddEntityLinkAsync(
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );

   public Task EnsureEntityLinksAsync(
      IReadOnlyCollection<Guid> sourceEntityIds,
      Guid targetEntityId,
      CancellationToken cancellationToken
   ) =>
      entities.EnsureEntityLinksAsync(
         sourceEntityIds,
         targetEntityId,
         cancellationToken
      );

   public Task<bool> RemoveEntityLinkAsync(
      Guid sourceEntityId,
      Guid targetEntityId,
      CancellationToken cancellationToken
   ) =>
      entities.RemoveEntityLinkAsync(
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );

   public Task DeleteEntityAsync(
      Guid id,
      CancellationToken cancellationToken
   ) =>
      entities.DeleteEntityAsync(id, cancellationToken);

   public Task<bool> UpdateEntityWatchPriorityAsync(
      Guid id,
      string watchPriorityId,
      CancellationToken cancellationToken
   ) =>
      entities.UpdateEntityWatchPriorityAsync(
         id,
         watchPriorityId,
         cancellationToken
      );

   public Task<EntityMergePreview?> GetEntityMergePreviewAsync(
      Guid sourceEntityId,
      Guid targetEntityId,
      CancellationToken cancellationToken
   ) =>
      merges.GetEntityMergePreviewAsync(
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );

   public Task<EntityMergeResult> MergeEntityAsync(
      Guid sourceEntityId,
      Guid targetEntityId,
      CancellationToken cancellationToken
   ) =>
      merges.MergeEntityAsync(
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );

   internal static string GetOtherSideEntityIdSql(string entityIdSql) =>
      EntityQueryRepository.GetOtherSideEntityIdSql(entityIdSql);
}
