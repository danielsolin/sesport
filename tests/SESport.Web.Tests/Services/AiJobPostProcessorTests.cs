namespace SESport.Web.Tests.Services;

public sealed class AiJobPostProcessorTests
{
   [Fact]
   public void ExtractGeneratedActivityFacts_ReadsFactsAndSources()
   {
      const string output = """
         {
           "facts": [
             {
               "text": "Fact one.",
               "sources": [
                 {
                   "url": "https://example.test/event",
                   "title": "Event page",
                   "excerpt": "Supporting text"
                 }
               ]
             },
             {
               "text": "Fact two.",
               "sources": [
                 {
                   "url": "https://example.test/second"
                 }
               ]
             },
             {
               "text": "Fact three.",
               "sources": [
                 {
                   "url": "https://example.test/third"
                 }
               ]
             }
           ]
         }
         """;

      var result = AiJobPostProcessor.ExtractGeneratedActivityFacts(output);

      Assert.NotNull(result);
      Assert.Equal(3, result.Facts.Count);
      Assert.Equal("Fact one.", result.Facts[0].Text);
      var source = Assert.Single(result.Facts[0].Sources);
      Assert.Equal("https://example.test/event", source.Url);
      Assert.Equal("Event page", source.Title);
      Assert.Equal("Supporting text", source.Excerpt);
   }

   [Fact]
   public void ExtractGeneratedActivityFacts_FiltersInvalidAndDuplicateUrls()
   {
      const string output = """
         {
           "facts": [
             {
               "text": "Fact.",
               "sources": [
                 {"url": "not-a-url"},
                 {"url": "https://example.test/event"},
                 {"url": "https://example.test/event"}
               ]
             },
             {
               "text": "Second fact.",
               "sources": [
                 {"url": "https://example.test/second"}
               ]
             },
             {
               "text": "Third fact.",
               "sources": [
                 {"url": "https://example.test/third"}
               ]
             },
             {
               "text": "Unsupported fact.",
               "sources": [
                 {"url": "not-a-url"}
               ]
             }
           ]
         }
         """;

      var result = AiJobPostProcessor.ExtractGeneratedActivityFacts(output);

      Assert.NotNull(result);
      Assert.Equal(3, result.Facts.Count);
      Assert.Single(result.Facts[0].Sources);
   }

   [Fact]
   public void ExtractGeneratedActivityFactsRejectsLegacyOutput()
   {
      const string output = """{"facts":"Legacy fact."}""";

      var result = AiJobPostProcessor.ExtractGeneratedActivityFacts(output);

      Assert.Null(result);
   }

   [Fact]
   public void ExtractGeneratedPersonFacts_ConvertsWhitespaceToNull()
   {
      const string output = """
         {
           "birthdate": "1991-04-08",
           "height": 181,
           "weight": 77,
           "formative_club": "   ",
           "sources": [
             {
               "url": "https://example.test/source"
             }
           ]
         }
         """;

      var result = AiJobPostProcessor.ExtractGeneratedPersonFacts(output);

      Assert.NotNull(result);
      Assert.Equal(new DateOnly(1991, 4, 8), result!.Birthdate);
      Assert.Equal(181, result.Height);
      Assert.Equal(77, result.Weight);
      Assert.Null(result.FormativeClub);
      Assert.Single(result.Sources);
   }
}
