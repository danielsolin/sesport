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
      Assert.DoesNotContain("participant source", prompt);
   }

   [Fact]
   public void AddCorruptedParticipantNameRetryPromptIsMinimal()
   {
      var messages = new JsonArray();

      LlamaRequestFactory.AddCorruptedParticipantNameRetryPrompt(messages);

      var prompt = messages[0]!["content"]!.GetValue<string>();

      Assert.Contains("corrupted participant name", prompt);
      Assert.Contains("Retry the same report once", prompt);
      Assert.DoesNotContain("schema validation", prompt);
   }

   [Fact]
   public void CreateFinalAddsLowReasoningEffortAfterTools()
   {
      var request = new JsonObject
      {
         ["tools"] = new JsonArray
         {
            new JsonObject
            {
               ["name"] = "submit_report"
            }
         },
         ["chat_template_kwargs"] = new JsonObject
         {
            ["other_setting"] = "keep"
         }
      };

      var finalRequest = LlamaRequestFactory.CreateFinal(
         request,
         CreateJob(),
         CreatePrompt()
      );

      Assert.Null(finalRequest["tools"]);
      Assert.Equal(
         "low",
         finalRequest["chat_template_kwargs"]!["reasoning_effort"]!
            .GetValue<string>()
      );
      Assert.Equal(
         "keep",
         finalRequest["chat_template_kwargs"]!["other_setting"]!
            .GetValue<string>()
      );
      Assert.NotNull(request["tools"]);
      Assert.Null(
         request["chat_template_kwargs"]!["reasoning_effort"]
      );
   }

   [Fact]
   public void CreateFinalDoesNotAddLowReasoningEffortWithoutTools()
   {
      var request = new JsonObject
      {
         ["messages"] = new JsonArray()
      };

      var finalRequest = LlamaRequestFactory.CreateFinal(
         request,
         CreateJob(),
         CreatePrompt()
      );

      Assert.Null(finalRequest["chat_template_kwargs"]);
      Assert.NotNull(request["messages"]);
   }

   private static AiJobDefinition CreateJob()
   {
      return new AiJobDefinition(
         "job",
         "Job",
         null,
         "provider",
         AiOutputModeIds.Text,
         null,
         null,
         null,
         false,
         true,
         null
      );
   }

   private static AiPromptDefinition CreatePrompt()
   {
      return new AiPromptDefinition(
         Guid.NewGuid(),
         "job",
         1,
         "System",
         "User",
         null,
         "{}",
         null,
         null,
         null,
         true
      );
   }
}
