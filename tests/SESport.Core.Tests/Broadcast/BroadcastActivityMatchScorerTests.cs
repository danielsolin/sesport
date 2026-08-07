using SESport.Core.Broadcast;
using SESport.Core.Configuration;

namespace SESport.Core.Tests.Broadcast;

public sealed class BroadcastActivityMatchScorerTests
{
   [Fact]
   public void GetScoreRejectsCandidatesOutsideMatchWindow()
   {
      var broadcastStart = new DateTime(2026, 8, 7, 12, 0, 0);

      var score = BroadcastActivityMatchScorer.GetScore(
         "World Championship",
         "World Championship",
         broadcastStart,
         broadcastStart.AddHours(ActivityGroupDefaults.MatchWindowHours + 1)
      );

      Assert.Equal(0, score);
   }

   [Fact]
   public void GetScoreRewardsExactTitleMatches()
   {
      var start = new DateTime(2026, 8, 7, 12, 0, 0);

      var exactScore = BroadcastActivityMatchScorer.GetScore(
         "World Championship",
         "World Championship",
         start,
         start
      );
      var partialScore = BroadcastActivityMatchScorer.GetScore(
         "World Championship",
         "World",
         start,
         start
      );

      Assert.True(exactScore > partialScore);
   }
}
