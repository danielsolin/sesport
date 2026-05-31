namespace SESport.Core.Domain;

public sealed record Activity(
   ActivityId Id,
   string Title,
   string? Description,
   ActivityType Type,
   Sport Sport,
   string? Context,
   ActivityTime Time,
   IReadOnlyCollection<ActivityEntityLink> EntityLinks,
   IReadOnlyCollection<ActivityEvidence> Evidence,
   string CountryRelevanceExplanation
);
