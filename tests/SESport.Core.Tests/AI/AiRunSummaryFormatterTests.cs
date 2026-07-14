using SESport.Core.AI;

namespace SESport.Core.Tests.AI;

public sealed class AiRunSummaryFormatterTests
{
   [Fact]
   public void FormatReturnsFirstArrayCountForJsonOutput()
   {
      var summary = AiRunSummaryFormatter.Format("""
         {
           "Participants": [
             {
               "Name": "Alice"
             },
             {
               "Name": "Bob"
             }
           ],
           "CheckedSources": [],
           "Participation": "Yes"
         }
         """);

      Assert.Equal("2 participants", summary);
   }

   [Fact]
   public void FormatReturnsTrimmedPlainTextForNonJsonOutput()
   {
      var summary = AiRunSummaryFormatter.Format(
         "Completed run with a short result.  "
      );

      Assert.Equal("Completed run with a short result.", summary);
   }
}
