namespace SESport.Tools.AIActivitySearch.Watchlist;

internal sealed record EntityWatchlistDocument(
   int SchemaVersion,
   IReadOnlyCollection<EntityWatchlistEntity> Entities
);

internal sealed record EntityWatchlistEntity(
   string Id,
   string Name,
   string Type,
   EntityWatchlistSport Sport,
   string SwedenConnection,
   string? CurrentRelationshipOrStatus,
   IReadOnlyCollection<string> LikelyActivityTypes,
   string? SuggestedEvidenceSources,
   string? Notes
);

internal sealed record EntityWatchlistSport(
   string Id,
   string Name
);
