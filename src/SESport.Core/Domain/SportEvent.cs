namespace SESport.Core.Domain;

public sealed record SportEvent(
   EventId Id,
   string Name,
   Competition Competition,
   DateTimeOffset StartsAt,
   string Stage,
   IReadOnlyCollection<Participant> Participants
)
{
   public IReadOnlyCollection<CountryConnection> GetCountryConnectionsFor(
      Country country
   )
   {
      var connections = new List<CountryConnection>();

      foreach (var participant in Participants)
      {
         if (participant.RepresentsCountry == country)
         {
            var reason = $"{participant.Name} represents {country.Name}.";

            connections.Add(
               new CountryConnection(
                  country,
                  participant,
                  null,
                  CountryConnectionKind.ParticipantRepresentsCountry,
                  reason
               )
            );
         }

         foreach (var membership in participant.Roster)
         {
            if (!membership.Person.Nationalities.Contains(country))
            {
               continue;
            }

            var reason =
               $"{membership.Person.Name} is a {country.Name} " +
               $"{membership.Role} on {participant.Name}.";

            connections.Add(
               new CountryConnection(
                  country,
                  participant,
                  membership.Person,
                  CountryConnectionKind.RosterMemberNationality,
                  reason
               )
            );
         }
      }

      return connections;
   }
}
