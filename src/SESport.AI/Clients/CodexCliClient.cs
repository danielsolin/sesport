using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging;

using SESport.AI.Protocols;
using SESport.Core.AI;
using SESport.Core.Configuration;

namespace SESport.AI.Clients;

public sealed class CodexCliClient : IAiProviderClient
{
   private const string ExecCommand = "exec";
   private const string FullAccessArgument =
      "--dangerously-bypass-approvals-and-sandbox";
   private const string JsonArgument = "--json";
   private const string SearchArgument = "--search";
   private const string EphemeralArgument = "--ephemeral";
   private const string ColorArgument = "--color";
   private const string OutputArgument = "--output-last-message";
   private const string SchemaArgument = "--output-schema";
   private const string ModelArgument = "--model";
   private const string ChangeDirectoryArgument = "--cd";
   private const string SkipGitCheckArgument = "--skip-git-repo-check";
   private const string NeverColorValue = "never";
   private const string StdinArgument = "-";
   private const string AgentMessageType = "agent_message";
   private const string ItemCompletedType = "item.completed";
   private const string TurnCompletedType = "turn.completed";

   private readonly CodexCliOptions options;
   private readonly ICodexCliProcessRunner processRunner;
   private readonly ILogger<CodexCliClient>? logger;

   public CodexCliClient(
      CodexCliOptions options,
      ILogger<CodexCliClient>? logger = null
   )
      : this(options, new CodexCliProcessRunner(), logger)
   {
   }

   internal CodexCliClient(
      CodexCliOptions options,
      ICodexCliProcessRunner processRunner,
      ILogger<CodexCliClient>? logger = null
   )
   {
      this.options = options;
      this.processRunner = processRunner;
      this.logger = logger;
   }

   public string Kind => AiProviderKinds.CodexCli;

