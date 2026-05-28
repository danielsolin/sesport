namespace SESport.Core.Tests;

public class RelevanceTests
{
   private static readonly Country Sweden = new("SE", "Sweden");
   private static readonly Country Switzerland = new("CH", "Switzerland");

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
      var finland = new Country("FI", "Finland");
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
      var iceHockey = new Sport("Ice hockey");
      var competition = new Competition(
         "2026 IIHF Ice Hockey World Championship",
         iceHockey
      );

      return new SportEvent(
         "Sweden vs Switzerland",
         competition,
         new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.FromHours(2)),
         "Quarter-final",
         [
            new Participant(
               "Sweden men's national ice hockey team",
               ParticipantKind.NationalTeam,
               Sweden
            ),
            new Participant(
               "Switzerland men's national ice hockey team",
               ParticipantKind.NationalTeam,
               Switzerland
            )
         ]
      );
   }

   private static SportEvent CreateVegasVsFloridaGame()
   {
      var iceHockey = new Sport("Ice hockey");
      var competition = new Competition("Stanley Cup Final", iceHockey);
      var williamKarlsson = new Person("William Karlsson", [Sweden]);

      return new SportEvent(
         "Las Vegas Golden Knights vs Florida Panthers",
         competition,
         new DateTimeOffset(2026, 6, 10, 2, 0, 0, TimeSpan.Zero),
         "Final",
         [
            new Participant(
               "Las Vegas Golden Knights",
               ParticipantKind.ClubTeam,
               null,
               [
                  new RosterMembership(williamKarlsson, "player")
               ]
            ),
            new Participant(
               "Florida Panthers",
               ParticipantKind.ClubTeam,
               null,
               []
            )
         ]
      );
   }
}
