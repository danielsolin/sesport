using SESport.Core.Domain;

namespace SESport.Data;

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

   public IReadOnlyList<PublicActivityParticipant> Participants
   {
      get;
      init;
   } = [];
}

public sealed record PublicActivityParticipant(
   Guid Id,
   string Name,
   DateOnly? Birthdate,
   int? Height,
   string Club
);

public sealed record EntityOption(
   Guid Id,
   string Name,
   string Type,
   string Sport,
   string Organization,
   int WatchPrioritySortOrder,
   string? PersonGenderId,
   string? AliasName = null
);

public sealed record LookupOption(string Id, string Label);

public sealed record ActivityGroupParticipant(
   Guid Id,
   string Name
);

public sealed record ActivityParticipantListItem(
   Guid Id,
   string Name,
   string RelatedOrganizations,
   string WatchPriority,
   string Gender,
   string Alias
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
   public Guid? Id { get; set; }

   public string Title { get; set; } = string.Empty;

   public string? Description { get; set; }

   public string? Teaser { get; set; }

   public string? Facts { get; set; }

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
