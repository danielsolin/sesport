namespace SESport.Core;

public sealed record Person(
   PersonId Id,
   string Name,
   IReadOnlyCollection<Country> Nationalities
);
