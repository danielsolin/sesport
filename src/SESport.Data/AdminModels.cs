namespace SESport.Data;

public enum ReferenceTableKind
{
   Lookup,
   Countries,
   Sports
}

public sealed record ReferenceTableInfo(
   string Id,
   string Title,
   string Description,
   ReferenceTableKind Kind = ReferenceTableKind.Lookup
);

public sealed record ReferenceRow(
   string Id,
   string Label,
   int? SortOrder,
   bool? IsActive
);

public sealed record BroadcastIgnoreRuleListItem(
   string Kind,
   string Value,
   string? SourceKey,
   string? Reason,
   bool IsActive
);

public sealed class ReferenceEditModel
{
   public string? OriginalId { get; set; }

   public string Id { get; set; } = string.Empty;

   public string Label { get; set; } = string.Empty;

   public int? SortOrder { get; set; }

   public bool IsActive { get; set; } = true;
}

public sealed class BroadcastIgnoreRuleEditModel
{
   public string? OriginalKind { get; set; }

   public string? OriginalValue { get; set; }

   public string? OriginalSourceKey { get; set; }

   public string Kind { get; set; } = string.Empty;

   public string Value { get; set; } = string.Empty;

   public string? SourceKey { get; set; }

   public string? Reason { get; set; }

   public bool IsActive { get; set; } = true;
}

public sealed record CountryReferenceRow(
   string Id,
   string Code,
   string Name
);

public sealed record SportReferenceRow(
   string Id,
   string Name,
   string? DisplayName,
   string? IconId
);

public sealed class CountryReferenceEditModel
{
   public string? OriginalId { get; set; }

   public string Id { get; set; } = string.Empty;

   public string Code { get; set; } = string.Empty;

   public string Name { get; set; } = string.Empty;
}

public sealed class SportReferenceEditModel
{
   public string? OriginalId { get; set; }

   public string Id { get; set; } = string.Empty;

   public string Name { get; set; } = string.Empty;

   public string? DisplayName { get; set; }

   public string IconId { get; set; } = string.Empty;
}

public sealed record EntityListItem(
   Guid Id,
   string Name,
   string EntityType,
   string Sport,
   string WatchPriorityId,
   string WatchPriority,
   string Country,
   string RelatedEntityNames
);

public sealed record EntityLinkOption(
   Guid Id,
   string Name,
   string EntityType,
   string Sport
);

public sealed record EntityNameOption(
   Guid Id,
   string Name
);

public sealed record EntityActivityListItem(
   Guid Id,
   DateOnly ActivityDate,
   TimeOnly? LocalStartTime,
   string Organization,
   string Title,
   string Sport,
   string ActivityType,
   string PublicationStatus
);

public sealed record EntityMergeEntitySummary(
   Guid Id,
   string CanonicalName,
   string EntityTypeId,
   string SportId,
   string CountryId,
   string WatchPriorityId,
   string ExpectedStabilityId,
   string? PersonGenderId,
   string? AliasName
);

public sealed record EntityMergeReferenceCount(
   string Label,
   int Count
);

public sealed record EntityMergeLinkPreview(
   string RelatedEntityName,
   string RelatedEntityType,
   string Action
);

public sealed record EntityMergePreview(
   EntityMergeEntitySummary Source,
   EntityMergeEntitySummary Target,
   IReadOnlyList<EntityMergeReferenceCount> ReferenceCounts,
   IReadOnlyList<EntityMergeLinkPreview> LinkPreviews
);

public sealed record EntityMergeResult(
   int ActivityEntityLinksMoved,
   int ActivityOrganizationLinksMoved,
   int BroadcastsMoved,
   int DuplicateActivityEntityLinksDeleted,
   int DuplicateEntityLinksDeleted,
   int EntityLinksMoved
);

public sealed class EntityEditModel
{
   public Guid? Id { get; set; }

   public string CanonicalName { get; set; } = string.Empty;

   public string EntityTypeId { get; set; } = string.Empty;

   public string SportId { get; set; } = string.Empty;

   public string CountryId { get; set; } = string.Empty;

   public string CountryRelevanceKindId { get; set; } = string.Empty;

   public string CountryRelevanceReason { get; set; } = string.Empty;

   public string WatchPriorityId { get; set; } = string.Empty;

   public string ExpectedStabilityId { get; set; } = string.Empty;

   public string? AliasName { get; set; }

   public string? Bio { get; set; }

   public DateOnly? Birthdate { get; set; }

   public int? Height { get; set; }

   public int? Weight { get; set; }

   public string? PersonGenderId { get; set; }

   public List<Guid> LinkedEntityIds { get; set; } = [];
}
