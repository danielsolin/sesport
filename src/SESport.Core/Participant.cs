namespace SESport.Core;

public sealed record Participant(
   ParticipantId Id,
   string Name,
   ParticipantKind Kind,
   Country? RepresentsCountry,
   IReadOnlyCollection<RosterMembership> Roster
)
{
   public Participant(
      ParticipantId id,
      string name,
      ParticipantKind kind,
      Country? representsCountry
   ) : this(id, name, kind, representsCountry, [])
   {
   }
}
