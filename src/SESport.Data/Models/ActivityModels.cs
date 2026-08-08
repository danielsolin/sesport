using SESport.Core.Domain;

namespace SESport.Data.Models;

public sealed record ActivityListItem(
   Guid Id,
   string Title,
   string? Description,
   string? Teaser,
   string ActivityType,
   string SportId,
   string SportName,
   string? SportIconPath,
   string TimeText,
   DateTimeOffset? StartsAt,
   string? TvChannelName,
   string PublicationStatus,
   string RelatedPersonEntities,
   Guid[] RelatedPersonEntityIds,
   string RelatedOrganizationEntities
)
{
   public DateOnly ActivityDate { get; init; }

   public TimeOnly? LocalStartTime { get; init; }

   public TimeOnly? LocalEndTime { get; init; }

   public DateTimeOffset? EndsAt { get; init; }

   public Guid? ActivityGroupId { get; init; }

   public string? ActivityGroupTitle { get; init; }

   public bool IsTeamSport { get; init; }

   public string RelatedOrganizationCanonicalEntities { get; init; } =
      string.Empty;

   public Guid[] ActiveRelatedPersonEntityIds { get; init; } = [];

   public bool HasNationalTeamRelatedOrganization { get; init; }

   public IReadOnlyList<PublicActivityParticipant> Participants
   {
      get;
      init;
   } = [];
}

public sealed record PublicActivityParticipant(
   Guid Id,
   string Name,
   string? StartTime,
   DateOnly? Birthdate,
   int? Height,
   string Club,
   bool IsActive,
   bool HasDiscipline,
   string? DisciplineAliasName
)
{
   public int? WatchPriority { get; init; }
}

public sealed record PublishedDateParticipantCount(
   DateOnly Date,
   int ParticipantCount
);

public sealed record EntityOption(
   Guid Id,
   string Name,
   string Type,
   string Sport,
   string Organization,
   string? PersonGenderId,
   string? AliasName = null
);

public sealed record LookupOption(string Id, string Label);

public sealed record ActivityGroupParticipant(
   Guid Id,
   string Name
);

public sealed class ActivityGroupEditModel
{
   public Guid Id { get; set; }

   public string Title { get; set; } = string.Empty;

   public string SportId { get; set; } = string.Empty;

   public DateOnly? StartDate { get; set; }

   public DateOnly? EndDate { get; set; }
}

public sealed record ActivityGroupActivityListItem(
   Guid Id,
   string Title,
   string? Description,
   DateOnly ActivityDate,
   TimeOnly? LocalStartTime,
   TimeOnly? LocalEndTime
);

public sealed record ActivityGroupSourceListItem(
   string Kind,
   string Url,
   string? Title,
   string? Excerpt,
   DateTimeOffset ObservedAt
);

public sealed record ActivityParticipantListItem(
   Guid Id,
   string Name,
   string RelatedOrganizations,
   string WatchPriority,
   string Gender,
   string Alias,
   bool IsActive
);

public sealed class ActivitySourceEditModel
{
   public Guid? Id { get; set; }

   public string Kind { get; set; } = string.Empty;

   public string Url { get; set; } = string.Empty;

   public string? Title { get; set; }

   public string? Excerpt { get; set; }

   public DateTimeOffset ObservedAt { get; set; }
}

public sealed class ActivityEditModel
{
   public Guid? ParticipationRunId { get; set; }

   public Guid? Id { get; set; }

   public string Title { get; set; } = string.Empty;

   public string? Description { get; set; }

   public string? Teaser { get; set; }

   public string ActivityType { get; set; } = string.Empty;

   public string SportId { get; set; } = string.Empty;

   public DateOnly? ActivityDate { get; set; }

   public TimeOnly? LocalStartTime { get; set; }

   public TimeOnly? LocalEndTime { get; set; }

   public string TimeZoneId { get; set; } = SportDay.TimeZoneId;

   public bool IsPublished { get; set; }

   public List<Guid> LinkedEntityIds { get; set; } = [];

   public Guid? OrganizationEntityId { get; set; }

   public List<ActivitySourceEditModel> Sources { get; set; } = [];

   public List<Guid> BroadcastIds { get; set; } = [];

   public Guid? ActivityGroupId { get; set; }

   public string? ActivityGroupTitle { get; set; }

   public bool ActivityGroupCreationRequired { get; set; }

   public string? TvChannelName { get; set; }
}
