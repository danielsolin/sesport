namespace SESport.MCP.Models;

public sealed record DbBroadcastGetResponse(
   [property: JsonPropertyName("found")]
   bool Found,
   [property: JsonPropertyName("broadcast")]
   DbBroadcastDetails? Broadcast
);

public sealed record DbBroadcastUpdateResponse(
   [property: JsonPropertyName("updated")]
   bool Updated,
   [property: JsonPropertyName("processed")]
   bool Processed,
   [property: JsonPropertyName("broadcast")]
   DbBroadcastDetails? Broadcast
);

public sealed record DbBroadcastDetails(
   [property: JsonPropertyName("id")]
   Guid Id,
   [property: JsonPropertyName("importRunId")]
   Guid? ImportRunId,
   [property: JsonPropertyName("sourceKey")]
   string SourceKey,
   [property: JsonPropertyName("externalId")]
   string ExternalId,
   [property: JsonPropertyName("fingerprint")]
   string Fingerprint,
   [property: JsonPropertyName("channelId")]
   string ChannelId,
   [property: JsonPropertyName("channelName")]
   string? ChannelName,
   [property: JsonPropertyName("title")]
   string Title,
   [property: JsonPropertyName("description")]
   string? Description,
   [property: JsonPropertyName("categories")]
   IReadOnlyList<string> Categories,
   [property: JsonPropertyName("isReplay")]
   bool IsReplay,
   [property: JsonPropertyName("originalAirDate")]
   DateOnly? OriginalAirDate,
   [property: JsonPropertyName("startsAt")]
   DateTimeOffset StartsAt,
   [property: JsonPropertyName("endsAt")]
   DateTimeOffset EndsAt,
   [property: JsonPropertyName("timeZoneId")]
   string TimeZoneId,
   [property: JsonPropertyName("rawProgrammeXml")]
   string? RawProgrammeXml,
   [property: JsonPropertyName("imageUrl")]
   string? ImageUrl,
   [property: JsonPropertyName("createdAt")]
   DateTimeOffset CreatedAt,
   [property: JsonPropertyName("updatedAt")]
   DateTimeOffset UpdatedAt,
   [property: JsonPropertyName("hiddenAt")]
   DateTimeOffset? HiddenAt,
   [property: JsonPropertyName("processedAt")]
   DateTimeOffset? ProcessedAt,
   [property: JsonPropertyName("organization")]
   DbBroadcastOrganization? Organization,
   [property: JsonPropertyName("activityGroup")]
   DbBroadcastActivityGroup? ActivityGroup,
   [property: JsonPropertyName("sources")]
   IReadOnlyList<DbBroadcastSource> Sources,
   [property: JsonPropertyName("linkedActivityIds")]
   IReadOnlyList<Guid> LinkedActivityIds
);

public sealed record DbBroadcastOrganization(
   [property: JsonPropertyName("id")]
   Guid Id,
   [property: JsonPropertyName("name")]
   string Name,
   [property: JsonPropertyName("sportId")]
   string? SportId,
   [property: JsonPropertyName("sportName")]
   string? SportName
);

public sealed record DbBroadcastActivityGroup(
   [property: JsonPropertyName("id")]
   Guid? Id,
   [property: JsonPropertyName("title")]
   string? Title,
   [property: JsonPropertyName("draftTitle")]
   string? DraftTitle,
   [property: JsonPropertyName("sourceKindId")]
   string? SourceKindId,
   [property: JsonPropertyName("sourceActivityId")]
   Guid? SourceActivityId
);

public sealed record DbBroadcastSource(
   [property: JsonPropertyName("kind")]
   string Kind,
   [property: JsonPropertyName("url")]
   string Url,
   [property: JsonPropertyName("title")]
   string? Title,
   [property: JsonPropertyName("excerpt")]
   string? Excerpt,
   [property: JsonPropertyName("observedAt")]
   DateTimeOffset ObservedAt,
   [property: JsonPropertyName("createdAt")]
   DateTimeOffset CreatedAt
);
