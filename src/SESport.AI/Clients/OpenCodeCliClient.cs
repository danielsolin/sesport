using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging;

using SESport.AI.Protocols;
using SESport.Core.AI;

namespace SESport.AI.Clients;

public sealed class OpenCodeCliClient : IAiProviderClient
{
   private const string RunCommand = "run";
   private const string FormatArgument = "--format";
   private const string JsonFormat = "json";
   private const string DirectoryArgument = "--dir";
   private const string AutoArgument = "--auto";
   private const string ThinkingArgument = "--thinking";
   private const string ReasoningEventType = "reasoning";
   private const string TextEventType = "text";
   private const string ToolEventType = "tool_use";
   private const string StepStartEventType = "step_start";
   private const string StepFinishEventType = "step_finish";

   private readonly OpenCodeCliOptions options;
   private readonly IOpenCodeCliProcessRunner processRunner;
   private readonly ILogger<OpenCodeCliClient>? logger;

   public OpenCodeCliClient(
      OpenCodeCliOptions options,
      ILogger<OpenCodeCliClient>? logger = null
   )
      : this(options, new OpenCodeCliProcessRunner(), logger)
   {
   }

   internal OpenCodeCliClient(
      OpenCodeCliOptions options,
      IOpenCodeCliProcessRunner processRunner,
      ILogger<OpenCodeCliClient>? logger = null
   )
   {
      this.options = options;
      this.processRunner = processRunner;
      this.logger = logger;
   }

   public IReadOnlyCollection<string> Kinds =>
      [AiProviderKinds.OpenCodeCli];

   public JsonObject CreateRequestPayload(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt
   )
   {
      var promptText = BuildPromptText(prompt, renderedPrompt);
      var invocation = CreateInvocation(promptText);
      var arguments = new JsonArray();

      foreach(var argument in invocation.Arguments)
      {
         arguments.Add(JsonValue.Create(argument));
      }

      return new JsonObject
      {
         ["executable"] = invocation.ExecutablePath,
         ["arguments"] = arguments,
         ["working_directory"] = invocation.WorkingDirectory,
         ["uses_configured_default_model"] = true,
         ["prompt"] = invocation.Prompt
      };
   }

