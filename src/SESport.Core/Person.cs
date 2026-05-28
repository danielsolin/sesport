namespace SESport.Core;

public sealed record Person(
   string Name,
   IReadOnlyCollection<Country> Nationalities
);
