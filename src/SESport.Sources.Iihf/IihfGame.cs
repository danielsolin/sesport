namespace SESport.Sources.Iihf;

public sealed record IihfGame(
   string ExternalId,
   string CompetitionExternalId,
   string CompetitionName,
   DateTimeOffset StartsAt,
   string Stage,
   IihfTeam? HomeTeam,
   IihfTeam? AwayTeam
);
