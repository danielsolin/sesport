namespace SESport.Core.Queries;

public sealed record EventQuery(
   Country Country,
   DateTimeOffset StartsAfter,
   DateTimeOffset StartsBefore,
   int MinimumCountryConnections,
   Competition? Competition
);
