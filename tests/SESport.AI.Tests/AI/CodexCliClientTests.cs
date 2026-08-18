using SESport.AI.Clients;
using System.Text.Json;

namespace SESport.Core.Tests.AI;

public sealed class CodexCliClientTests
{
   [Fact]
   public async Task GenerateAsyncUsesFullCodexContextAndJsonlOutput()
   {
      var output = CreateOutput("Yes");
      var runner = new RecordingProcessRunner(
         new CodexCliProcessResult(
            0,
            CreateJsonl(output),
            "",
            output
         )
      );
      var client = CreateClient(runner);
      var traceUpdates = new List<string>();

      var result = await client.GenerateAsync(
         CreateProvider(),
         CreateJob(),
         CreatePrompt(),
         CreateRenderedPrompt(),
         "{\"event_name\":\"Test event\"}",
         CancellationToken.None,
         (trace, _, _) =>
         {
            traceUpdates.Add(trace ?? "");
            return Task.CompletedTask;
         }
      );

      Assert.Equal(output, result.OutputText);
      Assert.Equal(1, result.ToolRoundCount);
      Assert.NotNull(result.RawResponseJson);
      Assert.NotEmpty(traceUpdates);
      Assert.NotNull(runner.Invocation);
      Assert.Contains(
         "--dangerously-bypass-approvals-and-sandbox",
         runner.Invocation!.Arguments
      );
      Assert.Contains("--json", runner.Invocation.Arguments);
      Assert.Contains("--search", runner.Invocation.Arguments);
      Assert.Equal("--search", runner.Invocation.Arguments[0]);
      Assert.Equal("exec", runner.Invocation.Arguments[1]);
      Assert.Contains("--output-last-message", runner.Invocation.Arguments);
      Assert.Contains("--output-schema", runner.Invocation.Arguments);
      Assert.Contains("--model", runner.Invocation.Arguments);
      Assert.Contains("--config", runner.Invocation.Arguments);
      Assert.Contains(
         "model_reasoning_effort=\"medium\"",
         runner.Invocation.Arguments
      );
      Assert.Contains("Test event", runner.Invocation.Prompt);
      Assert.DoesNotContain("web_search", runner.Invocation.Prompt);
   }

   [Fact]
   public async Task GenerateAsyncRejectsInvalidStructuredOutput()
   {
      var runner = new RecordingProcessRunner(
         new CodexCliProcessResult(0, CreateJsonl("{}"), "", "{}")
      );
      var client = CreateClient(runner);

      var exception = await Assert.ThrowsAsync<AiProviderExecutionException>(
         () => client.GenerateAsync(
            CreateProvider(),
            CreateJob(),
            CreatePrompt(),
            CreateRenderedPrompt(),
            "{}",
            CancellationToken.None
         )
      );

      Assert.Contains("invalid output", exception.Message);
      Assert.Contains("full_access", exception.RawRequestJson);
      Assert.NotNull(exception.RawResponseJson);
      Assert.NotNull(exception.ToolTraceJson);
   }

   [Fact]
   public async Task GenerateAsyncReportsNonZeroExitAsProviderFailure()
   {
      var runner = new RecordingProcessRunner(
         new CodexCliProcessResult(
            23,
            "",
            "Codex authentication failed.",
            null
         )
      );
      var client = CreateClient(runner);

      var exception = await Assert.ThrowsAsync<AiProviderExecutionException>(
         () => client.GenerateAsync(
            CreateProvider(),
            CreateJob(),
            CreatePrompt(),
            CreateRenderedPrompt(),
            "{}",
            CancellationToken.None
         )
      );

      Assert.Contains("code 23", exception.Message);
      Assert.Contains("authentication failed", exception.Message);
   }

   [Fact]
   public void CreateRequestPayloadDoesNotIncludeLlamaToolDefinitions()
   {
      var client = CreateClient(
         new RecordingProcessRunner(
            new CodexCliProcessResult(0, "", "", "")
         )
      );
      var job = CreateJob();
      var payload = client.CreateRequestPayload(
         CreateProvider(),
         job,
         CreatePrompt(),
         CreateRenderedPrompt()
      );

      var json = payload.ToJsonString();

      Assert.Contains("full_access", json);
      Assert.Contains("event_name", json);
      Assert.DoesNotContain("tools_json", json);
      Assert.DoesNotContain("\"format\"", json);
   }

