namespace SESport.Data.Models;

public sealed record ActivitySearchPage(
   IReadOnlyList<ActivitySearchReadModel> Results,
   bool HasMore
);

public sealed record ActivitySearchReadModel(
   Guid Id,
   string Title,
   string SportId,
   string SportName,
   string ActivityTypeId,
   string ActivityTypeName,
   DateOnly ActivityDate,
   TimeOnly? LocalStartTime,
   DateTimeOffset? StartsAt,
   IReadOnlyList<string> ParticipantNames
);

public sealed record ActivityReadModel(
   Guid Id,
   string Title,
   string? Description,
   string SportId,
   string SportName,
   string ActivityTypeId,
   string ActivityTypeName,
   DateOnly ActivityDate,
   TimeOnly? LocalStartTime,
   TimeOnly? LocalEndTime,
   DateTimeOffset? StartsAt,
   DateTimeOffset? EndsAt,
   string TimeZoneId,
   ActivityReadGroup? ActivityGroup,
   ActivityReadOrganization? Organization,
   IReadOnlyList<ActivityReadParticipant> Participants
);

public sealed record ActivityReadGroup(
   Guid Id,
   string Title
);

public sealed record ActivityReadOrganization(
   Guid Id,
   string Name
);

public sealed record ActivityReadParticipant(
   Guid Id,
   string Name,
   DateOnly? BirthDate,
   string? FormativeClub,
   string? StartTime
);
