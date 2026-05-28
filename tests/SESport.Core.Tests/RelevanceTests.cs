namespace SESport.Core.Tests;

public class RelevanceTests
{
   private static readonly Country Sweden =
      new(new CountryId("country:se"), "SE", "Sweden");

   private static readonly Country Switzerland =
      new(new CountryId("country:ch"), "CH", "Switzerland");

   [Fact]
   public void SwedenVsSwitzerlandIsRelevantToSweden()
   {
      var game = CreateSwedenVsSwitzerlandGame();

      var relevance = game.GetRelevanceFor(Sweden).Single();

      Assert.Equal(Sweden, relevance.Country);
      Assert.Null(relevance.Person);
      Assert.Equal(
         "Sweden men's national ice hockey team represents Sweden.",
         relevance.Reason
      );
   }

   [Fact]
   public void SwedenVsSwitzerlandIsNotRelevantToFinland()
   {
      var finland = new Country(new CountryId("country:fi"), "FI", "Finland");
      var game = CreateSwedenVsSwitzerlandGame();

      var relevance = game.GetRelevanceFor(finland);

      Assert.Empty(relevance);
   }

   [Fact]
   public void ClubGameIsRelevantWhenTeamHasSwedishRosterMember()
   {
      var game = CreateVegasVsFloridaGame();

      var relevance = game.GetRelevanceFor(Sweden).Single();

      Assert.Equal(Sweden, relevance.Country);
      Assert.Equal(
         "Las Vegas Golden Knights",
         relevance.EventParticipant.Name
      );
      Assert.Equal("William Karlsson", relevance.Person?.Name);
      Assert.Equal(
         "William Karlsson is a Sweden player on Las Vegas Golden Knights.",
         relevance.Reason
      );
   }

   private static SportEvent CreateSwedenVsSwitzerlandGame()
   {
      var iceHockey = new Sport(new SportId("sport:ice-hockey"), "Ice hockey");
      var competition = new Competition(
         new CompetitionId("competition:iihf-world-championship-2026"),
         "2026 IIHF Ice Hockey World Championship",
         iceHockey
      );

      return new SportEvent(
         new EventId("event:iihf-2026-sweden-switzerland"),
         "Sweden vs Switzerland",
         competition,
         new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.FromHours(2)),
         "Quarter-final",
         [
            new Participant(
               new ParticipantId("participant:sweden-mens-ice-hockey"),
               "Sweden men's national ice hockey team",
               ParticipantKind.NationalTeam,
               Sweden
            ),
            new Participant(
               new ParticipantId("participant:switzerland-mens-ice-hockey"),
               "Switzerland men's national ice hockey team",
               ParticipantKind.NationalTeam,
               Switzerland
            )
         ]
      );
   }

   private static SportEvent CreateVegasVsFloridaGame()
   {
      var iceHockey = new Sport(new SportId("sport:ice-hockey"), "Ice hockey");
      var competition = new Competition(
         new CompetitionId("competition:nhl-2025-2026"),
         "NHL 2025-2026 season",
         iceHockey
      );
      var williamKarlsson = new Person(
         new PersonId("person:william-karlsson"),
         "William Karlsson",
         [Sweden]
      );

      return new SportEvent(
         new EventId("event:nhl-2025-2026-stanley-cup-final-game-1"),
         "Las Vegas Golden Knights vs Florida Panthers, Game 1",
         competition,
         new DateTimeOffset(2026, 6, 10, 2, 0, 0, TimeSpan.Zero),
         "Stanley Cup Final",
         [
            new Participant(
               new ParticipantId("participant:las-vegas-golden-knights"),
               "Las Vegas Golden Knights",
               ParticipantKind.ClubTeam,
               null,
               [
                  new RosterMembership(williamKarlsson, "player")
               ]
            ),
            new Participant(
               new ParticipantId("participant:florida-panthers"),
               "Florida Panthers",
               ParticipantKind.ClubTeam,
               null,
               []
            )
         ]
      );
   }
}
