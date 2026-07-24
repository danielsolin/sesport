using SESport.Web.Services;

namespace SESport.Web.Tests.Services;

public sealed class AiJobPostProcessorTests
{
   [Fact]
   public void ExtractGeneratedActivityFacts_ReadsFactsAndSources()
   {
      const string output = """
         {
           "facts": "Fact one.\nFact two.",
           "sources": [
             {
               "url": "https://example.test/event",
               "title": "Event page",
               "excerpt": "Supporting text"
             }
           ]
         }
         """;

      var result = AiJobPostProcessor.ExtractGeneratedActivityFacts(output);

      Assert.NotNull(result);
      Assert.Equal("Fact one.\nFact two.", result.Facts);
      var source = Assert.Single(result.Sources);
      Assert.Equal("https://example.test/event", source.Url);
      Assert.Equal("Event page", source.Title);
      Assert.Equal("Supporting text", source.Excerpt);
   }

   [Fact]
   public void ExtractGeneratedActivityFacts_FiltersInvalidAndDuplicateUrls()
   {
      const string output = """
         {
           "facts": "Fact.",
           "sources": [
             {"url": "not-a-url"},
             {"url": "https://example.test/event"},
             {"url": "https://example.test/event"}
           ]
         }
         """;

      var result = AiJobPostProcessor.ExtractGeneratedActivityFacts(output);

      Assert.NotNull(result);
      Assert.Single(result.Sources);
   }

   [Fact]
   public void ExtractGeneratedFacts_RemainsCompatibleWithLegacyOutput()
   {
      const string output = """{"facts":"Legacy fact."}""";

      var result = AiJobPostProcessor.ExtractGeneratedFacts(output);

      Assert.Equal("Legacy fact.", result);
   }
}
