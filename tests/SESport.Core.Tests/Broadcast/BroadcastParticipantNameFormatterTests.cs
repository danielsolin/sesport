using SESport.Core.Broadcast;

namespace SESport.Core.Tests.Broadcast;

public sealed class BroadcastParticipantNameFormatterTests
{
   [Theory]
   [InlineData("Christoffer BRUNNHAGEN", "Christoffer Brunnhagen")]
   [InlineData("DINO BEGANOVIC", "Dino Beganovic")]
   [InlineData("Anna-Karin NILSSON", "Anna-Karin Nilsson")]
   [InlineData("  Christoffer   BRUNNHAGEN  ", "Christoffer Brunnhagen")]
   public void FormatTitleCasesShoutedParticipantWords(
      string value,
      string expected
   )
   {
      Assert.Equal(expected, BroadcastParticipantNameFormatter.Format(value));
   }

   [Theory]
   [InlineData("Ludvig Åberg")]
   [InlineData("Oscar Piastri")]
   [InlineData("McLaren")]
   public void FormatKeepsMixedCaseWords(string value)
   {
      Assert.Equal(value, BroadcastParticipantNameFormatter.Format(value));
   }
}
