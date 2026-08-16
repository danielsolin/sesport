using SESport.Core.AI;

namespace SESport.Core.Tests.AI;

public sealed class ActivityParticipantAiOutputParserTests
{
   [Fact]
   public void ParseReadsParticipantStartTimesAndSourceUrls()
   {
      const string output = """
         {
           "Participants": [
             {
               "Name": "First Runner",
               "start_time": "12:30",
               "source_url": "https://example.test/first"
             },
             {
               "name": "Second Runner",
               "start_time": null,
               "source_url": "https://example.test/second"
             }
           ]
         }
         """;

      var result = ActivityParticipantAiOutputParser.Parse(output);

      Assert.NotNull(result);
      Assert.Equal(2, result!.CheckedSources.Count);
      Assert.Equal(2, result.Participants.Count);

      var firstParticipant = result.Participants[0];
      Assert.Equal("First Runner", firstParticipant.Name);
      Assert.Single(firstParticipant.Sources);

      var firstSource = firstParticipant.Sources[0];
      Assert.Equal("https://example.test/first", firstSource.Url);

      var firstStartTime = Assert.Single(
         firstParticipant.Fields,
         field => field.FieldKey == "start_time"
      );
      Assert.Equal("12:30", firstStartTime.ValueText);
      Assert.Equal("\"12:30\"", firstStartTime.ValueJson);

      var secondParticipant = result.Participants[1];
      Assert.Equal("Second Runner", secondParticipant.Name);
      Assert.Single(secondParticipant.Sources);

      var secondSource = secondParticipant.Sources[0];
      Assert.Equal("https://example.test/second", secondSource.Url);

      var secondStartTime = Assert.Single(
         secondParticipant.Fields,
         field => field.FieldKey == "start_time"
      );
      Assert.Null(secondStartTime.ValueText);
      Assert.Equal("null", secondStartTime.ValueJson);

      Assert.Contains(
         result.CheckedSources,
         source => source.Url == "https://example.test/first"
      );
      Assert.Contains(
         result.CheckedSources,
         source => source.Url == "https://example.test/second"
      );
   }

   [Fact]
   public void ParseTreatsStringNullParticipantStartTimeAsNull()
   {
      const string output = """
         {
           "participants": [
             {
               "name": "Runner",
               "start_time": "null",
               "source_url": "https://example.test/runner"
             }
           ]
         }
         """;

      var result = ActivityParticipantAiOutputParser.Parse(output);

      Assert.NotNull(result);
      var participant = Assert.Single(result!.Participants);
      var startTime = Assert.Single(
         participant.Fields,
         field => field.FieldKey == "start_time"
      );

      Assert.Null(startTime.ValueText);
      Assert.Equal("null", startTime.ValueJson);
   }

   [Fact]
   public void ParseReturnsNullForInvalidJson()
   {
      Assert.Null(ActivityParticipantAiOutputParser.Parse("not json"));
   }
}
