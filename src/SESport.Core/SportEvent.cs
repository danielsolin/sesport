namespace SESport.Core;

public sealed record SportEvent(
    string Name,
    Competition Competition,
    DateTimeOffset StartsAt,
    string Stage,
    IReadOnlyCollection<Participant> Participants
)
{
   public Relevance? GetRelevanceFor(Country country)
   {
      var participant = Participants.FirstOrDefault(
          candidate => candidate.RepresentsCountry == country);

      if(participant is null)
      {
         return null;
      }

      var reason = $"{participant.Name} represents {country.Name}.";

      return new Relevance(country, participant, reason);
   }
}
