namespace SESport.Core.Broadcast;

public sealed record Broadcast(
   Guid Id,
   string SourceKey,
   string ExternalId,
   string Fingerprint,
   string ChannelId,
   string? ChannelName,
   string Title,
   string? Description,
   IReadOnlyCollection<string> Categories,
   bool IsReplay,
   DateOnly? OriginalAirDate,
   DateTimeOffset StartsAt,
   DateTimeOffset EndsAt,
   string TimeZoneId,
   string? RawProgrammeXml
);
