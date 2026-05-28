namespace SESport.Core.Domain;

public sealed record Competition(
   CompetitionId Id,
   string Name,
   Sport Sport,
   CompetitionStatus Status
)
{
   public Competition(
      CompetitionId id,
      string name,
      Sport sport
   ) : this(id, name, sport, CompetitionStatus.Unknown)
   {
   }
}
