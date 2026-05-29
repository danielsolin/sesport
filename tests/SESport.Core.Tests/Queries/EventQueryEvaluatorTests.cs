namespace SESport.Core.Tests.Queries;

public class EventQueryEvaluatorTests
{
   private static readonly Country Sweden =
      new(new CountryId("country:se"), "SE", "Sweden");

   private static readonly Sport IceHockey =
      new(new SportId("sport:ice-hockey"), "Ice hockey");

   private static readonly Competition NhlSeason = new(
      new CompetitionId("competition:nhl-2025-2026"),
      "NHL 2025-2026 season",
      IceHockey
   );

   [Fact]
   public void QueryCanFindUpcomingNhlGameWithFourSwedishPlayers()
   {
      var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
      var matchingEvent = CreateNhlGame(
         new EventId("event:nhl-game-with-four-swedes"),
         "Team A vs Team B",
         now.AddHours(24),
         4
      );
      var lowRosterEvent = CreateNhlGame(
         new EventId("event:nhl-game-with-one-swede"),
         "Team C vs Team D",
         now.AddHours(24),
         1
      );
      var outsideWindowEvent = CreateNhlGame(
         new EventId("event:nhl-game-after-window"),
         "Team E vs Team F",
         now.AddHours(72),
         5
      );
      var query = new EventQuery(
         Sweden,
         now,
         now.AddHours(48),
         4,
         NhlSeason
      );
      var evaluator = new EventQueryEvaluator();

      var matches = evaluator.Evaluate(
         [matchingEvent, lowRosterEvent, outsideWindowEvent],
         query
      );

      Assert.Single(matches);
      Assert.Equal(matchingEvent, matches.Single());
   }

   private static SportEvent CreateNhlGame(
      EventId id,
      string name,
      DateTimeOffset startsAt,
      int swedishPlayers
   )
   {
      return new SportEvent(
         id,
         name,
         NhlSeason,
         startsAt,
         "Regular season",
         [
            CreateClubTeam(
               new ParticipantId($"{id.Value}:home"),
               "Home team",
               swedishPlayers
            ),
            CreateClubTeam(
               new ParticipantId($"{id.Value}:away"),
               "Away team",
               0
            )
         ]
      );
   }

   private static Participant CreateClubTeam(
      ParticipantId id,
      string name,
      int swedishPlayers
   )
   {
      return new Participant(
         id,
         name,
         ParticipantKind.ClubTeam,
         null,
         CreateRoster(swedishPlayers)
      );
   }

   private static IReadOnlyCollection<RosterMembership> CreateRoster(
      int swedishPlayers
   )
   {
      return Enumerable
         .Range(1, swedishPlayers)
         .Select(index => new RosterMembership(
            new Person(
               new PersonId($"person:swedish-player-{index}"),
               $"Swedish Player {index}",
               [Sweden]
            ),
            "player"
         ))
         .ToList();
   }
}
