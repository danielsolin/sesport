namespace SESport.Core;

public sealed record SportEvent(
   string Name,
   Competition Competition,
   DateTimeOffset StartsAt,
   string Stage,
   IReadOnlyCollection<Participant> Participants
)
{
   public IReadOnlyCollection<Relevance> GetRelevanceFor(Country country)
   {
      var relevance = new List<Relevance>();

      foreach (var participant in Participants)
      {
         if (participant.RepresentsCountry == country)
         {
            var reason = $"{participant.Name} represents {country.Name}.";

            relevance.Add(new Relevance(country, participant, null, reason));
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

            relevance.Add(
               new Relevance(
                  country,
                  participant,
                  membership.Person,
                  reason
               )
            );
         }
      }

      return relevance;
   }
}
