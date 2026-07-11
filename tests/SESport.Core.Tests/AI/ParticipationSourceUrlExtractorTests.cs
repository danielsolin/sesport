using SESport.Data.AI;

namespace SESport.Core.Tests.AI;

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
   public void ExtractFromOutput_ReturnsSourcesArray()
   {
      var outputText = """
         {
            "Participation": "Yes",
            "Participants": ["Dino Beganovic"],
            "Sources": [
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
   public void ExtractFromOutput_ReturnsNestedParticipantSources()
   {
      var outputText = """
         {
            "Participation": "Yes",
            "Participants": [
               {
                  "Name": "Dino Beganovic",
                  "Sources": [
                     {
                        "Url": "https://example.test/participant",
                        "EvidenceType": "ParticipantList"
                     }
                  ]
               }
            ],
            "CheckedSources": [
               {
                  "Url": "https://example.test/checked",
                  "EvidenceType": "ParticipantList"
               },
               {
                  "Url": "https://example.test/participant",
                  "EvidenceType": "ParticipantList"
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
