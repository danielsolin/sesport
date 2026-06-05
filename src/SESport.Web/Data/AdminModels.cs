namespace SESport.Web.Data;

public sealed record AdminArea(string Title, string Description, string Href);

public sealed record AdminNavItem(string Title, string Href);

public sealed record AdminNavGroup(
   string Title,
   IReadOnlyList<AdminNavItem> Items
);

public enum ReferenceTableKind
{
   Lookup,
   ActivityAudit,
   Countries,
   Sports
}

public sealed record ReferenceTableInfo(
   string Id,
   string Title,
   string Description,
   ReferenceTableKind Kind = ReferenceTableKind.Lookup
);

public sealed record ReferenceNavigationItem(
   string Title,
   string Href
);

public sealed record ReferenceRow(
   string Id,
   string Label,
   int? SortOrder,
   bool? IsActive
);

public sealed class ReferenceEditModel
{
   public string? OriginalId { get; set; }

   public string Id { get; set; } = string.Empty;

   public string Label { get; set; } = string.Empty;

   public int? SortOrder { get; set; }

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

   public string IconId { get; set; } = string.Empty;
}

public sealed record SourceListItem(
   string Id,
   string Name,
   DateTimeOffset UpdatedAt
);

public sealed class SourceEditModel
{
   public string? OriginalId { get; set; }

   public string Id { get; set; } = string.Empty;

   public string Name { get; set; } = string.Empty;
}

public sealed record EntityListItem(
   Guid Id,
   string Name,
   string EntityType,
   string Sport,
   string WatchPriority,
   string Country,
   int LinkedEntityCount
);

public sealed record EntityLinkOption(
   Guid Id,
   string Name,
   string EntityType,
   string Sport
);

public sealed class EntityEditModel
{
   public Guid? Id { get; set; }

   public string CanonicalName { get; set; } = string.Empty;

   public string EntityTypeId { get; set; } = "Person";

   public string SportId { get; set; } = "football";

   public string CountryId { get; set; } = "se";

   public string CountryRelevanceKindId { get; set; } =
      "NationalityOrSportingIdentity";

   public string CountryRelevanceReason { get; set; } = string.Empty;

   public string WatchPriorityId { get; set; } = "review";

   public string ExpectedStabilityId { get; set; } = "medium_term";

   public List<Guid> LinkedEntityIds { get; set; } = [];
}
