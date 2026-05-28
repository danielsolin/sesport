namespace SESport.Core;

public sealed class EventQueryEvaluator
{
   public IReadOnlyCollection<SportEvent> Evaluate(
      IReadOnlyCollection<SportEvent> events,
      EventQuery query
   )
   {
      return events
         .Where(candidate => StartsWithinWindow(candidate, query))
         .Where(candidate => MatchesCompetition(candidate, query))
         .Where(
            candidate => MatchesCountryConnectionThreshold(candidate, query)
         )
         .ToList();
   }

   private static bool StartsWithinWindow(
      SportEvent candidate,
      EventQuery query
   )
   {
      return candidate.StartsAt >= query.StartsAfter &&
         candidate.StartsAt <= query.StartsBefore;
   }

   private static bool MatchesCompetition(
      SportEvent candidate,
      EventQuery query
   )
   {
      return query.Competition is null ||
         candidate.Competition == query.Competition;
   }

   private static bool MatchesCountryConnectionThreshold(
      SportEvent candidate,
      EventQuery query
   )
   {
      return candidate.Participants.Any(
         participant =>
            CountCountryConnections(participant, query.Country) >=
               query.MinimumCountryConnections
      );
   }

   private static int CountCountryConnections(
      Participant participant,
      Country country
   )
   {
      return participant.Roster.Count(
         membership => membership.Person.Nationalities.Contains(country)
      );
   }
}
