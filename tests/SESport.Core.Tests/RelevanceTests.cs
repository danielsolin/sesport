namespace SESport.Core.Tests;

public class RelevanceTests
{
   [Fact]
   public void SwedenVsSwitzerlandIsRelevantToSweden()
   {
      var sweden = new Country("SE", "Sweden");
      var switzerland = new Country("CH", "Switzerland");
      var iceHockey = new Sport("Ice hockey");
      var competition = new Competition(
         "2026 IIHF Ice Hockey World Championship",
         iceHockey
      );

      var game = new SportEvent(
         "Sweden vs Switzerland",
         competition,
         new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.FromHours(2)),
         "Quarter-final",
         [
            new Participant(
               "Sweden men's national ice hockey team",
               sweden
            ),
            new Participant(
               "Switzerland men's national ice hockey team",
               switzerland
            )
         ]
      );

      var relevance = game.GetRelevanceFor(sweden);

      Assert.NotNull(relevance);
      Assert.Equal(sweden, relevance.Country);
      Assert.Equal(
         "Sweden men's national ice hockey team represents Sweden.",
         relevance.Reason
      );
   }

   [Fact]
   public void SwedenVsSwitzerlandIsNotRelevantToFinland()
   {
      var sweden = new Country("SE", "Sweden");
      var switzerland = new Country("CH", "Switzerland");
      var finland = new Country("FI", "Finland");
      var iceHockey = new Sport("Ice hockey");
      var competition = new Competition(
         "2026 IIHF Ice Hockey World Championship",
         iceHockey
      );

      var game = new SportEvent(
         "Sweden vs Switzerland",
         competition,
         new DateTimeOffset(2026, 5, 28, 20, 20, 0, TimeSpan.FromHours(2)),
         "Quarter-final",
         [
            new Participant(
               "Sweden men's national ice hockey team",
               sweden
            ),
            new Participant(
               "Switzerland men's national ice hockey team",
               switzerland
            )
         ]
      );

      var relevance = game.GetRelevanceFor(finland);

      Assert.Null(relevance);
   }
}
