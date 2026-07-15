using SESport.Core.Ingestion;

namespace SESport.Core.Tests.Ingestion;

public class ActivityProposalDisplayFormatterTests
{
   [Fact]
   public void HasAiPrompt_ReturnsTrueForAiSearchWithPrompt()
   {
      var result = ActivityProposalDisplayFormatter.HasAiPrompt(
         ActivityProposalProducerType.AiSearch.ToString(),
         "Use the web search results."
      );

      Assert.True(result);
   }

   [Fact]
   public void HasAiPrompt_ReturnsFalseForLowerCaseProducerTypeId()
   {
      var result = ActivityProposalDisplayFormatter.HasAiPrompt(
         "aisearch",
         "Use the web search results."
      );

      Assert.False(result);
   }

   [Theory]
   [InlineData(ActivityProposalProducerType.Manual)]
   [InlineData(ActivityProposalProducerType.WebImport)]
   public void HasAiPrompt_ReturnsFalseForOtherProducerTypes(
      ActivityProposalProducerType producerType
   )
   {
      var result = ActivityProposalDisplayFormatter.HasAiPrompt(
         producerType.ToString(),
         "Use the web search results."
      );

      Assert.False(result);
   }

   [Fact]
   public void HasAiPrompt_ReturnsFalseForMissingPrompt()
   {
      var result = ActivityProposalDisplayFormatter.HasAiPrompt(
         ActivityProposalProducerType.AiSearch.ToString(),
         " "
      );

      Assert.False(result);
   }
}
