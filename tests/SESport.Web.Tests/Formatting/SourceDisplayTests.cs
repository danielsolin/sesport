using SESport.Core.Sources;
using SESport.Web.Formatting;

namespace SESport.Core.Tests.Formatting;

public sealed class SourceDisplayTests
{
   [Theory]
   [InlineData(SourceKinds.ActivityEvidence, "Aktivitet")]
   [InlineData(SourceKinds.ParticipationEvidence, "Deltagande")]
   [InlineData(SourceKinds.ParticipantStartEvidence, "Starttid")]
   [InlineData(SourceKinds.ParticipantStarEvidence, "Stjärna")]
   public void FormatKindTranslatesKnownKinds(
      string kind,
      string expected
   )
   {
      Assert.Equal(expected, SourceDisplay.FormatKind(kind));
   }

   [Fact]
   public void FormatKindUsesOneWordFallbackForUnknownKinds()
   {
      Assert.Equal("Källa", SourceDisplay.FormatKind("Unknown"));
   }
}
