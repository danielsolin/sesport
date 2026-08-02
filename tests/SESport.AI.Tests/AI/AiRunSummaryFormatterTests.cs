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
   public void FormatCountsNonNullObjectFieldValues()
   {
      var summary = AiRunSummaryFormatter.Format("""
         {
           "participants": [
             {
               "name": "Alice",
               "start_time": "10:00",
               "source_url": "https://example.test/a"
             },
             {
               "name": "Bob",
               "start_time": null,
               "source_url": "https://example.test/b"
             },
             {
               "name": "Carol",
               "start_time": "11:00",
               "source_url": "https://example.test/c"
             },
             {
               "name": "Dave",
               "start_time": "12:00",
               "source_url": "https://example.test/d"
             }
           ]
         }
         """);

      Assert.Equal("4 participants, 3 start times", summary);
   }

   [Fact]
   public void FormatUsesNullableSchemaFieldWhenAllValuesArePresent()
   {
      var summary = AiRunSummaryFormatter.Format(
         """
         {
           "participants": [
             {"name":"Alice","start_time":"10:00"},
             {"name":"Bob","start_time":"11:00"}
           ]
         }
         """,
         outputSchemaJson: """
         {
           "type": "object",
           "properties": {
             "participants": {
               "type": "array",
               "items": {
                 "type": "object",
                 "properties": {
                   "name": {"type": "string"},
                   "start_time": {
                     "type": ["string", "null"]
                   }
                 }
               }
             }
           }
         }
         """
      );

      Assert.Equal("2 participants, 2 start times", summary);
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
         AiJobIds.FindPersonData
      );

      Assert.Equal(
         "birthdate: 2000-10-12, height: 201, weight: 105, " +
         "formative club: Malmö FF",
         summary
      );
   }
}
