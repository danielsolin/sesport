using SESport.Web.Pages.Admin.Config.Ai.Runs;

namespace SESport.Core.Tests.Pages.Admin.Config.Ai.Runs;

public sealed class DetailsModelTests
{
   [Fact]
   public void FormatToolCallReturnsCompactFindSignature()
   {
      var toolCall = new DetailsModel.ToolTraceCallViewModel(
         "call_1",
         "web_find_in_page",
         """
         {
           "id": "s2_8",
           "find": "Sweden"
         }
         """
      );

      Assert.Equal(
         "web_find_in_page('s2_8','Sweden')",
         DetailsModel.FormatToolCall(toolCall)
      );
   }

   [Fact]
   public void FormatToolCallReturnsCompactSearchSignature()
   {
      var toolCall = new DetailsModel.ToolTraceCallViewModel(
         "call_1",
         "web_search",
         """
         {
           "query": "Belgien runt Etapp 2 participants",
           "limit": 5
         }
         """
      );

      Assert.Equal(
         "web_search('Belgien runt Etapp 2 participants',5)",
         DetailsModel.FormatToolCall(toolCall)
      );
   }
}
