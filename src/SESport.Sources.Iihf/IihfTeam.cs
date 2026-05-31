namespace SESport.Sources.Iihf;

public sealed record IihfTeam(
   string ExternalId,
   string CountryCode,
   string CountryName,
   string TeamName
);
