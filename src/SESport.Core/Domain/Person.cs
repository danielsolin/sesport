namespace SESport.Core.Domain;

public sealed record Person(
   PersonId Id,
   string Name,
   IReadOnlyCollection<Country> Nationalities
);
