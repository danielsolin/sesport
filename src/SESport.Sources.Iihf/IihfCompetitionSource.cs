namespace SESport.Sources.Iihf;

public sealed record IihfCompetitionSource(
   CompetitionId CompetitionId,
   string EventPath,
   Uri StatsUri
);
