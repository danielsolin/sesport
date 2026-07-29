using SESport.Core.AI;

namespace SESport.Core.Tests.AI;

public sealed class ActivityParticipantAiOutputParserTests
{
   [Fact]
   public void ParseReadsGenericParticipantFieldsAndSources()
   {
      const string output = """
         {
           "Participants": [
             {
               "Name": "First Runner",
               "Sources": [
                 {
                   "url": "https://example.test/first"
                 }
               ],
               "start_time": "12:30",
               "lane": 2
             }
           ],
           "CheckedSources": [
             {
               "url": "https://example.test/event",
               "title": "Event page"
             }
           ]
         }
         """;

      var result = ActivityParticipantAiOutputParser.Parse(output);

      Assert.NotNull(result);
      Assert.Single(result!.CheckedSources);
      Assert.Single(result.Participants);

      var participant = result.Participants[0];
      Assert.Equal("First Runner", participant.Name);
      Assert.Single(participant.Sources);

      var source = participant.Sources[0];
      Assert.Equal("https://example.test/first", source.Url);

      var startTime = Assert.Single(
         participant.Fields,
         field => field.FieldKey == "start_time"
      );
      Assert.Equal("12:30", startTime.ValueText);
      Assert.Equal("\"12:30\"", startTime.ValueJson);

      var lane = Assert.Single(
         participant.Fields,
         field => field.FieldKey == "lane"
      );
      Assert.Equal("2", lane.ValueText);
      Assert.Equal("2", lane.ValueJson);
   }

   [Fact]
   public void ParseReturnsNullForInvalidJson()
   {
      Assert.Null(ActivityParticipantAiOutputParser.Parse("not json"));
   }
}
