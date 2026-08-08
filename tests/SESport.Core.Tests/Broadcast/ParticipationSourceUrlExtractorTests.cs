using SESport.Core.Broadcast;

namespace SESport.Core.Tests.Broadcast;

public class ParticipationSourceUrlExtractorTests
{
   [Fact]
   public void Extract_ReturnsUniqueUrls()
   {
      var rawResponse = """
         {
            "choices":[
               {
                  "message":{
                     "content":"See https://example.test/a.",
                     "more":"See https://example.test/a."
                  }
               }
            ],
            "source":"https://example.test/b"
         }
         """;

      var urls = ParticipationSourceUrlExtractor.Extract(rawResponse);

      Assert.Equal(
         [
            "https://example.test/a",
            "https://example.test/b"
         ],
         urls
      );
   }

   [Fact]
   public void ExtractFromOutput_ReturnsCheckedSourcesArray()
   {
      var outputText = """
         {
            "Participation": "Yes",
            "Participants": ["Dino Beganovic"],
            "CheckedSources": [
               "https://example.test/a",
               "https://example.test/a",
               "https://example.test/b"
            ]
         }
         """;

      var urls = ParticipationSourceUrlExtractor.ExtractFromOutput(outputText);

      Assert.Equal(
         [
            "https://example.test/a",
            "https://example.test/b"
         ],
         urls
      );
   }

   [Fact]
   public void ExtractFromOutput_ReturnsGlobalCheckedSources()
   {
      var outputText = """
         {
            "Participation": "Yes",
            "Participants": [
               {
                  "Name": "Dino Beganovic"
               }
            ],
            "CheckedSources": [
               {
                  "Url": "https://example.test/checked"
               },
               {
                  "Url": "https://example.test/participant"
               }
            ]
         }
         """;

      var urls = ParticipationSourceUrlExtractor.ExtractFromOutput(outputText);

      Assert.Equal(
         [
            "https://example.test/checked",
            "https://example.test/participant"
         ],
         urls
      );
   }

   [Fact]
   public void Extract_ReturnsEmptyListForMissingResponse()
   {
      var urls = ParticipationSourceUrlExtractor.Extract(null);

      Assert.Empty(urls);
   }
}
