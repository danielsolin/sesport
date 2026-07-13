using SESport.AI.Llama;
using System.Text.Json.Nodes;

namespace SESport.Core.Tests.AI;

public sealed class LlamaRequestFactoryTests
{
   [Fact]
   public void AddValidationFeedbackPromptExplainsRejectedSubmitReport()
   {
      var messages = new JsonArray();

      LlamaRequestFactory.AddValidationFeedbackPrompt(
         messages,
         "AI job returned invalid json_schema output: " +
         "Participant source EvidenceType must match fetched source.",
         reportSubmissionAttempt: true
      );

      var prompt = messages[0]!["content"]!.GetValue<string>();

      Assert.Contains(
         "The previous final answer was rejected by validation:",
         prompt
      );
      Assert.Contains(
         "This happened after a submit_report attempt.",
         prompt
      );
      Assert.Contains(
         "EvidenceType did not match the fetched page classification",
         prompt
      );
      Assert.Contains(
         "ParticipantList or TeamRoster",
         prompt
      );
   }
}
