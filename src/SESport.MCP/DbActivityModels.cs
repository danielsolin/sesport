namespace SESport.MCP;

public sealed record DbActivitySearchResponse(
   [property: JsonPropertyName("results")]
   IReadOnlyList<DbActivitySearchResult> Results,
   [property: JsonPropertyName("hasMore")]
   bool HasMore
);

public sealed record DbActivitySearchResult(
   [property: JsonPropertyName("id")]
   Guid Id,
   [property: JsonPropertyName("title")]
   string Title,
   [property: JsonPropertyName("sport")]
   DbActivityLookup Sport,
   [property: JsonPropertyName("activityType")]
   DbActivityLookup ActivityType,
   [property: JsonPropertyName("activityDate")]
   DateOnly ActivityDate,
   [property: JsonPropertyName("startTime")]
   string? StartTime,
   [property: JsonPropertyName("participantNames")]
   IReadOnlyList<string> ParticipantNames
);

public sealed record DbActivityGetResponse(
   [property: JsonPropertyName("found")]
   bool Found,
   [property: JsonPropertyName("activity")]
   DbActivityDetails? Activity
);

public sealed record DbActivityDetails(
   [property: JsonPropertyName("id")]
   Guid Id,
   [property: JsonPropertyName("title")]
   string Title,
   [property: JsonPropertyName("description")]
   string? Description,
   [property: JsonPropertyName("sport")]
   DbActivityLookup Sport,
   [property: JsonPropertyName("activityType")]
   DbActivityLookup ActivityType,
   [property: JsonPropertyName("activityDate")]
   DateOnly ActivityDate,
   [property: JsonPropertyName("localStartTime")]
   string? LocalStartTime,
   [property: JsonPropertyName("localEndTime")]
   string? LocalEndTime,
   [property: JsonPropertyName("startsAt")]
   DateTimeOffset? StartsAt,
   [property: JsonPropertyName("endsAt")]
   DateTimeOffset? EndsAt,
   [property: JsonPropertyName("timeZoneId")]
   string TimeZoneId,
   [property: JsonPropertyName("activityGroup")]
   DbActivityGroup? ActivityGroup,
   [property: JsonPropertyName("organization")]
   DbActivityOrganization? Organization,
   [property: JsonPropertyName("participants")]
   IReadOnlyList<DbActivityParticipant> Participants
);

public sealed record DbActivityLookup(
   [property: JsonPropertyName("id")]
   string Id,
   [property: JsonPropertyName("name")]
   string Name
);

public sealed record DbActivityGroup(
   [property: JsonPropertyName("id")]
   Guid Id,
   [property: JsonPropertyName("title")]
   string Title
);

public sealed record DbActivityOrganization(
   [property: JsonPropertyName("id")]
   Guid Id,
   [property: JsonPropertyName("name")]
   string Name
);

public sealed record DbActivityParticipant(
   [property: JsonPropertyName("id")]
   Guid Id,
   [property: JsonPropertyName("name")]
   string Name,
   [property: JsonPropertyName("birthDate")]
   DateOnly? BirthDate,
   [property: JsonPropertyName("formativeClub")]
   string? FormativeClub,
   [property: JsonPropertyName("startTime")]
   string? StartTime
);