   public async Task<AiJobResult> GenerateAsync(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt,
      string inputPayloadJson,
      CancellationToken cancellationToken,
      Func<string?, int, CancellationToken, Task>? toolTraceUpdated = null
   )
   {
      var promptText = BuildPromptText(prompt, renderedPrompt);
      var invocation = CreateInvocation(promptText);
      var requestJson = CreateRequestPayload(
         provider,
         job,
         prompt,
         renderedPrompt
      ).ToJsonString();
      var trace = new JsonArray();
      var rawEvents = new JsonArray();
      var assistantEntries = new Dictionary<int, JsonObject>();
      var toolEntries = new Dictionary<string, JsonObject>(
         StringComparer.Ordinal
      );
      var currentTurn = 0;
      string? finalMessage = null;

      try
      {
         try
         {
            var processResult = await processRunner.RunAsync(
               invocation,
               cancellationToken,
               async (line, progressCancellationToken) =>
               {
                  if(TryParseEvent(line, out var eventNode))
                  {
                     rawEvents.Add(eventNode);
                     ProcessEvent(
                        eventNode,
                        trace,
                        assistantEntries,
                        toolEntries,
                        ref currentTurn,
                        ref finalMessage
                     );
                  }
                  else if(!string.IsNullOrWhiteSpace(line))
                  {
                     rawEvents.Add(
                        new JsonObject
                        {
                           ["type"] = "stdout",
                           ["text"] = line
                        }
                     );
                  }

                  await ReportTraceAsync(
                     trace,
                     CountToolCalls(trace),
                     toolTraceUpdated,
                     progressCancellationToken
                  );
               }
            );
            var rawResponse = CreateRawResponseJson(
               processResult.ExitCode,
               processResult.StandardOutput,
               processResult.StandardError,
               rawEvents,
               finalMessage
            );
            var toolTraceJson = trace.ToJsonString();
            var toolRoundCount = CountToolCalls(trace);

            if(processResult.ExitCode != 0)
            {
               throw new AiProviderExecutionException(
                  CreateExitCodeMessage(processResult),
                  null,
                  requestJson,
                  rawResponse,
                  toolTraceJson,
                  toolRoundCount,
                  promptText.Length +
                     processResult.StandardOutput.Length
               );
            }

            if(string.IsNullOrWhiteSpace(finalMessage))
            {
               throw new AiProviderExecutionException(
                  "OpenCode CLI returned no final message.",
                  null,
                  requestJson,
                  rawResponse,
                  toolTraceJson,
                  toolRoundCount,
                  promptText.Length +
                     processResult.StandardOutput.Length
               );
            }

            string outputText;

            try
            {
               outputText = ResponsesOutputValidator.ValidateStructuredOutput(
                  finalMessage,
                  job.OutputMode,
                  prompt.OutputSchemaJson
               );
            }
            catch(Exception exception)
            {
               throw new AiProviderExecutionException(
                  "OpenCode CLI returned invalid output: " +
                     exception.Message,
                  exception,
                  requestJson,
                  rawResponse,
                  toolTraceJson,
                  toolRoundCount,
                  promptText.Length +
                     processResult.StandardOutput.Length
               );
            }

            var result = new AiJobResult(
               Guid.NewGuid(),
               job.Id,
               provider.Id,
               provider.Model,
               renderedPrompt.ToPromptText(),
               requestJson,
               outputText,
               rawResponse,
               toolTraceJson,
               toolRoundCount,
               promptText.Length +
                  processResult.StandardOutput.Length,
               null,
               null,
               null,
               null
            );
            await ReportTraceAsync(
               trace,
               toolRoundCount,
               toolTraceUpdated,
               cancellationToken
            );
            return result;
         }
         catch(OperationCanceledException)
            when(cancellationToken.IsCancellationRequested)
         {
            throw;
         }
         catch(AiProviderExecutionException)
         {
            throw;
         }
         catch(Exception exception)
         {
            var rawResponseJson = CreateRawResponseJson(
               null,
               null,
               exception.Message,
               rawEvents,
               finalMessage
            );
            throw new AiProviderExecutionException(
               "OpenCode CLI execution failed: " + exception.Message,
               exception,
               requestJson,
               rawResponseJson,
               trace.ToJsonString(),
               CountToolCalls(trace),
               promptText.Length
            );
         }
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch(AiProviderExecutionException)
      {
         throw;
      }
      catch(Exception exception)
      {
         logger?.LogWarning(
            exception,
            "OpenCode CLI processing failed for AI job {JobId}.",
            job.Id
         );
         throw;
      }
   }

   private OpenCodeCliInvocation CreateInvocation(string promptText)
   {
      var workingDirectory = GetWorkingDirectory();
      var arguments = new List<string>
      {
         RunCommand,
         FormatArgument,
         JsonFormat,
         DirectoryArgument,
         workingDirectory,
         ThinkingArgument,
         AutoArgument,
         promptText
      };

      return new OpenCodeCliInvocation(
         GetExecutablePath(),
         arguments,
         promptText,
         workingDirectory,
         TimeSpan.FromSeconds(options.TimeoutSeconds)
      );
   }

   private static string BuildPromptText(
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt
   )
   {
      var builder = new StringBuilder();
      var configuredPrompt = renderedPrompt.ToPromptText();

      if(!string.IsNullOrWhiteSpace(configuredPrompt))
      {
         builder.AppendLine(configuredPrompt);
      }

      if(!string.IsNullOrWhiteSpace(prompt.OutputSchemaJson))
      {
         builder.AppendLine();
         builder.AppendLine("Output schema JSON:");
         builder.AppendLine(prompt.OutputSchemaJson.Trim());
      }

      return builder.ToString().Trim();
   }

   private string GetWorkingDirectory()
   {
      var configuredDirectory = options.WorkingDirectory?.Trim();
      if(!string.IsNullOrWhiteSpace(configuredDirectory))
      {
         return Path.GetFullPath(configuredDirectory);
      }

      var currentDirectory = Directory.GetCurrentDirectory();
      var directory = new DirectoryInfo(currentDirectory);

      while(directory is not null)
      {
         var gitPath = Path.Combine(directory.FullName, ".git");

         if(Directory.Exists(gitPath) || File.Exists(gitPath))
         {
            return directory.FullName;
         }

         directory = directory.Parent;
      }

      return Path.GetFullPath(currentDirectory);
   }

   private string GetExecutablePath()
   {
      var configuredPath = options.ExecutablePath.Trim();

      if(!string.Equals(
         configuredPath,
         "opencode",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return configuredPath;
      }

      var userExecutablePath = Path.Combine(
         Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
         ".opencode",
         "bin",
         "opencode"
      );

      return File.Exists(userExecutablePath)
         ? userExecutablePath
         : configuredPath;
   }

   private static bool TryParseEvent(
      string line,
      out JsonObject eventNode
   )
   {
      eventNode = null!;

      try
      {
         eventNode = JsonNode.Parse(line) as JsonObject ?? null!;
         return eventNode is not null;
      }
      catch(JsonException)
      {
         return false;
      }
   }

   private static void ProcessEvent(
      JsonObject eventNode,
      JsonArray trace,
      IDictionary<int, JsonObject> assistantEntries,
      IDictionary<string, JsonObject> toolEntries,
      ref int currentTurn,
      ref string? finalMessage
   )
   {
      var eventType = GetString(eventNode, "type");

      if(string.Equals(
         eventType,
         StepStartEventType,
         StringComparison.Ordinal
      ))
      {
         currentTurn++;
         return;
      }

      if(currentTurn == 0)
      {
         currentTurn = 1;
      }

      if(string.Equals(
         eventType,
         ReasoningEventType,
         StringComparison.Ordinal
      ))
      {
         var reasoning = GetString(
            GetObject(eventNode, "part"),
            "text"
         );

         if(!string.IsNullOrWhiteSpace(reasoning))
         {
            var assistantEntry = GetOrCreateAssistantEntry(
               trace,
               assistantEntries,
               currentTurn
            );
            assistantEntry["reasoning_content"] = reasoning;
         }

         return;
      }

      if(string.Equals(
         eventType,
         TextEventType,
         StringComparison.Ordinal
      ))
      {
         var text = GetString(GetObject(eventNode, "part"), "text");

         if(!string.IsNullOrWhiteSpace(text))
         {
            var assistantEntry = GetOrCreateAssistantEntry(
               trace,
               assistantEntries,
               currentTurn
            );
            assistantEntry["content"] = text;
            finalMessage = text;
         }

         return;
      }

      if(string.Equals(
         eventType,
         ToolEventType,
         StringComparison.Ordinal
      ))
      {
         ProcessToolEvent(
            eventNode,
            trace,
            assistantEntries,
            toolEntries,
            currentTurn
         );
         return;
      }

      if(string.Equals(
         eventType,
         StepFinishEventType,
         StringComparison.Ordinal
      ))
      {
         var reason = GetString(
            GetObject(eventNode, "part"),
            "reason"
         );

         if(!string.IsNullOrWhiteSpace(reason))
         {
            var assistantEntry = GetOrCreateAssistantEntry(
               trace,
               assistantEntries,
               currentTurn
            );
            assistantEntry["finish_reason"] = reason;
         }
      }
   }

   private static void ProcessToolEvent(
      JsonObject eventNode,
      JsonArray trace,
      IDictionary<int, JsonObject> assistantEntries,
      IDictionary<string, JsonObject> toolEntries,
      int currentTurn
   )
   {
      var part = GetObject(eventNode, "part");
      var state = GetObject(part, "state");
      var name = GetString(part, "tool") ?? "opencode_tool";
      var callId = GetString(part, "callID") ??
         GetString(part, "id") ??
         Guid.NewGuid().ToString("N");
      var input = GetNode(state, "input");
      var output = GetString(state, "output") ??
         GetString(state, "error") ??
         string.Empty;
      var status = GetString(state, "status");
      var call = new JsonObject
      {
         ["id"] = callId,
         ["name"] = name,
         ["arguments"] = input?.DeepClone() ?? new JsonObject()
      };
      var assistantEntry = GetOrCreateAssistantEntry(
         trace,
         assistantEntries,
         currentTurn
      );
      var toolCalls = GetOrCreateArray(assistantEntry, "tool_calls");
      toolCalls.Add(call.DeepClone());

      var result = new JsonObject
      {
         ["kind"] = "tool",
         ["turn"] = currentTurn,
         ["tool_call_id"] = callId,
         ["name"] = name,
         ["arguments"] = input?.DeepClone() ?? new JsonObject(),
         ["result"] = output,
         ["status"] = status,
         ["provider_event"] = eventNode.DeepClone()
      };

      if(toolEntries.TryGetValue(callId, out var existingEntry))
      {
         var index = trace.IndexOf(existingEntry);

         if(index >= 0)
         {
            trace[index] = result;
         }
      }
      else
      {
         trace.Add(result);
      }

      toolEntries[callId] = result;
   }

   private static JsonObject GetOrCreateAssistantEntry(
      JsonArray trace,
      IDictionary<int, JsonObject> assistantEntries,
      int turn
   )
   {
      if(assistantEntries.TryGetValue(turn, out var entry))
      {
         return entry;
      }

      entry = new JsonObject
      {
         ["kind"] = "assistant",
         ["turn"] = turn
      };
      trace.Add(entry);
      assistantEntries[turn] = entry;
      return entry;
   }

   private static JsonArray GetOrCreateArray(
      JsonObject objectNode,
      string propertyName
   )
   {
      if(objectNode[propertyName] is JsonArray array)
      {
         return array;
      }

      var newArray = new JsonArray();
      objectNode[propertyName] = newArray;
      return newArray;
   }

   private static int CountToolCalls(JsonArray trace)
   {
      return trace
         .OfType<JsonObject>()
         .Count(entry => string.Equals(
            GetString(entry, "kind"),
            "tool",
            StringComparison.Ordinal
         ));
   }

   private static string CreateRawResponseJson(
      int? exitCode,
      string? standardOutput,
      string? standardError,
      JsonArray rawEvents,
      string? finalMessage
   )
   {
      var response = new JsonObject
      {
         ["exit_code"] = exitCode,
         ["stdout"] = standardOutput,
         ["stderr"] = standardError,
         ["final_message"] = finalMessage,
         ["events"] = rawEvents.DeepClone()
      };
      var usage = ExtractUsage(rawEvents);

      if(usage is not null)
      {
         response["usage"] = usage;
      }

      return response.ToJsonString();
   }

   private static JsonObject? ExtractUsage(JsonArray rawEvents)
   {
      var finishEvents = rawEvents
         .OfType<JsonObject>()
         .Where(entry => string.Equals(
            GetString(entry, "type"),
            StepFinishEventType,
            StringComparison.Ordinal
         ));
      var tokens = finishEvents
         .Select(entry => GetObject(
            GetObject(entry, "part"),
            "tokens"
         ))
         .LastOrDefault(value => value is not null);

      if(tokens is null)
      {
         return null;
      }

      var usage = new JsonObject();
      CopyNumber(tokens, usage, "input", "input_tokens");
      CopyNumber(tokens, usage, "output", "output_tokens");
      CopyNumber(tokens, usage, "reasoning", "reasoning_tokens");
      CopyNumber(tokens, usage, "total", "total_tokens");
      var cache = GetObject(tokens, "cache");

      if(cache is not null)
      {
         CopyNumber(
            cache,
            usage,
            "read",
            "cached_input_tokens"
         );
         CopyNumber(
            cache,
            usage,
            "write",
            "cache_write_input_tokens"
         );
      }

      return usage.Count == 0 ? null : usage;
   }

   private static void CopyNumber(
      JsonObject source,
      JsonObject target,
      string sourceName,
      string targetName
   )
   {
      if(source[sourceName] is JsonValue value)
      {
         target[targetName] = value.DeepClone();
      }
   }

   private static string CreateExitCodeMessage(
      OpenCodeCliProcessResult processResult
   )
   {
      var detail = string.IsNullOrWhiteSpace(processResult.StandardError)
         ? processResult.StandardOutput.Trim()
         : processResult.StandardError.Trim();

      return string.IsNullOrWhiteSpace(detail)
         ? $"OpenCode CLI exited with code {processResult.ExitCode}."
         : "OpenCode CLI exited with code " +
            $"{processResult.ExitCode}: {detail}";
   }

   private static string? GetString(
      JsonObject? objectNode,
      string propertyName
   )
   {
      if(objectNode is null ||
         objectNode[propertyName] is not JsonValue value)
      {
         return null;
      }

      try
      {
         return value.TryGetValue<string>(out var text)
            ? text
            : value.ToString();
      }
      catch(ArgumentException)
      {
         return null;
      }
   }

   private static JsonObject? GetObject(
      JsonObject? objectNode,
      string propertyName
   )
   {
      return objectNode?[propertyName] as JsonObject;
   }

   private static JsonNode? GetNode(
      JsonObject? objectNode,
      string propertyName
   )
   {
      return objectNode?[propertyName];
   }

   private static async Task ReportTraceAsync(
      JsonArray trace,
      int toolRoundCount,
      Func<string?, int, CancellationToken, Task>? toolTraceUpdated,
      CancellationToken cancellationToken
   )
   {
      if(toolTraceUpdated is null)
      {
         return;
      }

      await toolTraceUpdated(
         trace.ToJsonString(),
         toolRoundCount,
         cancellationToken
      );
   }
}

internal sealed record OpenCodeCliInvocation(
   string ExecutablePath,
   IReadOnlyList<string> Arguments,
   string Prompt,
   string WorkingDirectory,
   TimeSpan Timeout
);

internal sealed record OpenCodeCliProcessResult(
   int ExitCode,
   string StandardOutput,
   string StandardError
);

internal interface IOpenCodeCliProcessRunner
{
   Task<OpenCodeCliProcessResult> RunAsync(
      OpenCodeCliInvocation invocation,
      CancellationToken cancellationToken,
      Func<string, CancellationToken, Task> traceLineReceived
   );
}

internal sealed class OpenCodeCliProcessRunner : IOpenCodeCliProcessRunner
{
   public async Task<OpenCodeCliProcessResult> RunAsync(
      OpenCodeCliInvocation invocation,
      CancellationToken cancellationToken,
      Func<string, CancellationToken, Task> traceLineReceived
   )
   {
      var startInfo = new ProcessStartInfo
      {
         FileName = invocation.ExecutablePath,
         WorkingDirectory = invocation.WorkingDirectory,
         UseShellExecute = false,
         CreateNoWindow = true,
         RedirectStandardOutput = true,
         RedirectStandardError = true,
         StandardOutputEncoding = Encoding.UTF8,
         StandardErrorEncoding = Encoding.UTF8
      };

      foreach(var argument in invocation.Arguments)
      {
         startInfo.ArgumentList.Add(argument);
      }

      using var process = new Process
      {
         StartInfo = startInfo
      };
      process.Start();

      var standardOutputTask = ReadOutputAsync(
         process.StandardOutput,
         traceLineReceived,
         cancellationToken
      );
      var standardErrorTask = process.StandardError.ReadToEndAsync(
         cancellationToken
      );

      using var timeoutSource =
         CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
         );
      timeoutSource.CancelAfter(invocation.Timeout);

      try
      {
         await process.WaitForExitAsync(timeoutSource.Token);
      }
      catch(OperationCanceledException)
      {
         TryKill(process);

         if(cancellationToken.IsCancellationRequested)
         {
            throw;
         }

         throw new TimeoutException(
            "OpenCode CLI exceeded its " +
               $"{invocation.Timeout} timeout."
         );
      }

      var standardOutput = await standardOutputTask;
      var standardError = await standardErrorTask;

      return new OpenCodeCliProcessResult(
         process.ExitCode,
         standardOutput,
         standardError
      );
   }

   private static async Task<string> ReadOutputAsync(
      StreamReader reader,
      Func<string, CancellationToken, Task> lineReceived,
      CancellationToken cancellationToken
   )
   {
      var builder = new StringBuilder();

      while(await reader.ReadLineAsync(cancellationToken) is { } line)
      {
         builder.AppendLine(line);
         await lineReceived(line, cancellationToken);
      }

      return builder.ToString();
   }

   private static void TryKill(Process process)
   {
      try
      {
         if(!process.HasExited)
         {
            process.Kill(entireProcessTree: true);
         }
      }
      catch(InvalidOperationException)
      {
      }
   }
}