   public JsonObject CreateRequestPayload(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt
   )
   {
      var payload = new JsonObject
      {
         ["executable"] = options.ExecutablePath,
         ["command"] = ExecCommand,
         ["full_access"] = true,
         ["jsonl"] = true,
         ["ephemeral"] = true,
         ["live_web_search"] = job.RequiresWebSearch,
         ["working_directory"] = GetWorkingDirectory(),
         ["prompt"] = BuildAgentPrompt(job, prompt, renderedPrompt)
      };

      if(!string.IsNullOrWhiteSpace(provider.Model))
      {
         payload["model"] = provider.Model.Trim();
      }

      if(!string.IsNullOrWhiteSpace(prompt.OutputSchemaJson))
      {
         payload["output_schema"] = PrepareOutputSchemaForCodex(
            prompt.OutputSchemaJson
         );
      }

      return payload;
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
      var requestPayload = CreateRequestPayload(
         provider,
         job,
         prompt,
         renderedPrompt
      );
      var requestJson = requestPayload.ToJsonString();
      var trace = new JsonArray();
      var workingDirectory = GetWorkingDirectory();
      var promptText = BuildAgentPrompt(job, prompt, renderedPrompt);
      var temporaryDirectory = CreateTemporaryDirectory();
      var outputPath = Path.Combine(
         temporaryDirectory,
         "last-message.txt"
      );
      string? schemaPath = null;

      try
      {
         if(!string.IsNullOrWhiteSpace(prompt.OutputSchemaJson))
         {
            schemaPath = Path.Combine(
               temporaryDirectory,
               "output-schema.json"
            );
            await File.WriteAllTextAsync(
               schemaPath,
               PrepareOutputSchemaForCodex(
                  prompt.OutputSchemaJson
               ).ToJsonString(),
               new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
               cancellationToken
            );
         }

         var invocation = CreateInvocation(
            provider,
            promptText,
            workingDirectory,
            outputPath,
            schemaPath,
            job.RequiresWebSearch
         );
         CodexCliProcessResult processResult;

         try
         {
            processResult = await processRunner.RunAsync(
               invocation,
               cancellationToken,
               async (line, progressCancellationToken) =>
               {
                  AddTraceLine(trace, line);
                  var toolRoundCount = CountToolSteps(trace);
                  await ReportTraceAsync(
                     trace,
                     toolRoundCount,
                     toolTraceUpdated,
                     progressCancellationToken
                  );
               }
            );
         }
         catch(OperationCanceledException)
            when(cancellationToken.IsCancellationRequested)
         {
            throw;
         }
         catch(Exception exception)
         {
            var rawResponseJson = CreateRawResponseJson(
               null,
               null,
               exception.Message,
               trace
            );
            throw new AiProviderExecutionException(
               $"Codex CLI execution failed: {exception.Message}",
               exception,
               requestJson,
               rawResponseJson,
               trace.ToJsonString(),
               CountToolSteps(trace),
               promptText.Length
            );
         }

         var finalMessage = processResult.FinalMessage;
         var rawResponse = CreateRawResponseJson(
            processResult.ExitCode,
            processResult.StandardOutput,
            processResult.StandardError,
            trace,
            finalMessage
         );
         var toolTraceJson = trace.ToJsonString();
         var toolRoundCount = CountToolSteps(trace);

         if(processResult.ExitCode != 0)
         {
            throw new AiProviderExecutionException(
               CreateExitCodeMessage(processResult),
               null,
               requestJson,
               rawResponse,
               toolTraceJson,
               toolRoundCount,
               promptText.Length + processResult.StandardOutput.Length
            );
         }

         if(string.IsNullOrWhiteSpace(finalMessage))
         {
            finalMessage = ExtractFinalMessage(trace);
         }

         if(string.IsNullOrWhiteSpace(finalMessage))
         {
            throw new AiProviderExecutionException(
               "Codex CLI returned no final message.",
               null,
               requestJson,
               rawResponse,
               toolTraceJson,
               toolRoundCount,
               promptText.Length + processResult.StandardOutput.Length
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
               $"Codex CLI returned invalid output: {exception.Message}",
               exception,
               requestJson,
               rawResponse,
               toolTraceJson,
               toolRoundCount,
               promptText.Length + processResult.StandardOutput.Length
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
            promptText.Length + processResult.StandardOutput.Length,
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
            trace
         );
         throw new AiProviderExecutionException(
            $"Codex CLI processing failed: {exception.Message}",
            exception,
            requestJson,
            rawResponseJson,
            trace.ToJsonString(),
            CountToolSteps(trace),
            promptText.Length
         );
      }
      finally
      {
         TryDeleteTemporaryDirectory(temporaryDirectory);
      }
   }

   private CodexCliInvocation CreateInvocation(
      AiProviderDefinition provider,
      string promptText,
      string workingDirectory,
      string outputPath,
      string? schemaPath,
      bool requiresWebSearch
   )
   {
      var arguments = new List<string>();

      if(requiresWebSearch)
      {
         arguments.Add(SearchArgument);
      }

      arguments.Add(ExecCommand);
      arguments.Add(FullAccessArgument);
      arguments.Add(JsonArgument);
      arguments.Add(EphemeralArgument);
      arguments.Add(ColorArgument);
      arguments.Add(NeverColorValue);
      arguments.Add(OutputArgument);
      arguments.Add(outputPath);
      arguments.Add(ChangeDirectoryArgument);
      arguments.Add(workingDirectory);
      arguments.Add(SkipGitCheckArgument);

      if(!string.IsNullOrWhiteSpace(provider.Model))
      {
         arguments.Add(ModelArgument);
         arguments.Add(provider.Model.Trim());
      }

      if(schemaPath is not null)
      {
         arguments.Add(SchemaArgument);
         arguments.Add(schemaPath);
      }

      arguments.Add(StdinArgument);

      return new CodexCliInvocation(
         options.ExecutablePath,
         arguments,
         promptText,
         workingDirectory,
         outputPath,
         TimeSpan.FromSeconds(options.TimeoutSeconds)
      );
   }

   private string GetWorkingDirectory()
   {
      var configuredDirectory = options.WorkingDirectory?.Trim();
      var workingDirectory = string.IsNullOrWhiteSpace(configuredDirectory)
         ? Directory.GetCurrentDirectory()
         : configuredDirectory;

      return Path.GetFullPath(workingDirectory);
   }

   private static string BuildAgentPrompt(
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt
   )
   {
      var builder = new StringBuilder();
      builder.AppendLine(
         "You are the full Codex agent executing an SESport AI job."
      );
      builder.AppendLine($"Job ID: {job.Id}");
      builder.AppendLine(
         "Use the available Codex tools and repository context as needed."
      );
      builder.AppendLine(
         "Complete the configured task and return the final answer only."
      );
      builder.AppendLine();
      builder.AppendLine("Configured system instructions:");
      builder.AppendLine(renderedPrompt.SystemPrompt?.Trim() ?? "");
      builder.AppendLine();
      builder.AppendLine("Configured user task:");
      builder.AppendLine(renderedPrompt.UserPrompt.Trim());

      if(!string.IsNullOrWhiteSpace(prompt.OutputSchemaJson))
      {
         builder.AppendLine();
         builder.AppendLine(
            "Return only one response matching the supplied output schema."
         );
         builder.AppendLine(
            "Do not wrap the response in markdown or add commentary."
         );
      }

      return builder.ToString().Trim();
   }

   private static JsonNode PrepareOutputSchemaForCodex(
      string schemaJson
   )
   {
      var schema = JsonNode.Parse(schemaJson) ??
         throw new JsonException("Output schema must be a JSON value.");

      RemoveUnsupportedSchemaFormats(schema);
      return schema;
   }

   private static void RemoveUnsupportedSchemaFormats(JsonNode node)
   {
      if(node is JsonObject objectNode)
      {
         objectNode.Remove("format");

         foreach(var property in objectNode.ToList())
         {
            if(property.Value is not null)
            {
               RemoveUnsupportedSchemaFormats(property.Value);
            }
         }
      }
      else if(node is JsonArray arrayNode)
      {
         foreach(var child in arrayNode)
         {
            if(child is not null)
            {
               RemoveUnsupportedSchemaFormats(child);
            }
         }
      }
   }

   private static string CreateTemporaryDirectory()
   {
      var directory = Path.Combine(
         Path.GetTempPath(),
         "sesport-codex-" + Guid.NewGuid().ToString("N")
      );
      Directory.CreateDirectory(directory);
      return directory;
   }

   private void TryDeleteTemporaryDirectory(string directory)
   {
      try
      {
         Directory.Delete(directory, recursive: true);
      }
      catch(Exception exception)
      {
         logger?.LogWarning(
            exception,
            "Unable to remove Codex CLI temporary directory {Directory}.",
            directory
         );
      }
   }

   private static void AddTraceLine(JsonArray trace, string line)
   {
      if(string.IsNullOrWhiteSpace(line))
      {
         return;
      }

      try
      {
         var node = JsonNode.Parse(line);

         if(node is not null)
         {
            trace.Add(node);
            return;
         }
      }
      catch(JsonException)
      {
      }

      trace.Add(
         new JsonObject
         {
            ["type"] = "stdout",
            ["text"] = line
         }
      );
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

   private static int CountToolSteps(JsonArray trace)
   {
      return trace
         .OfType<JsonObject>()
         .Count(entry =>
            string.Equals(
               GetString(entry, "type"),
               ItemCompletedType,
               StringComparison.Ordinal
            ) &&
            entry["item"] is JsonObject item &&
            !string.Equals(
               GetString(item, "type"),
               AgentMessageType,
               StringComparison.Ordinal
            )
         );
   }

   private static string? ExtractFinalMessage(JsonArray trace)
   {
      return trace
         .OfType<JsonObject>()
         .Where(entry => string.Equals(
            GetString(entry, "type"),
            ItemCompletedType,
            StringComparison.Ordinal
         ))
         .Select(entry => entry["item"] as JsonObject)
         .Where(item => item is not null)
         .Where(item => string.Equals(
            GetString(item!, "type"),
            AgentMessageType,
            StringComparison.Ordinal
         ))
         .Select(item => GetString(item!, "text"))
         .LastOrDefault(text => !string.IsNullOrWhiteSpace(text));
   }

   private static string? GetString(JsonObject node, string propertyName)
   {
      try
      {
         return node[propertyName] is JsonValue value &&
            value.TryGetValue<string>(out var text)
               ? text
               : null;
      }
      catch(ArgumentException)
      {
         return null;
      }
   }

   private static string CreateRawResponseJson(
      int? exitCode,
      string? standardOutput,
      string? standardError,
      JsonArray trace,
      string? finalMessage = null
   )
   {
      var response = new JsonObject
      {
         ["exit_code"] = exitCode,
         ["stdout"] = standardOutput,
         ["stderr"] = standardError,
         ["final_message"] = finalMessage,
         ["trace"] = trace.DeepClone()
      };
      var usage = trace
         .OfType<JsonObject>()
         .Where(entry => string.Equals(
            GetString(entry, "type"),
            TurnCompletedType,
            StringComparison.Ordinal
         ))
         .Select(entry => entry["usage"])
         .LastOrDefault(value => value is JsonObject);

      if(usage is not null)
      {
         response["usage"] = usage.DeepClone();
      }

      return response.ToJsonString();
   }

   private static string CreateExitCodeMessage(
      CodexCliProcessResult processResult
   )
   {
      var detail = string.IsNullOrWhiteSpace(processResult.StandardError)
         ? processResult.StandardOutput.Trim()
         : processResult.StandardError.Trim();

      return string.IsNullOrWhiteSpace(detail)
         ? $"Codex CLI exited with code {processResult.ExitCode}."
         : $"Codex CLI exited with code {processResult.ExitCode}: {detail}";
   }
}

internal sealed record CodexCliInvocation(
   string ExecutablePath,
   IReadOnlyList<string> Arguments,
   string Prompt,
   string WorkingDirectory,
   string OutputPath,
   TimeSpan Timeout
);

internal sealed record CodexCliProcessResult(
   int ExitCode,
   string StandardOutput,
   string StandardError,
   string? FinalMessage
);

internal interface ICodexCliProcessRunner
{
   Task<CodexCliProcessResult> RunAsync(
      CodexCliInvocation invocation,
      CancellationToken cancellationToken,
      Func<string, CancellationToken, Task> traceLineReceived
   );
}

internal sealed class CodexCliProcessRunner : ICodexCliProcessRunner
{
   public async Task<CodexCliProcessResult> RunAsync(
      CodexCliInvocation invocation,
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
         RedirectStandardInput = true,
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

      try
      {
         await process.StandardInput.WriteAsync(
            invocation.Prompt.AsMemory(),
            cancellationToken
         );
         await process.StandardInput.FlushAsync(cancellationToken);
      }
      catch(IOException) when(process.HasExited)
      {
      }
      finally
      {
         process.StandardInput.Close();
      }

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
            $"Codex CLI exceeded its {invocation.Timeout} timeout."
         );
      }

      var standardOutput = await standardOutputTask;
      var standardError = await standardErrorTask;
      var finalMessage = File.Exists(invocation.OutputPath)
         ? await File.ReadAllTextAsync(
            invocation.OutputPath,
            cancellationToken
         )
         : null;

      return new CodexCliProcessResult(
         process.ExitCode,
         standardOutput,
         standardError,
         finalMessage
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
