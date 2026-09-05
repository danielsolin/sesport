namespace SESport.Data.Models;

public sealed record BroadcastReadModel(
   Guid Id,
   Guid? ImportRunId,
   string SourceKey,
   string ExternalId,
   string Fingerprint,
   string ChannelId,
   string? ChannelName,
   string Title,
   string? Description,
   IReadOnlyList<string> Categories,
   bool IsReplay,
   DateOnly? OriginalAirDate,
   DateTimeOffset StartsAt,
   DateTimeOffset EndsAt,
   string TimeZoneId,
   string? RawProgrammeXml,
   string? ImageUrl,
   DateTimeOffset CreatedAt,
   DateTimeOffset UpdatedAt,
   DateTimeOffset? HiddenAt,
   DateTimeOffset? ProcessedAt,
   BroadcastReadOrganization? Organization,
   BroadcastReadActivityGroup? ActivityGroup,
   IReadOnlyList<BroadcastReadSource> Sources,
   IReadOnlyList<Guid> LinkedActivityIds
);

public sealed record BroadcastReadOrganization(
   Guid Id,
   string Name,
   string? SportId,
   string? SportName
);

public sealed record BroadcastReadActivityGroup(
   Guid? Id,
   string? Title,
   string? DraftTitle,
   string? SourceKindId,
   Guid? SourceActivityId
);

public sealed record BroadcastReadSource(
   string Kind,
   string Url,
   string? Title,
   string? Excerpt,
   DateTimeOffset ObservedAt,
   DateTimeOffset CreatedAt
);
