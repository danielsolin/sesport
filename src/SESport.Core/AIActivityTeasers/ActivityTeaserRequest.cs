namespace SESport.Core.AIActivityTeasers;

public sealed record ActivityTeaserRequest(
   string Title,
   string? Description,
   string ActivityType,
   string Sport,
   DateOnly? ActivityDate,
   TimeOnly? LocalStartTime,
   string TimeZoneId,
   IReadOnlyCollection<string> Entities,
   IReadOnlyCollection<string> RelatedEntities
);
