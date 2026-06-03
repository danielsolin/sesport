namespace SESport.Core.TvSport;

public sealed record TvSportBroadcast(
   Guid Id,
   string SourceKey,
   string ExternalId,
   string Fingerprint,
   string ChannelId,
   string? ChannelName,
   string Title,
   string? Description,
   IReadOnlyCollection<string> Categories,
   DateTimeOffset StartsAt,
   DateTimeOffset EndsAt,
   string TimeZoneId,
   string? RawProgrammeXml
);
