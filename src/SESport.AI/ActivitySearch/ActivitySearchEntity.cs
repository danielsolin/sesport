using SESport.Core.Ingestion;

namespace SESport.AI.ActivitySearch;

public sealed record ActivitySearchEntity(
   ExternalEntityId WatchlistId,
   string Name,
   string Type,
   ImportedSport Sport,
   string SwedenConnection,
   string? CurrentRelationshipOrStatus,
   IReadOnlyCollection<string> LikelyActivityTypes,
   string? SuggestedEvidenceSources,
   string? Notes,
   string Country = "Sweden"
);
