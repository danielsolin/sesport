using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using SESport.AI.Interfaces;
using SESport.AI.Models;
using SESport.AI.Validation;

namespace SESport.AI.Providers;

public sealed class LlamaServerClient : IAiProviderClient
{
   // Rough character budget for the in-memory chat history.
   // Keep this comfortably below the llama-server token limit.
   private const int MaxConversationContextCharacters = 250000;
   private const int MaxTransientRetryAttempts = 12;
   private const int MaxFormatRepairAttempts = 3;
   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   )
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   public string Kind => "llama-server";

   public LlamaServerClient(
      HttpClient httpClient,
      IWebSearchClient webSearchClient,
      IWebPageContentClient webPageContentClient,
      ILogger<LlamaServerClient> logger,
      SearxngWebSearchClientOptions? searxngOptions = null
   )
   {
      HttpClient = httpClient;
      WebSearchClient = webSearchClient;
      WebPageContentClient = webPageContentClient;
      Logger = logger;
      SearxngOptions = searxngOptions;
   }

   private HttpClient HttpClient { get; }

   private IWebSearchClient WebSearchClient { get; }

   private IWebPageContentClient WebPageContentClient { get; }

   private ILogger<LlamaServerClient> Logger { get; }

   private SearxngWebSearchClientOptions? SearxngOptions { get; }

   public JsonObject CreateRequestPayload(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt
   )
   {
      return CreateRequestPayload(
         provider,
         job,
         prompt,
         renderedPrompt,
         includeTools: job.RequiresWebSearch
      );
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
      var request = CreateRequestPayload(
         provider,
         job,
         prompt,
         renderedPrompt
      );
      string? rawFinalRequestJson = null;
      JsonObject? responseJson = null;
      var rawResponse = "";
      var toolTrace = new JsonArray();
      var toolState = new ToolLoopState();
      var messages = (JsonArray)request["messages"]!;
      var baseSystemPrompt = renderedPrompt.SystemPrompt?.Trim();
      var toolRoundCount = 0;
      var turn = 0;
      var toolBudgetExhausted = false;
      var repeatedToolCallStreak = 0;
      var formatRepairAttempts = 0;

      try
      {
         if(string.IsNullOrWhiteSpace(provider.BaseAddress))
         {
            throw new InvalidOperationException(
               $"Provider '{provider.Id}' is missing a base address."
            );
         }

         if(string.IsNullOrWhiteSpace(provider.Model))
         {
            throw new InvalidOperationException(
               $"Provider '{provider.Id}' is missing a model."
            );
         }

         while(true)
         {
            turn++;
            ApplyTemperature(
               request,
               prompt.Temperature,
               repeatedToolCallStreak
            );
            ApplyToolBudgetPrompt(
               messages,
               baseSystemPrompt,
               prompt.MaxToolRounds,
               toolRoundCount
            );
            var payloadCharacterCount =
               LlamaConversationTrimmer.EstimateRequestPayloadSize(
                  request,
                  JsonOptions
               );
            toolTrace.Add(
               CreateToolBudgetTraceEntry(
                  turn,
                  prompt.MaxToolRounds,
                  toolRoundCount,
                  payloadCharacterCount,
                  GetRequestTemperature(request)
               )
            );
            await ReportToolTraceProgressAsync(
               toolTrace,
               toolRoundCount,
               toolTraceUpdated,
               cancellationToken
            );
            LogToolBudget(
               turn,
               prompt.MaxToolRounds,
               toolRoundCount
            );
            rawFinalRequestJson = AiRequestJsonSerializer.Serialize(request);
            var responseEnvelope = await SendWithStructuredOutputRepairAsync(
               provider,
               request,
               messages,
               toolTrace,
               turn,
               "turn",
               job.OutputMode,
               prompt,
               formatRepairAttempts,
               cancellationToken,
               () => formatRepairAttempts++
            );
            responseJson = responseEnvelope.ResponseJson;
            rawResponse = responseEnvelope.RawResponseJson;

            LogResponse("turn", turn, responseJson);

            if(!TryGetToolCalls(responseJson, out var toolCalls))
            {
               repeatedToolCallStreak = 0;
               toolTrace.Add(
                  CreateAssistantTraceEntry(turn, responseJson, [])
               );
               break;
            }

            toolRoundCount++;
            toolTrace.Add(
               CreateAssistantTraceEntry(turn, responseJson, toolCalls)
            );
            await ReportToolTraceProgressAsync(
               toolTrace,
               toolRoundCount,
               toolTraceUpdated,
               cancellationToken
            );
            AppendAssistantMessage(messages, responseJson);

            var repeatedToolCallDetectedThisTurn = false;
            var repeatedToolCallCountThisTurn = 0;
            foreach(var toolCall in toolCalls)
            {
               LogToolCall(turn, toolCall);

               if(TryGetRepeatedToolResult(
                  toolCall,
                  toolState,
                  out _
               ))
               {
                  repeatedToolCallDetectedThisTurn = true;
                  repeatedToolCallCountThisTurn++;
               }

               var toolResult = await ExecuteToolCallAsync(
                  toolCall,
                  toolState,
                  turn,
                  cancellationToken
               );

               messages.Add(
                  CreateToolMessage(toolCall.Id, toolResult)
               );

               toolTrace.Add(
                  CreateToolTraceEntry(
                     turn,
                     toolCall,
                     toolResult,
                     toolState.LastSearchProvider,
                     toolState.LastSearchProviderDetails,
                     toolState.LastSearchEngine,
                     toolState.LastPageFetcher
                  )
               );
               await ReportToolTraceProgressAsync(
                  toolTrace,
                  toolRoundCount,
                  toolTraceUpdated,
                  cancellationToken
               );
            }

            repeatedToolCallStreak = repeatedToolCallDetectedThisTurn
               ? repeatedToolCallStreak + repeatedToolCallCountThisTurn
               : 0;

            if(job.RequiresWebSearch)
            {
               request["tool_choice"] = "auto";
            }

            LlamaConversationTrimmer.TrimMessages(
               request,
               messages,
               MaxConversationContextCharacters,
               JsonOptions
            );

            if(prompt.MaxToolRounds is not null &&
               toolRoundCount >= prompt.MaxToolRounds.Value)
            {
               toolBudgetExhausted = true;
               break;
            }
         }

         if(toolBudgetExhausted)
         {
            ApplyToolBudgetPrompt(
               messages,
               baseSystemPrompt,
               prompt.MaxToolRounds,
               prompt.MaxToolRounds ?? 0
            );
            var payloadCharacterCount =
               LlamaConversationTrimmer.EstimateRequestPayloadSize(
                  request,
                  JsonOptions
               );
            toolTrace.Add(
               CreateToolBudgetTraceEntry(
                  turn + 1,
                  prompt.MaxToolRounds,
                  prompt.MaxToolRounds ?? 0,
                  payloadCharacterCount,
                  GetRequestTemperature(request)
               )
            );
            await ReportToolTraceProgressAsync(
               toolTrace,
               toolRoundCount,
               toolTraceUpdated,
               cancellationToken
            );
            LogToolBudget(
               turn + 1,
               prompt.MaxToolRounds,
               prompt.MaxToolRounds ?? 0
            );

            request = CreateFinalRequestPayload(
               request,
               job,
               prompt
            );
            rawFinalRequestJson = AiRequestJsonSerializer.Serialize(request);
            turn++;
            var finalEnvelope = await SendWithStructuredOutputRepairAsync(
               provider,
               request,
               messages,
               toolTrace,
               turn,
               "final",
               job.OutputMode,
               prompt,
               formatRepairAttempts,
               cancellationToken,
               () => formatRepairAttempts++
            );
            responseJson = finalEnvelope.ResponseJson;
            rawResponse = finalEnvelope.RawResponseJson;
         }

         var structuredOutputRepairAttempts = 0;

         while(true)
         {
            if(responseJson is null)
            {
               throw new InvalidOperationException(
                  "llama-server returned no response."
               );
            }

            var finalOutputText = NormalizeOutput(
               ExtractFinalText(responseJson)
            );

            try
            {
               finalOutputText =
                  ResponsesOutputValidator.ValidateStructuredOutput(
                     finalOutputText,
                     job.OutputMode,
                     prompt.OutputSchemaJson
                  );

               LogResponse("final", turn, responseJson);
               if(TryGetToolCalls(responseJson, out var finalToolCalls))
               {
                  toolTrace.Add(
                     CreateAssistantTraceEntry(
                        turn,
                        responseJson,
                        finalToolCalls,
                        validationStatus: "accepted"
                     )
                  );
               }
               else
               {
                  toolTrace.Add(
                     CreateAssistantTraceEntry(
                        turn,
                        responseJson,
                        [],
                        validationStatus: "accepted"
                     )
                  );
               }

               var toolTraceJson = toolTrace.Count == 0
                  ? null
                  : JsonSerializer.Serialize(toolTrace, JsonOptions);

                  return new AiJobResult(
                  Guid.NewGuid(),
                  job.Id,
                  provider.Id,
                  provider.Model,
                  renderedPrompt.ToPromptText(),
                  rawFinalRequestJson ?? string.Empty,
                  finalOutputText,
                  rawResponse,
                  toolTraceJson,
                  toolRoundCount,
                  LlamaConversationTrimmer.EstimateRequestPayloadSize(
                     request,
                     JsonOptions
                  ),
                  null,
                  null,
                  null,
                  null
               );
            }
            catch(InvalidOperationException exception) when (
               structuredOutputRepairAttempts < MaxFormatRepairAttempts &&
               IsInvalidStructuredOutputFailure(exception) &&
               CanRepairStructuredOutput(job.OutputMode, prompt)
            )
            {
               structuredOutputRepairAttempts++;
               toolTrace.Add(
                  CreateAssistantTraceEntry(
                     turn,
                     responseJson,
                     [],
                     validationStatus: "rejected",
                     validationError: exception.Message
                  )
               );
               toolTrace.Add(
                  CreateRepairPromptTraceEntry(
                     turn,
                     GetStructuredOutputRepairPrompt()
                  )
               );
               await ReportToolTraceProgressAsync(
                  toolTrace,
                  toolRoundCount,
                  toolTraceUpdated,
                  cancellationToken
               );
               ApplyStructuredOutputRepairPrompt(messages);
               request = CreateFinalRequestPayload(
                  request,
                  job,
                  prompt
               );
               rawFinalRequestJson = AiRequestJsonSerializer.Serialize(request);
               turn++;
               var finalEnvelope = await SendWithStructuredOutputRepairAsync(
                  provider,
                  request,
                  messages,
                  toolTrace,
                  turn,
                  "final",
                  job.OutputMode,
                  prompt,
                  formatRepairAttempts,
                  cancellationToken,
                  () => formatRepairAttempts++
               );
               responseJson = finalEnvelope.ResponseJson;
               rawResponse = finalEnvelope.RawResponseJson;
            }
         }
      }
      catch(OperationCanceledException)
         when(cancellationToken.IsCancellationRequested)
      {
         throw;
      }
      catch(Exception exception)
      {
         var toolTraceJson = toolTrace.Count == 0
            ? null
            : JsonSerializer.Serialize(toolTrace, JsonOptions);

         throw new AiProviderExecutionException(
            exception.Message,
            exception,
            rawFinalRequestJson,
            string.IsNullOrWhiteSpace(rawResponse) ? null : rawResponse,
            toolTraceJson,
            toolRoundCount,
            LlamaConversationTrimmer.EstimateRequestPayloadSize(
               request,
               JsonOptions
            )
         );
      }
   }

   private async Task<HttpResponseMessage> SendAsync(
      AiProviderDefinition provider,
      JsonObject request,
      CancellationToken cancellationToken
   )
   {
      var requestMessage = new HttpRequestMessage(
         HttpMethod.Post,
         new Uri(new Uri(provider.BaseAddress!), "chat/completions")
      );

      requestMessage.Content = JsonContent.Create(
         request,
         options: JsonOptions
      );

      var apiKey = ApiKeySourceResolver.Resolve(provider.ApiKeySource);

      if(!string.IsNullOrWhiteSpace(apiKey))
      {
         requestMessage.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
      }

      return await HttpClient.SendAsync(requestMessage, cancellationToken);
   }

   private async Task<ResponseEnvelope> SendWithStructuredOutputRepairAsync(
      AiProviderDefinition provider,
      JsonObject request,
      JsonArray messages,
      JsonArray toolTrace,
      int turn,
      string stage,
      string outputMode,
      AiPromptDefinition prompt,
      int formatRepairAttempts,
      CancellationToken cancellationToken,
      Action incrementFormatRepairAttempts
   )
   {
      try
      {
         var responseEnvelope = await SendWithRetryAsync(
            provider,
            request,
            turn,
            stage,
            cancellationToken
         );

         if(ShouldRepairStructuredOutput(stage, outputMode, prompt))
         {
            ValidateStructuredOutput(
               responseEnvelope,
               outputMode,
               prompt
            );
         }

         return responseEnvelope;
      }
         catch(HttpRequestException exception) when (
         formatRepairAttempts < MaxFormatRepairAttempts &&
         IsPegNativeFormatFailure(exception) &&
         CanRepairStructuredOutput(outputMode, prompt)
      )
      {
         incrementFormatRepairAttempts();
         ApplyStructuredOutputRepairPrompt(messages);
         toolTrace.Add(
            CreateRepairPromptTraceEntry(
               turn,
               GetStructuredOutputRepairPrompt()
            )
         );
         return await SendWithStructuredOutputRepairAsync(
            provider,
            request,
            messages,
            toolTrace,
            turn,
            stage,
            outputMode,
            prompt,
            formatRepairAttempts + 1,
            cancellationToken,
            incrementFormatRepairAttempts
         );
      }
      catch(InvalidOperationException exception) when (
         formatRepairAttempts < MaxFormatRepairAttempts &&
         IsInvalidStructuredOutputFailure(exception) &&
         CanRepairStructuredOutput(outputMode, prompt) &&
         string.Equals(stage, "final", StringComparison.Ordinal)
      )
      {
         incrementFormatRepairAttempts();
         ApplyStructuredOutputRepairPrompt(messages);
         var repairedEnvelope = await SendWithStructuredOutputRepairAsync(
            provider,
            request,
            messages,
            toolTrace,
            turn,
            stage,
            outputMode,
            prompt,
            formatRepairAttempts + 1,
            cancellationToken,
            incrementFormatRepairAttempts
         );

         if(ShouldRepairStructuredOutput(stage, outputMode, prompt))
         {
            ValidateStructuredOutput(
               repairedEnvelope,
               outputMode,
               prompt
            );
         }

         return repairedEnvelope;
      }
   }

   private static void ValidateStructuredOutput(
      ResponseEnvelope responseEnvelope,
      string outputMode,
      AiPromptDefinition prompt
   )
   {
      var outputText = NormalizeOutput(
         ExtractFinalText(responseEnvelope.ResponseJson)
      );

      ResponsesOutputValidator.ValidateStructuredOutput(
         outputText,
         outputMode,
         prompt.OutputSchemaJson
      );
   }

   private static bool CanRepairStructuredOutput(
      string outputMode,
      AiPromptDefinition prompt
   )
   {
      return !string.IsNullOrWhiteSpace(prompt.OutputSchemaJson) ||
         string.Equals(
            outputMode,
            "json_object",
            StringComparison.OrdinalIgnoreCase
         );
   }

   private static bool IsPegNativeFormatFailure(Exception exception)
   {
      return exception.Message.Contains(
         "peg-native format",
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static bool IsInvalidStructuredOutputFailure(
      Exception exception
   )
   {
      return exception.Message.Contains(
         "invalid json_schema output",
         StringComparison.OrdinalIgnoreCase
      ) || exception.Message.Contains(
         "invalid json_object output",
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static bool ShouldRepairStructuredOutput(
      string stage,
      string outputMode,
      AiPromptDefinition prompt
   )
   {
      return string.Equals(stage, "final", StringComparison.Ordinal) &&
         CanRepairStructuredOutput(outputMode, prompt);
   }

   private static void ApplyStructuredOutputRepairPrompt(JsonArray messages)
   {
      var repairPrompt = GetStructuredOutputRepairPrompt();

      var repairMessage = new JsonObject
      {
         ["role"] = "system",
         ["content"] = repairPrompt
      };

      var insertionIndex =
         LlamaConversationTrimmer.FindPrimarySystemMessageIndex(messages);

      if(insertionIndex < 0)
      {
         messages.Insert(0, repairMessage);
         return;
      }

      messages.Insert(insertionIndex + 1, repairMessage);
   }

   private static JsonObject CreateRepairPromptTraceEntry(
      int turn,
      string repairPrompt
   )
   {
      return new JsonObject
      {
         ["kind"] = "repair_prompt",
         ["turn"] = turn,
         ["content"] = repairPrompt
      };
   }

   private static string GetStructuredOutputRepairPrompt()
   {
      return """
         The previous response was rejected because it was not valid JSON.
         Return only the raw JSON object required by the schema.
         Do not use markdown, fences, tool calls, commentary, or
         explanations.
         """.Trim();
   }

   private async Task<ResponseEnvelope> SendWithRetryAsync(
      AiProviderDefinition provider,
      JsonObject request,
      int turn,
      string stage,
      CancellationToken cancellationToken
   )
   {
      for(var attempt = 1; attempt <= MaxTransientRetryAttempts; attempt++)
      {
         var rawResponse = "";

         try
         {
            using var response = await SendAsync(
               provider,
               request,
               cancellationToken
            );

            rawResponse = await response.Content.ReadAsStringAsync(
               cancellationToken
            );

            if(!response.IsSuccessStatusCode)
            {
               if(IsTransientFailure(response.StatusCode, rawResponse) &&
                  attempt < MaxTransientRetryAttempts)
               {
                  await DelayTransientRetryAsync(
                     stage,
                     turn,
                     attempt,
                     CreateFailureMessage(
                        response.StatusCode,
                        rawResponse
                     ),
                     cancellationToken
                  );
                  continue;
               }

               throw new HttpRequestException(
                  CreateFailureMessage(response.StatusCode, rawResponse),
                  null,
                  response.StatusCode
               );
            }

            var responseJson = JsonDocument.Parse(rawResponse).RootElement
               .Deserialize<JsonObject>(JsonOptions);

            if(responseJson is null)
            {
               throw new InvalidOperationException(
                  "llama-server returned an empty response."
               );
            }

            return new ResponseEnvelope(responseJson, rawResponse);
         }
         catch(Exception exception) when (
            IsTransientFailure(exception, rawResponse, cancellationToken) &&
            attempt < MaxTransientRetryAttempts)
         {
            await DelayTransientRetryAsync(
               stage,
               turn,
               attempt,
               exception.Message,
               cancellationToken
            );
         }
      }

      throw new InvalidOperationException(
         "llama-server stayed unavailable after retrying."
      );
   }

   private async Task DelayTransientRetryAsync(
      string stage,
      int turn,
      int attempt,
      string reason,
      CancellationToken cancellationToken
   )
   {
      var delay = GetTransientRetryDelay(attempt);

      Logger.LogWarning(
         "llama-server {Stage}:{Turn} attempt {Attempt} failed with " +
         "{Reason}. Retrying in {Delay}.",
         stage,
         turn,
         attempt,
         reason,
         delay
      );

      await Task.Delay(delay, cancellationToken);
   }

   private static bool IsTransientFailure(
      HttpStatusCode statusCode,
      string rawResponse
   )
   {
      return statusCode == HttpStatusCode.ServiceUnavailable ||
         rawResponse.Contains(
            "Loading model",
            StringComparison.OrdinalIgnoreCase
         );
   }

   private static bool IsTransientFailure(
      Exception exception,
      string rawResponse,
      CancellationToken cancellationToken
   )
   {
      if(exception is HttpRequestException httpRequestException)
      {
         if(httpRequestException.StatusCode is not null)
         {
            return IsTransientFailure(
               httpRequestException.StatusCode.Value,
               rawResponse
            );
         }

         return true;
      }

      if(exception is TaskCanceledException &&
         !cancellationToken.IsCancellationRequested)
      {
         return true;
      }

      if(exception is IOException)
      {
         return true;
      }

      return rawResponse.Contains(
         "Loading model",
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static TimeSpan GetTransientRetryDelay(int attempt)
   {
      var seconds = attempt switch
      {
         1 => 1,
         2 => 2,
         3 => 4,
         4 => 8,
         5 => 16,
         _ => 30
      };

      return TimeSpan.FromSeconds(seconds);
   }

   private void LogResponse(
      string stage,
      int step,
      JsonObject response
   )
   {
      if(!Logger.IsEnabled(LogLevel.Debug))
      {
         return;
      }

      var finishReason = GetFinishReason(response);
      var reasoningContent = ExtractReasoningContent(response);
      var content = NormalizeOutput(ExtractFinalText(response));
      var toolCalls = ExtractToolCallNames(response);

      Logger.LogDebug(
         "llama-server {Stage}:{Step} finish_reason={FinishReason} " +
         "reasoning={HasReasoning} tool_calls={ToolCalls} content={Content}",
         stage,
         step,
         string.IsNullOrWhiteSpace(finishReason) ? "null" : finishReason,
         string.IsNullOrWhiteSpace(reasoningContent) ? "false" : "true",
         toolCalls.Length == 0 ? "[]" : string.Join(",", toolCalls),
         TruncateForLog(content, 800)
      );
   }

   private static JsonObject CreateAssistantTraceEntry(
      int turn,
      JsonObject response,
      IReadOnlyList<ToolCall> toolCalls,
      string? validationStatus = null,
      string? validationError = null
   )
   {
      return new JsonObject
      {
         ["kind"] = "assistant",
         ["turn"] = turn,
         ["finish_reason"] = GetFinishReason(response),
         ["content"] = NormalizeOutput(ExtractFinalText(response)),
         ["validation_status"] = validationStatus,
         ["validation_error"] = validationError,
         ["tool_calls"] = JsonSerializer.SerializeToNode(
            toolCalls.Select(toolCall => new
            {
               id = toolCall.Id,
               name = toolCall.Name,
               arguments = toolCall.Arguments
            }).ToArray(),
            JsonOptions
         )
      };
   }

   private static JsonObject CreateToolBudgetTraceEntry(
      int turn,
      int? maxToolRounds,
      int toolRoundCount,
      int payloadCharacterCount,
      decimal? temperature
   )
   {
      if(maxToolRounds is null)
      {
         return new JsonObject
         {
            ["kind"] = "budget",
            ["turn"] = turn,
            ["enabled"] = false,
            ["temperature"] = temperature
         };
      }

      var remainingToolCalls = Math.Max(maxToolRounds.Value - toolRoundCount,
         0);

      return new JsonObject
      {
         ["kind"] = "budget",
         ["turn"] = turn,
         ["enabled"] = true,
         ["remaining"] = remainingToolCalls,
         ["max"] = maxToolRounds.Value,
         ["payload_chars"] = payloadCharacterCount,
         ["temperature"] = temperature,
         ["content"] = $"Tool calls remaining: {remainingToolCalls} of " +
            $"{maxToolRounds.Value}."
      };
   }

   private static decimal? GetRequestTemperature(JsonObject request)
   {
      if(!request.TryGetPropertyValue("temperature", out var value))
      {
         return null;
      }

      return value is JsonValue jsonValue &&
         jsonValue.TryGetValue<decimal>(out var temperature)
         ? temperature
         : null;
   }

   internal static decimal? GetEffectiveTemperature(
      decimal? baseTemperature,
      int repeatedToolCallStreak
   )
   {
      if(baseTemperature is null)
      {
         return null;
      }

      if(repeatedToolCallStreak <= 0)
      {
         return baseTemperature;
      }

      var adjustedTemperature = 0.15m + (repeatedToolCallStreak - 1) * 0.05m;
      adjustedTemperature = Math.Min(adjustedTemperature, 0.6m);

      return Math.Max(baseTemperature.Value, adjustedTemperature);
   }

   private static void ApplyTemperature(
      JsonObject request,
      decimal? baseTemperature,
      int repeatedToolCallStreak
   )
   {
      var effectiveTemperature = GetEffectiveTemperature(
         baseTemperature,
         repeatedToolCallStreak
      );

      if(effectiveTemperature is null)
      {
         return;
      }

      request["temperature"] = effectiveTemperature.Value;
   }

   private static JsonObject CreateToolTraceEntry(
      int turn,
      ToolCall toolCall,
      string toolResult,
      string? searchProvider = null,
      string? searchProviderDetails = null,
      string? searchEngine = null,
      string? pageFetcher = null
   )
   {
      var isSearchTool = string.Equals(
         toolCall.Name,
         WebToolNames.Search,
         StringComparison.Ordinal
      );

      var isGetPageTool = string.Equals(
         toolCall.Name,
         WebToolNames.GetPage,
         StringComparison.Ordinal
      );
      var isFindInPageTool = string.Equals(
         toolCall.Name,
         WebToolNames.FindInPage,
         StringComparison.Ordinal
      );
      var find = ExtractFind(toolCall.Arguments);

      return new JsonObject
      {
         ["kind"] = "tool",
         ["turn"] = turn,
         ["tool_call_id"] = toolCall.Id,
         ["name"] = toolCall.Name,
         ["arguments"] = toolCall.Arguments,
         ["query"] = isSearchTool ? ExtractQuery(toolCall.Arguments) : null,
         ["limit"] = isSearchTool ? ExtractLimit(toolCall.Arguments) : null,
         ["url"] = isGetPageTool || isFindInPageTool
            ? ExtractUrl(toolCall.Arguments)
            : null,
         ["find"] = isFindInPageTool || !string.IsNullOrWhiteSpace(find)
            ? find
            : null,
         ["search_provider"] = isSearchTool ? searchProvider : null,
         ["search_provider_details"] = isSearchTool
            ? searchProviderDetails
            : null,
         ["search_engine"] = isSearchTool ? searchEngine : null,
         ["fetcher"] = isGetPageTool || isFindInPageTool
            ? pageFetcher
            : null,
         ["result"] = toolResult
      };
   }

   private static async Task ReportToolTraceProgressAsync(
      JsonArray toolTrace,
      int toolRoundCount,
      Func<string?, int, CancellationToken, Task>? toolTraceUpdated,
      CancellationToken cancellationToken
   )
   {
      if(toolTraceUpdated is null)
      {
         return;
      }

      var toolTraceJson = toolTrace.Count == 0
         ? null
         : JsonSerializer.Serialize(toolTrace, JsonOptions);

      await toolTraceUpdated(toolTraceJson, toolRoundCount, cancellationToken);
   }

   private static string? GetFinishReason(JsonObject response)
   {
      if(!response.TryGetPropertyValue("choices", out var choicesNode) ||
         choicesNode is not JsonArray choices ||
         choices.Count == 0 ||
         choices[0] is not JsonObject choice ||
         !choice.TryGetPropertyValue("finish_reason", out var finishReasonNode))
      {
         return null;
      }

      return finishReasonNode is JsonValue value &&
         value.TryGetValue<string>(out var finishReason)
         ? finishReason
         : finishReasonNode is null
            ? ""
            : finishReasonNode.ToJsonString();
   }

   private static string ExtractReasoningContent(JsonObject response)
   {
      if(!response.TryGetPropertyValue("choices", out var choicesNode) ||
         choicesNode is not JsonArray choices ||
         choices.Count == 0 ||
         choices[0] is not JsonObject choice ||
         !choice.TryGetPropertyValue("message", out var messageNode) ||
         messageNode is not JsonObject message ||
         !message.TryGetPropertyValue("reasoning_content",
            out var reasoningNode))
      {
         return "";
      }

      return reasoningNode is JsonValue value &&
         value.TryGetValue<string>(out var reasoningContent)
         ? reasoningContent
         : reasoningNode is null
            ? ""
            : reasoningNode.ToJsonString();
   }

   private static string[] ExtractToolCallNames(JsonObject response)
   {
      if(!TryGetToolCalls(response, out var toolCalls))
      {
         return [];
      }

      return toolCalls.Select(toolCall => toolCall.Name).ToArray();
   }

   private static string TruncateForLog(string value, int maxLength)
   {
      if(value.Length <= maxLength)
      {
         return value;
      }

      return value[..maxLength] + "...";
   }

   private void LogToolCall(
      int step,
      ToolCall toolCall
   )
   {
      if(!Logger.IsEnabled(LogLevel.Debug))
      {
         return;
      }

      var query = ExtractQuery(toolCall.Arguments);
      var limit = ExtractLimit(toolCall.Arguments);
      var find = ExtractFind(toolCall.Arguments);

      Logger.LogDebug(
         "llama-server tool:{Step} name={Name} query={Query} " +
         "limit={Limit} find={Find}",
         step,
         toolCall.Name,
         TruncateForLog(query, 240),
         limit,
         TruncateForLog(find, 120)
      );
   }

   private void LogSearchResults(
      string query,
      int limit,
      IReadOnlyList<WebSearchResult> searchResults,
      string? searchProvider
   )
   {
      if(!Logger.IsEnabled(LogLevel.Debug))
      {
         return;
      }

      var firstResult = searchResults.Count == 0
         ? "none"
         : $"{searchResults[0].Title} | {searchResults[0].Url}";

      Logger.LogDebug(
         "llama-server search provider={SearchProvider} query={Query} " +
         "limit={Limit} results={ResultCount} first_result={FirstResult}",
         string.IsNullOrWhiteSpace(searchProvider)
            ? "unknown"
            : searchProvider,
         TruncateForLog(query, 240),
         limit,
         searchResults.Count,
         TruncateForLog(firstResult, 240)
      );
   }

   private JsonObject CreateRequestPayload(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt,
      bool includeTools
   )
   {
      var payload = CreateBaseRequestPayload(
         provider,
         job,
         prompt,
         renderedPrompt,
         includeTools
      );

      if(includeTools)
      {
         payload["tools"] = CreateToolsArray(job.ToolsJson);
         payload["tool_choice"] = "required";
      }

      MergeRequestOptions(payload, provider.RequestOptionsJson);
      MergeRequestOptions(payload, prompt.RequestOptionsJson);
      return payload;
   }

   private static JsonObject CreateFinalRequestPayload(
      JsonObject request,
      AiJobDefinition job,
      AiPromptDefinition prompt
   )
   {
      var finalRequest = (JsonObject)request.DeepClone();
      finalRequest.Remove("tools");
      finalRequest.Remove("tool_choice");

      ResponsesRequestFormat.Apply(
         finalRequest,
         job.OutputMode,
         prompt.OutputSchemaJson,
         $"prompt_{prompt.Id:N}"
      );

      return finalRequest;
   }

   private static void ApplyToolBudgetPrompt(
      JsonArray messages,
      string? baseSystemPrompt,
      int? maxToolRounds,
      int toolRoundCount
   )
   {
      if(maxToolRounds is null)
      {
         return;
      }

      var remainingToolCalls = Math.Max(maxToolRounds.Value - toolRoundCount,
         0);
      var budgetPrompt = $"Tool calls remaining: {remainingToolCalls} of " +
         $"{maxToolRounds.Value}.";
      var systemPrompt = string.IsNullOrWhiteSpace(baseSystemPrompt)
         ? budgetPrompt
         : $"{baseSystemPrompt}{Environment.NewLine}{Environment.NewLine}" +
            budgetPrompt;
      var systemMessage = new JsonObject
      {
         ["role"] = "system",
         ["content"] = systemPrompt
      };

      var systemIndex =
         LlamaConversationTrimmer.FindPrimarySystemMessageIndex(messages);

      if(systemIndex < 0)
      {
         messages.Insert(0, systemMessage);
         return;
      }

      messages[systemIndex] = systemMessage;
   }

   private void LogToolBudget(
      int turn,
      int? maxToolRounds,
      int toolRoundCount
   )
   {
      if(maxToolRounds is null || !Logger.IsEnabled(LogLevel.Debug))
      {
         return;
      }

      var remainingToolCalls = Math.Max(maxToolRounds.Value - toolRoundCount,
         0);
      var prompt = $"Tool calls remaining: {remainingToolCalls} of " +
         $"{maxToolRounds.Value}.";

      Logger.LogDebug(
         "llama-server turn:{Turn} tool_budget={Remaining}/{Max} " +
         "system_prompt={SystemPrompt}",
         turn,
         remainingToolCalls,
         maxToolRounds.Value,
         TruncateForLog(prompt, 120)
      );
   }

   private JsonObject CreateBaseRequestPayload(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt,
      bool includeTools
   )
   {
      var payload = new JsonObject
      {
         ["model"] = provider.Model
      };

      payload["messages"] = CreateMessages(
         renderedPrompt
      );

      if(prompt.MaxOutputTokens is not null)
      {
         payload["max_tokens"] = prompt.MaxOutputTokens.Value;
      }

      if(prompt.Temperature is not null)
      {
         payload["temperature"] = prompt.Temperature.Value;
      }

      if(!includeTools)
      {
         ResponsesRequestFormat.Apply(
            payload,
            job.OutputMode,
            prompt.OutputSchemaJson,
            $"prompt_{prompt.Id:N}"
         );
      }

      return payload;
   }

   private static JsonArray CreateMessages(
      AiRenderedPrompt renderedPrompt
   )
   {
      var messages = new JsonArray();
      var systemPrompt = renderedPrompt.SystemPrompt?.Trim();

      if(!string.IsNullOrWhiteSpace(systemPrompt))
      {
         messages.Add(
            new JsonObject
            {
               ["role"] = "system",
               ["content"] = systemPrompt
            }
         );
      }

      messages.Add(
         new JsonObject
         {
            ["role"] = "user",
            ["content"] = renderedPrompt.UserPrompt.Trim()
         }
      );

      return messages;
   }

   private static JsonArray CreateToolsArray(string? toolsJson)
   {
      if(string.IsNullOrWhiteSpace(toolsJson))
      {
         throw new InvalidOperationException(
            "Tool usage is enabled but no tools JSON was configured."
         );
      }

      var tools = JsonNode.Parse(toolsJson) as JsonArray;

      if(tools is null)
      {
         throw new InvalidOperationException(
            "Configured tools JSON must be a JSON array."
         );
      }

      return tools;
   }

   private async Task<string> ExecuteToolCallAsync(
      ToolCall toolCall,
      ToolLoopState toolState,
      int turn,
      CancellationToken cancellationToken
   )
   {
      if(string.Equals(
         toolCall.Name,
         WebToolNames.Search,
         StringComparison.Ordinal
      ))
      {
         return await ExecuteSearchToolCallAsync(
            toolCall,
            toolState,
            turn,
            cancellationToken
         );
      }

      if(TryGetRepeatedToolResult(
         toolCall,
         toolState,
         out var repeatedResult
      ))
      {
         return repeatedResult;
      }

      if(string.Equals(
         toolCall.Name,
         WebToolNames.GetPage,
         StringComparison.Ordinal
      ))
      {
         var url = ExtractUrl(toolCall.Arguments);

         var pageResult = await FormatPageContentAsync(
            url,
            toolState,
            turn,
            cancellationToken
         );

         RecordToolCallResult(
            toolCall,
            toolState,
            turn,
            pageResult
         );
         return pageResult;
      }

      if(string.Equals(
         toolCall.Name,
         WebToolNames.FindInPage,
         StringComparison.Ordinal
      ))
      {
         var url = ExtractUrl(toolCall.Arguments);
         var find = ExtractFind(toolCall.Arguments);

         var result = await FormatPageFindResultsAsync(
            url,
            find,
            toolState,
            turn,
            cancellationToken
         );

         RecordToolCallResult(
            toolCall,
            toolState,
            turn,
            result
         );
         return result;
      }

      throw new InvalidOperationException(
         $"Unsupported tool call '{toolCall.Name}'."
      );
   }

   private async Task<string> ExecuteSearchToolCallAsync(
      ToolCall toolCall,
      ToolLoopState toolState,
      int turn,
      CancellationToken cancellationToken
   )
   {
      var query = ExtractQuery(toolCall.Arguments);
      var limit = ExtractLimit(toolCall.Arguments);
      var signature = BuildToolCallSignature(toolCall);
      var searchAttempt = toolState.SearchCallCount;
      var engineCount = SearxngSearchEngineRotation.GetEngineCount(
         SearxngOptions?.Engines
      );
      var repeatedSearchCount = toolState.SearchAttemptCounts.TryGetValue(
         signature,
         out var existingAttempt
      )
         ? existingAttempt
         : 0;

      if(repeatedSearchCount >= engineCount)
      {
         return CreateRepeatedToolReplayMessage(
            toolCall.Name,
            toolState.ToolCallHistory.TryGetValue(
               signature,
               out var record
            )
               ? record.Result
               : ""
         );
      }

      var searchResponse = await WebSearchClient.SearchAsync(
         query,
         limit,
         cancellationToken,
         searchAttempt
      );
      var searchResults = searchResponse.Results;
      toolState.LastSearchProvider = searchResponse.Provider;
      toolState.LastSearchProviderDetails = searchResponse.Details;
      toolState.LastSearchEngine = GetSearchEngine(searchResponse.Details);
      toolState.SearchCallCount = searchAttempt + 1;
      toolState.SearchAttemptCounts[signature] = repeatedSearchCount + 1;

      LogSearchResults(
         query,
         limit,
         searchResults,
         toolState.LastSearchProvider
      );

      var result = FormatSearchResults(
         searchResults
      );

      RecordToolCallResult(
         toolCall,
         toolState,
         turn,
         result
      );
      return result;
   }

   private static string? GetSearchEngine(string? searchProviderDetails)
   {
      if(string.IsNullOrWhiteSpace(searchProviderDetails))
      {
         return null;
      }

      const string prefix = "engines=";

      return searchProviderDetails.StartsWith(
         prefix,
         StringComparison.OrdinalIgnoreCase
      )
         ? searchProviderDetails[prefix.Length..]
         : null;
   }

   private static bool TryGetToolCalls(
      JsonObject response,
      out IReadOnlyList<ToolCall> toolCalls
   )
   {
      toolCalls = [];

      if(
         !response.TryGetPropertyValue("choices", out var choicesNode) ||
         choicesNode is not JsonArray choices ||
         choices.Count == 0 ||
         choices[0] is not JsonObject choice ||
         !choice.TryGetPropertyValue("message", out var messageNode) ||
         messageNode is not JsonObject message ||
         !message.TryGetPropertyValue("tool_calls", out var toolCallsNode) ||
         toolCallsNode is not JsonArray toolCallsArray ||
         toolCallsArray.Count == 0
      )
      {
         return false;
      }

      var parsedToolCalls = new List<ToolCall>();

      foreach(var toolCallNode in toolCallsArray)
      {
         if(toolCallNode is not JsonObject toolCallObject)
         {
            continue;
         }

         if(
            !toolCallObject.TryGetPropertyValue("id", out var idNode) ||
            idNode is not JsonValue idValue ||
            idValue.TryGetValue<string>(out var id) == false ||
            string.IsNullOrWhiteSpace(id) ||
            !toolCallObject.TryGetPropertyValue(
               "function",
               out var functionNode
            ) ||
            functionNode is not JsonObject functionObject ||
            !functionObject.TryGetPropertyValue("name", out var nameNode) ||
            nameNode is not JsonValue nameValue ||
            nameValue.TryGetValue<string>(out var name) == false ||
            string.IsNullOrWhiteSpace(name)
         )
         {
            continue;
         }

         var arguments = "";

         if(
            functionObject.TryGetPropertyValue(
               "arguments",
               out var argumentsNode
            ) &&
            argumentsNode is not null
         )
         {
            arguments = argumentsNode.ToJsonString();

            if(argumentsNode is JsonValue argumentsValue &&
               argumentsValue.TryGetValue<string>(out var stringArguments))
            {
               arguments = stringArguments;
            }
         }

         parsedToolCalls.Add(new ToolCall(id, name, arguments));
      }

      toolCalls = parsedToolCalls;
      return toolCalls.Count > 0;
   }

   private static void AppendAssistantMessage(
      JsonArray messages,
      JsonObject response
   )
   {
      messages.Add(CreateAssistantMessage(response));
   }

   private static JsonObject CreateAssistantMessage(JsonObject response)
   {
      var hasToolCalls = TryGetAssistantToolCalls(response, out var toolCalls);
      var assistantMessage = new JsonObject
      {
         ["role"] = "assistant",
         ["content"] = hasToolCalls
            ? ""
            : ExtractMessageContent(response)
      };

      if(hasToolCalls)
      {
         assistantMessage["tool_calls"] = JsonSerializer.SerializeToNode(
            toolCalls.Select(toolCall => new
            {
               id = toolCall.Id,
               type = "function",
               function = new
               {
                  name = toolCall.Name,
                  arguments = toolCall.Arguments
               }
            }).ToArray(),
            JsonOptions
         );
      }

      return assistantMessage;
   }

   private static JsonObject CreateToolMessage(string toolCallId, string result)
   {
      return new JsonObject
      {
         ["role"] = "tool",
         ["tool_call_id"] = toolCallId,
         ["content"] = result
      };
   }

   private static bool TryGetAssistantToolCalls(
      JsonObject response,
      out IReadOnlyList<ToolCall> toolCalls
   )
   {
      toolCalls = [];

      if(
         !response.TryGetPropertyValue("choices", out var choicesNode) ||
         choicesNode is not JsonArray choices ||
         choices.Count == 0 ||
         choices[0] is not JsonObject choice ||
         !choice.TryGetPropertyValue("message", out var messageNode) ||
         messageNode is not JsonObject message ||
         !message.TryGetPropertyValue("tool_calls", out var toolCallsNode) ||
         toolCallsNode is not JsonArray toolCallsArray ||
         toolCallsArray.Count == 0
      )
      {
         return false;
      }

      var parsedToolCalls = new List<ToolCall>();

      foreach(var toolCallNode in toolCallsArray)
      {
         if(toolCallNode is not JsonObject toolCallObject)
         {
            continue;
         }

         var id = toolCallObject["id"]?.GetValue<string>() ?? "";
         var function = toolCallObject["function"] as JsonObject;
         var name = function?["name"]?.GetValue<string>() ?? "";
         var arguments = function?["arguments"] is JsonValue value &&
            value.TryGetValue<string>(out var stringArguments)
            ? stringArguments
            : function?["arguments"]?.ToJsonString() ?? "";

         if(string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
         {
            continue;
         }

         parsedToolCalls.Add(new ToolCall(id, name, arguments));
      }

      toolCalls = parsedToolCalls;
      return toolCalls.Count > 0;
   }

   private static string ExtractFinalText(JsonObject response)
   {
      if(
         !response.TryGetPropertyValue("choices", out var choicesNode) ||
         choicesNode is not JsonArray choices ||
         choices.Count == 0 ||
         choices[0] is not JsonObject choice ||
         !choice.TryGetPropertyValue("message", out var messageNode) ||
         messageNode is not JsonObject message
      )
      {
         return response.ToJsonString(JsonOptions);
      }

      return NormalizeOutput(ExtractMessageContent(message));
   }

   private static string ExtractMessageContent(JsonObject message)
   {
      if(!message.TryGetPropertyValue("content", out var contentNode))
      {
         return "";
      }

      if(contentNode is JsonValue contentValue &&
         contentValue.TryGetValue<string>(out var contentText))
      {
         return contentText;
      }

      if(contentNode is not JsonArray contentArray)
      {
         return contentNode?.ToJsonString() ?? "";
      }

      var builder = new System.Text.StringBuilder();

      foreach(var item in contentArray)
      {
         if(item is JsonObject contentItem &&
            contentItem.TryGetPropertyValue("text", out var textNode) &&
            textNode is JsonValue textValue &&
            textValue.TryGetValue<string>(out var itemText))
         {
            builder.Append(itemText);
         }
      }

      return builder.ToString();
   }

   private static string NormalizeOutput(string value)
   {
      return value
         .Trim()
         .Trim('"', '\'')
         .ReplaceLineEndings(" ");
   }

   private static string ExtractQuery(string arguments)
   {
      if(string.IsNullOrWhiteSpace(arguments))
      {
         return "";
      }

      try
      {
         using var document = JsonDocument.Parse(arguments);
         var root = document.RootElement;

         if(
            TryGetStringProperty(root, "query", out var query) &&
            !string.IsNullOrWhiteSpace(query)
         )
         {
            return query;
         }
      }
      catch(JsonException)
      {
      }

      return arguments.Trim();
   }

   private static int ExtractLimit(string arguments)
   {
      if(string.IsNullOrWhiteSpace(arguments))
      {
         return 10;
      }

      try
      {
         using var document = JsonDocument.Parse(arguments);
         var root = document.RootElement;

         if(
            root.TryGetProperty("limit", out var maxResultsNode) &&
            maxResultsNode.ValueKind == JsonValueKind.Number &&
            maxResultsNode.TryGetInt32(out var maxResults)
         )
         {
            return Math.Clamp(maxResults, 1, 10);
         }
      }
      catch(JsonException)
      {
      }

      return 10;
   }

   private static string ExtractUrl(string arguments)
   {
      if(string.IsNullOrWhiteSpace(arguments))
      {
         return "";
      }

      try
      {
         using var document = JsonDocument.Parse(arguments);
         var root = document.RootElement;

         if(
            TryGetStringProperty(root, "url", out var url) &&
            !string.IsNullOrWhiteSpace(url)
         )
         {
            return url;
         }
      }
      catch(JsonException)
      {
      }

      return "";
   }

   private static string ExtractFind(string arguments)
   {
      if(string.IsNullOrWhiteSpace(arguments))
      {
         return "";
      }

      try
      {
         using var document = JsonDocument.Parse(arguments);
         var root = document.RootElement;

         if(
            TryGetStringProperty(root, "find", out var find) &&
            !string.IsNullOrWhiteSpace(find)
         )
         {
            return find;
         }
      }
      catch(JsonException)
      {
      }

      return "";
   }

   private static bool TryGetStringProperty(
      JsonElement element,
      string propertyName,
      out string value
   )
   {
      value = "";

      if(
         !element.TryGetProperty(propertyName, out var property) ||
         property.ValueKind != JsonValueKind.String
      )
      {
         return false;
      }

      value = property.GetString() ?? "";
      return !string.IsNullOrWhiteSpace(value);
   }

   private static string FormatSearchResults(
      IReadOnlyList<WebSearchResult> searchResults
   )
   {
      if(searchResults.Count == 0)
      {
         return "[]";
      }

      var output = searchResults
         .Select(searchResult =>
         {
            return new
            {
               title = searchResult.Title,
               url = searchResult.Url,
               snippet = searchResult.Snippet,
               published_at = searchResult.PublishedAt?.ToString("O")
            };
         })
         .ToArray();

      return JsonSerializer.Serialize(output, JsonOptions);
   }

   private async Task<string> FormatPageContentAsync(
      string url,
      ToolLoopState toolState,
      int turn,
      CancellationToken cancellationToken
   )
   {
      if(!TryValidatePageUrl(url, out var normalizedUrl, out var error))
      {
         return JsonSerializer.Serialize(
            new
            {
               error,
               url
            },
            JsonOptions
         );
      }

      var pageTarget = new PageTarget(
         "Page URL",
         normalizedUrl,
         normalizedUrl,
         normalizedUrl,
         null
      );

      var signature = BuildPageCallSignature(
         WebToolNames.GetPage,
         pageTarget.Url,
         ""
      );

      if(TryGetRepeatedResult(
         signature,
         toolState.PageCallHistory,
         out var repeatedResult
      ))
      {
         toolState.LastPageFetcher = TryGetCachedPageFetcher(
            toolState,
            pageTarget.Url
         );
         return repeatedResult;
      }

      var pageContent = await GetPageContentAsync(
         pageTarget.Url,
         toolState,
         cancellationToken
      );
      toolState.LastPageFetcher = pageContent?.Fetcher;

      string result;

      if(pageContent is null)
      {
         result = FormatFetchErrorText(pageTarget, null, null);
      }
      else
      {
         result = LlamaPageToolFormatter.FormatPageContentText(
            pageTarget.ReferenceLabel,
            pageTarget.ReferenceValue,
            pageContent.Title,
            pageContent.Url,
            pageTarget.SearchSnippet,
            pageContent.PublishedAt,
            pageContent.Headings,
            pageContent.RelevantLinks,
            $"Detected rows for {PrimaryCountry.DisplayName}",
            LlamaPageToolFormatter.ExtractMatchingRows(
               pageContent.MainTextFull,
               [
                  PrimaryCountry.DisplayName,
                  PrimaryCountry.LocalDisplayName,
                  PrimaryCountry.ThreeLetterCode
               ]
            ),
            pageContent.MainText,
            pageContent.FetchErrorMessage,
            pageContent.FetchErrorKind
         );
      }

      RecordResult(
         signature,
         toolState.PageCallHistory,
         turn,
         result
      );

      return result;
   }

   private async Task<string> FormatPageFindResultsAsync(
      string url,
      string find,
      ToolLoopState toolState,
      int turn,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(find))
      {
         return JsonSerializer.Serialize(
            new
            {
               error = "Missing search term."
            },
            JsonOptions
         );
      }

      if(!TryValidatePageUrl(url, out var normalizedUrl, out var error))
      {
         return JsonSerializer.Serialize(
            new
            {
               error,
               url,
               find
            },
            JsonOptions
         );
      }

      var pageTarget = new PageTarget(
         "Page URL",
         normalizedUrl,
         normalizedUrl,
         normalizedUrl,
         null
      );

      var signature = BuildPageCallSignature(
         WebToolNames.FindInPage,
         pageTarget.Url,
         find
      );

      if(TryGetRepeatedResult(
         signature,
         toolState.PageCallHistory,
         out var repeatedResult
      ))
      {
         toolState.LastPageFetcher = TryGetCachedPageFetcher(
            toolState,
            pageTarget.Url
         );
         return repeatedResult;
      }

      var pageContent = await GetPageContentAsync(
         pageTarget.Url,
         toolState,
         cancellationToken
      );
      toolState.LastPageFetcher = pageContent?.Fetcher;

      string result;

      if(pageContent is null)
      {
         result = FormatFetchErrorText(pageTarget, null, null);
      }
      else if(!string.IsNullOrWhiteSpace(pageContent.FetchErrorMessage))
      {
         result = FormatFetchErrorText(
            pageTarget,
            pageContent.FetchErrorMessage,
            pageContent.FetchErrorKind
         );
      }
      else
      {
         var matches = LlamaPageToolFormatter.FindPageMatches(
            pageContent,
            find
         );
         var allRows = LlamaPageToolFormatter.ExtractMatchingRows(
            pageContent.MainTextFull,
            find,
            int.MaxValue
         );
         var returnedRows = allRows.Take(50).ToList();
         var hasRows = returnedRows.Count > 0;

         result = JsonSerializer.Serialize(
            new
            {
               reference_label = pageTarget.ReferenceLabel,
               reference_value = pageTarget.ReferenceValue,
               find,
               title = pageContent.Title,
               url = pageContent.Url,
               published_at = pageContent.PublishedAt?.ToString("O"),
               match_count = hasRows ? allRows.Count : matches.Count,
               returned_count = hasRows ? returnedRows.Count : matches.Count,
               rows = hasRows ? returnedRows : null,
               matches
            },
            JsonOptions
         );
      }

      RecordResult(
         signature,
         toolState.PageCallHistory,
         turn,
         result
      );

      return result;
   }

   private static bool TryValidatePageUrl(
      string url,
      out string normalizedUrl,
      out string error
   )
   {
      normalizedUrl = "";
      error = "";

      if(string.IsNullOrWhiteSpace(url))
      {
         error = "Missing page URL.";
         return false;
      }

      if(url.Length > 2048)
      {
         error = "Page URL is too long.";
         return false;
      }

      if(!Uri.TryCreate(url, UriKind.Absolute, out var absoluteUrl))
      {
         error = "Invalid page URL.";
         return false;
      }

      if(!string.Equals(
         absoluteUrl.Scheme,
         Uri.UriSchemeHttp,
         StringComparison.OrdinalIgnoreCase
      ) &&
         !string.Equals(
            absoluteUrl.Scheme,
            Uri.UriSchemeHttps,
            StringComparison.OrdinalIgnoreCase
         ))
      {
         error = "Page URL must use http or https.";
         return false;
      }

      if(string.IsNullOrWhiteSpace(absoluteUrl.Host))
      {
         error = "Page URL is missing a host.";
         return false;
      }

      if(IsBlockedHost(absoluteUrl.Host))
      {
         error = "Page URL host is not allowed.";
         return false;
      }

      normalizedUrl = absoluteUrl.ToString();
      return true;
   }

   private async Task<WebPageContent?> GetPageContentAsync(
      string url,
      ToolLoopState toolState,
      CancellationToken cancellationToken
   )
   {
      if(toolState.PageContentCache.TryGetValue(url, out var cachedContent))
      {
         return cachedContent;
      }

      var pageContent = await WebPageContentClient.FetchAsync(
         url,
         cancellationToken
      );
      toolState.PageContentCache[url] = pageContent;
      return pageContent;
   }

   private static string? TryGetCachedPageFetcher(
      ToolLoopState toolState,
      string url
   )
   {
      return toolState.PageContentCache.TryGetValue(url, out var cachedPage)
         ? cachedPage?.Fetcher
         : null;
   }

   private static bool IsBlockedHost(string host)
   {
      return string.Equals(
         host,
         "localhost",
         StringComparison.OrdinalIgnoreCase
      ) || string.Equals(
         host,
         "127.0.0.1",
         StringComparison.OrdinalIgnoreCase
      ) || string.Equals(
         host,
         "::1",
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static string FormatFetchErrorText(
      PageTarget pageTarget,
      string? fetchErrorMessage,
      WebPageFetchErrorKind? fetchErrorKind
   )
   {
      var message = string.IsNullOrWhiteSpace(fetchErrorMessage)
         ? $"Unable to fetch page content from {pageTarget.Url}."
         : fetchErrorMessage.Trim();

      return LlamaPageToolFormatter.FormatPageContentText(
         pageTarget.ReferenceLabel,
         pageTarget.ReferenceValue,
         pageTarget.Title,
         pageTarget.Url,
         pageTarget.SearchSnippet,
         null,
         null,
         null,
         null,
         null,
         null,
         message,
         fetchErrorKind
      );
   }

   internal static string SummarizeToolResult(
      string toolName,
      string toolContent
   )
   {
      return LlamaConversationTrimmer.SummarizeToolResult(
         toolName,
         toolContent
      );
   }

   private static IReadOnlyList<ToolCall> ParseMessageToolCalls(
      JsonObject message
   )
   {
      if(!message.TryGetPropertyValue("tool_calls", out var toolCallsNode) ||
         toolCallsNode is not JsonArray toolCallsArray ||
         toolCallsArray.Count == 0)
      {
         return [];
      }

      var toolCalls = new List<ToolCall>();

      foreach(var toolCallNode in toolCallsArray)
      {
         if(toolCallNode is not JsonObject toolCallObject)
         {
            continue;
         }

         var id = toolCallObject["id"]?.GetValue<string>() ?? "";
         var function = toolCallObject["function"] as JsonObject;
         var name = function?["name"]?.GetValue<string>() ?? "";
         var arguments = function?["arguments"] is JsonValue value &&
            value.TryGetValue<string>(out var stringArguments)
            ? stringArguments
            : function?["arguments"]?.ToJsonString() ?? "";

         if(string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
         {
            continue;
         }

         toolCalls.Add(new ToolCall(id, name, arguments));
      }

      return toolCalls;
   }

   private static string CreateFailureMessage(
      System.Net.HttpStatusCode statusCode,
      string rawResponse
   )
   {
      var preview = rawResponse
         .ReplaceLineEndings(" ")
         .Trim();

      if(preview.Length > 240)
      {
         preview = preview[..240] + "...";
      }

      return
         $"llama-server failed with {(int)statusCode} {statusCode}: " +
         preview;
   }

   private static bool TryGetRepeatedToolResult(
      ToolCall toolCall,
      ToolLoopState toolState,
      out string repeatedResult
   )
   {
      repeatedResult = "";
      var signature = BuildToolCallSignature(toolCall);

      if(string.IsNullOrWhiteSpace(signature) ||
         !toolState.ToolCallHistory.TryGetValue(signature, out var record))
      {
         return false;
      }

      repeatedResult = CreateRepeatedToolReplayMessage(
         toolCall.Name,
         record.Result
      );
      return true;
   }

   internal static string CreateRepeatedToolResultMessage(string toolName)
   {
      return $"Repeated {toolName} call detected. No new information.";
   }

   internal static string CreateRepeatedToolReplayMessage(
      string toolName,
      string cachedResult
   )
   {
      var result = new StringBuilder();

      result.AppendLine(CreateRepeatedToolResultMessage(toolName));

      if(!string.IsNullOrWhiteSpace(cachedResult))
      {
         result.AppendLine();
         result.Append(cachedResult);
      }

      return result.ToString().TrimEnd();
   }

   private static void RecordToolCallResult(
      ToolCall toolCall,
      ToolLoopState toolState,
      int turn,
      string result
   )
   {
      var signature = BuildToolCallSignature(toolCall);

      if(string.IsNullOrWhiteSpace(signature))
      {
         return;
      }

      toolState.ToolCallHistory[signature] = new ToolCallRecord(
         turn,
         result
      );
   }

   private static string BuildPageCallSignature(
      string toolName,
      string url,
      string find
   )
   {
      return $"{toolName}|url={url}|find={find}";
   }

   private static bool TryGetRepeatedResult(
      string signature,
      IDictionary<string, ToolCallRecord> history,
      out string repeatedResult
   )
   {
      repeatedResult = "";

      if(string.IsNullOrWhiteSpace(signature) ||
         !history.TryGetValue(signature, out var record))
      {
         return false;
      }

      repeatedResult = record.Result;
      return true;
   }

   private static void RecordResult(
      string signature,
      IDictionary<string, ToolCallRecord> history,
      int turn,
      string result
   )
   {
      if(string.IsNullOrWhiteSpace(signature))
      {
         return;
      }

      history[signature] = new ToolCallRecord(turn, result);
   }

   private static string BuildToolCallSignature(
      ToolCall toolCall
   )
   {
      var query = ExtractQuery(toolCall.Arguments);
      var limit = ExtractLimit(toolCall.Arguments);
      var url = ExtractUrl(toolCall.Arguments);
      var find = ExtractFind(toolCall.Arguments);

      return toolCall.Name switch
      {
         WebToolNames.Search =>
            $"{toolCall.Name}|query={query}|limit={limit}",
         WebToolNames.GetPage => BuildPageToolCallSignature(
            toolCall.Name,
            url,
            ""
         ),
         WebToolNames.FindInPage => BuildPageToolCallSignature(
            toolCall.Name,
            url,
            find
         ),
         _ => $"{toolCall.Name}|arguments={toolCall.Arguments.Trim()}"
      };
   }

   private static string BuildPageToolCallSignature(
      string toolName,
      string url,
      string find
   )
   {
      return $"{toolName}|url={url}|find={find}";
   }

   private sealed record ResponseEnvelope(
      JsonObject ResponseJson,
      string RawResponseJson
   );

   private static void MergeRequestOptions(
      JsonObject payload,
      string requestOptionsJson
   )
   {
      if(string.IsNullOrWhiteSpace(requestOptionsJson))
      {
         return;
      }

      try
      {
         var requestOptions = JsonNode.Parse(requestOptionsJson) as JsonObject;

         if(requestOptions is null)
         {
            return;
         }

         foreach(var property in requestOptions)
         {
            if(payload.ContainsKey(property.Key))
            {
               continue;
            }

            payload[property.Key] = property.Value?.DeepClone();
         }
      }
      catch(JsonException)
      {
      }
   }

   private sealed record ToolCall(
      string Id,
      string Name,
      string Arguments
   );

   private sealed record PageTarget(
      string ReferenceLabel,
      string ReferenceValue,
      string Url,
      string Title,
      string? SearchSnippet
   );

   private sealed class ToolLoopState
   {
      public string? LastSearchProvider { get; set; }

      public string? LastSearchProviderDetails { get; set; }

      public string? LastSearchEngine { get; set; }

      public string? LastPageFetcher { get; set; }

      public Dictionary<string, WebPageContent?> PageContentCache { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      public Dictionary<string, ToolCallRecord> ToolCallHistory { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      public int SearchCallCount { get; set; }

      public Dictionary<string, int> SearchAttemptCounts { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      public Dictionary<string, ToolCallRecord> PageCallHistory { get; } =
         new(StringComparer.OrdinalIgnoreCase);
   }

   private sealed record ToolCallRecord(
      int Turn,
      string Result
   );

}
