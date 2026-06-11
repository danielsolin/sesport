using SESport.AI.Persistence;

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
                     "content":"See https://example.test/a and https://example.test/a."
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
   public void Extract_ReturnsEmptyListForMissingResponse()
   {
      var urls = ParticipationSourceUrlExtractor.Extract(null);

      Assert.Empty(urls);
   }
}
