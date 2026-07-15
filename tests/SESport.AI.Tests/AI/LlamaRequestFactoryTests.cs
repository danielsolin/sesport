using System.Text.Json.Nodes;

using SESport.AI.Llama;

namespace SESport.Core.Tests.AI;

public sealed class LlamaRequestFactoryTests
{
   [Fact]
   public void AddValidationFeedbackPromptExplainsSchemaFailure()
   {
      var messages = new JsonArray();

      LlamaRequestFactory.AddValidationFeedbackPrompt(
         messages,
         "AI job returned invalid json_schema output: " +
         "Expected a JSON object.",
         reportSubmissionAttempt: true
      );

      var prompt = messages[0]!["content"]!.GetValue<string>();

      Assert.Contains(
         "The previous final answer was rejected by schema validation:",
         prompt
      );
      Assert.Contains(
         "This happened after a submit_report attempt.",
         prompt
      );
      Assert.Contains(
         "AI job returned invalid json_schema output:",
         prompt
      );
      Assert.DoesNotContain("EvidenceType", prompt);
      Assert.DoesNotContain("participant source", prompt);
   }
}