   [Fact]
   public async Task GenerateAsyncUsesPromptReasoningEffort()
   {
      var runner = new RecordingProcessRunner(
         new CodexCliProcessResult(
            0,
            CreateJsonl(CreateOutput("Yes")),
            "",
            CreateOutput("Yes")
         )
      );
      var client = CreateClient(runner);

      await client.GenerateAsync(
         CreateProvider(),
         CreateJob(),
         CreatePrompt(CodexReasoningEfforts.Low),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.Contains(
         "model_reasoning_effort=\"low\"",
         runner.Invocation!.Arguments
      );
   }

   [Fact]
   public async Task GenerateAsyncIgnoresReasoningForOtherProviderKinds()
   {
      var runner = new RecordingProcessRunner(
         new CodexCliProcessResult(
            0,
            CreateJsonl(CreateOutput("Yes")),
            "",
            CreateOutput("Yes")
         )
      );
      var client = CreateClient(runner);
      var provider = CreateProvider() with
      {
         Kind = AiProviderKinds.LlamaServer
      };

      await client.GenerateAsync(
         provider,
         CreateJob(),
         CreatePrompt(CodexReasoningEfforts.Low),
         CreateRenderedPrompt(),
         "{}",
         CancellationToken.None
      );

      Assert.DoesNotContain("--config", runner.Invocation!.Arguments);
      Assert.DoesNotContain(
         "model_reasoning_effort=\"low\"",
         runner.Invocation.Arguments
      );
   }

   private static CodexCliClient CreateClient(
      ICodexCliProcessRunner runner
   )
   {
      return new CodexCliClient(
         new CodexCliOptions
         {
            ExecutablePath = "/usr/bin/codex",
            WorkingDirectory = "/tmp",
            TimeoutSeconds = 60
         },
         runner
      );
   }

   private static AiProviderDefinition CreateProvider()
   {
      return new AiProviderDefinition(
         "codex",
         "Codex",
         AiProviderKinds.CodexCli,
         null,
         "gpt-5.6-luna",
         null,
         "{}",
         true
      );
   }

   private static AiJobDefinition CreateJob()
   {
      return new AiJobDefinition(
         AiJobIds.DecidePrimaryCountryParticipation,
         "Decide Swedish participation",
         null,
         "codex",
         AiOutputModeIds.JsonObject,
         "[{\"name\":\"web_search\"}]",
         null,
         null,
         true,
         true,
         null
      );
   }

   private static AiPromptDefinition CreatePrompt(
      string? reasoningEffort = null
   )
   {
      return new AiPromptDefinition(
         Guid.NewGuid(),
         AiJobIds.DecidePrimaryCountryParticipation,
         1,
         "Use current sources.",
         "Assess event_name and return the participation result.",
         CreateSchema(),
         "{}",
         null,
         null,
         null,
         true,
         null,
         reasoningEffort
      );
   }

   private static AiRenderedPrompt CreateRenderedPrompt()
   {
      return new AiRenderedPrompt(
         "Use current sources for event_name.",
         "event_name: Test event"
      );
   }

   private static string CreateOutput(string participation)
   {
      return JsonSerializer.Serialize(
         new
         {
            Participation = participation,
            Participants = Array.Empty<object>(),
            CheckedSources = new[]
            {
               new { Url = "https://example.test/source" }
            }
         }
      );
   }

   private static string CreateJsonl(string output)
   {
      return string.Join(
         Environment.NewLine,
         "{\"type\":\"thread.started\"}",
         "{\"type\":\"turn.started\"}",
         "{\"type\":\"item.completed\",\"item\":{"
            + "\"type\":\"command_execution\"}}",
         "{\"type\":\"item.completed\",\"item\":{"
            + "\"type\":\"agent_message\",\"text\":"
            + JsonSerializer.Serialize(output) + "}}",
         "{\"type\":\"turn.completed\",\"usage\":{"
            + "\"input_tokens\":12,\"output_tokens\":8,"
            + "\"reasoning_tokens\":3}}"
      );
   }

   private static string CreateSchema()
   {
      return """
      {
        "type": "object",
        "required": ["Participation", "Participants", "CheckedSources"],
        "properties": {
          "Participation": {
            "type": "string",
            "enum": ["Yes", "No", "Unknown"]
          },
          "Participants": {
            "type": "array"
          },
          "CheckedSources": {
            "type": "array",
            "items": {
              "type": "object",
              "properties": {
                "Url": {
                  "type": "string",
                  "format": "uri"
                }
              }
            }
          }
        },
        "additionalProperties": false
      }
      """;
   }

   private sealed class RecordingProcessRunner : ICodexCliProcessRunner
   {
      private readonly CodexCliProcessResult result;

      public RecordingProcessRunner(CodexCliProcessResult result)
      {
         this.result = result;
      }

      public CodexCliInvocation? Invocation { get; private set; }

      public async Task<CodexCliProcessResult> RunAsync(
         CodexCliInvocation invocation,
         CancellationToken cancellationToken,
         Func<string, CancellationToken, Task> traceLineReceived
      )
      {
         Invocation = invocation;

         foreach(var line in result.StandardOutput.Split(
            Environment.NewLine,
            StringSplitOptions.RemoveEmptyEntries
         ))
         {
            await traceLineReceived(line, cancellationToken);
         }

         return result;
      }
   }
}
