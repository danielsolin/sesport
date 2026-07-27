using SESport.AI.Llama;

namespace SESport.Core.Tests.AI;

public sealed class LlamaReportSubmissionTests
{
   [Theory]
   [InlineData("""
      {"Participants":[{"Name":"R. (???) ...??...???"}]}
      """)]
   [InlineData("""
      {"Participants":["SWE?"]}
      """)]
   [InlineData("""
      {"Participants":[{"Name":" "}]}
      """)]
   public void TryGetCorruptedParticipantNameReasonFlagsSuspiciousNames(
      string report
   )
   {
      var detected = LlamaReportSubmission
         .TryGetCorruptedParticipantNameReason(report, out var reason);

      Assert.True(detected);
      Assert.Contains("corrupted participant name", reason);
   }

   [Theory]
   [InlineData("""
      {"Participants":[{"Name":"Armand Duplantis"}]}
      """)]
   [InlineData("""
      {"Participants":["Dino Beganovic"]}
      """)]
   [InlineData("""
      {"Participants":[{"Name":"R."}]}
      """)]
   public void TryGetCorruptedParticipantNameReasonIgnoresCleanNames(
      string report
   )
   {
      var detected = LlamaReportSubmission
         .TryGetCorruptedParticipantNameReason(report, out var reason);

      Assert.False(detected);
      Assert.Equal("", reason);
   }
}
