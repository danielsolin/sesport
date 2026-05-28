namespace SESport.Core.Domain;

public sealed record Competition(
   CompetitionId Id,
   string Name,
   Sport Sport
);
