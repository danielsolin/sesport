namespace SESport.Core;

public sealed record Participant(
   string Name,
   ParticipantKind Kind,
   Country? RepresentsCountry,
   IReadOnlyCollection<RosterMembership> Roster
)
{
   public Participant(
      string name,
      ParticipantKind kind,
      Country? representsCountry
   ) : this(name, kind, representsCountry, [])
   {
   }
}
