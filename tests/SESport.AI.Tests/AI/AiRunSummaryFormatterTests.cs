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

   [Fact]
   public void FormatReturnsAllPersonFacts()
   {
      var summary = AiRunSummaryFormatter.Format(
         """
         {
           "height": 201,
           "weight": 105,
           "formative_club": "Malmö FF",
           "birthdate": "2000-10-12",
           "sources": []
         }
         """,
         AiJobIds.FindPersonFacts
      );

      Assert.Equal(
         "birthdate: 2000-10-12, height: 201, weight: 105, " +
         "formative club: Malmö FF",
         summary
      );
   }
}
