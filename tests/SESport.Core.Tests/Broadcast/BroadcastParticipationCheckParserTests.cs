using SESport.Core.Broadcast;

namespace SESport.Core.Tests.Broadcast;

public class BroadcastParticipationCheckParserTests
{
   [Fact]
   public void Parse_ReturnsStructuredResultAndSourceUrls()
   {
      var outputText = """
         {
            "Participation": "Yes",
            "Participants": [
               "Dino Beganovic",
               {
                  "Name": "Oliver Bearman"
               }
            ],
            "CheckedSources": [
               "https://example.test/a",
               "https://example.test/a"
            ]
         }
         """;

      var result = BroadcastParticipationCheckParser.Parse(
         Guid.NewGuid(),
         "completed",
         3,
         outputText,
         null,
         null
      );

      Assert.Equal("Yes", result.Participation);
      Assert.Equal(
         [
            "Dino Beganovic",
            "Oliver Bearman"
         ],
         result.Participants
      );
      Assert.Equal(["https://example.test/a"], result.SourceUrls);
      Assert.Null(result.ErrorMessage);
   }

   [Fact]
   public void Parse_FallsBackToRawResponseUrlsAndDefaultErrorMessage()
   {
      var result = BroadcastParticipationCheckParser.Parse(
         Guid.NewGuid(),
         "failed",
         1,
         "not json",
         "See https://example.test/raw.",
         null
      );

      Assert.Null(result.Participation);
      Assert.Empty(result.Participants);
      Assert.Equal(["https://example.test/raw"], result.SourceUrls);
      Assert.Equal(
         "The model returned invalid JSON.",
         result.ErrorMessage
      );
   }
}
