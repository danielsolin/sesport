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
   private const int MaxConversationContextCharacters = 12000;
   private const int MaxTransientRetryAttempts = 12;
   private const int MaxFormatRepairAttempts = 1;
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
      ILogger<LlamaServerClient> logger
   )
   {
      HttpClient = httpClient;
      WebSearchClient = webSearchClient;
      WebPageContentClient = webPageContentClient;
      Logger = logger;
   }

   private HttpClient HttpClient { get; }

   private IWebSearchClient WebSearchClient { get; }

   private IWebPageContentClient WebPageContentClient { get; }

   private ILogger<LlamaServerClient> Logger { get; }

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
            var conversationCharacterCount = EstimateConversationSize(
               messages
            );
            toolTrace.Add(
               CreateToolBudgetTraceEntry(
                  turn,
                  prompt.MaxToolRounds,
                  toolRoundCount,
                  conversationCharacterCount,
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
                     toolState.LastSearchProviderDetails
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
               ? repeatedToolCallStreak + 1
               : 0;

            if(job.RequiresWebSearch)
            {
               request["tool_choice"] = "auto";
            }

            TrimConversationMessages(messages);

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
            var conversationCharacterCount = EstimateConversationSize(
               messages
            );
            toolTrace.Add(
               CreateToolBudgetTraceEntry(
                  turn + 1,
                  prompt.MaxToolRounds,
                  prompt.MaxToolRounds ?? 0,
                  conversationCharacterCount,
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

            LogResponse("final", turn, responseJson);
            if(TryGetToolCalls(responseJson, out var finalToolCalls))
            {
               toolTrace.Add(
                  CreateAssistantTraceEntry(turn, responseJson, finalToolCalls)
               );
            }
            else
            {
               toolTrace.Add(
                  CreateAssistantTraceEntry(turn, responseJson, [])
               );
            }

            await ReportToolTraceProgressAsync(
               toolTrace,
               toolRoundCount,
               toolTraceUpdated,
               cancellationToken
            );
         }

         if(responseJson is null)
         {
            throw new InvalidOperationException(
               "llama-server returned no response."
            );
         }

         var finalOutputText = NormalizeOutput(ExtractFinalText(responseJson));
         finalOutputText = ResponsesOutputValidator.ValidateStructuredOutput(
            finalOutputText,
            job.OutputMode,
            prompt.OutputSchemaJson
         );
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
            EstimateConversationSize(messages),
            null,
            null,
            null,
            null
         );
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
            EstimateConversationSize(messages)
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
         return await SendWithRetryAsync(
            provider,
            request,
            turn,
            stage,
            cancellationToken
         );
      }
      catch(HttpRequestException exception) when (
         formatRepairAttempts < MaxFormatRepairAttempts &&
         IsPegNativeFormatFailure(exception) &&
         CanRepairStructuredOutput(outputMode, prompt)
      )
      {
         incrementFormatRepairAttempts();
         ApplyStructuredOutputRepairPrompt(messages);
         return await SendWithRetryAsync(
            provider,
            request,
            turn,
            stage,
            cancellationToken
         );
      }
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

   private static void ApplyStructuredOutputRepairPrompt(JsonArray messages)
   {
      var repairPrompt = """
         The previous response was rejected because it was not valid JSON.
         Return only the raw JSON object required by the schema.
         Do not use markdown, fences, commentary, or explanations.
         """.Trim();

      var repairMessage = new JsonObject
      {
         ["role"] = "system",
         ["content"] = repairPrompt
      };

      var insertionIndex = FindPrimarySystemMessageIndex(messages);

      if(insertionIndex < 0)
      {
         messages.Insert(0, repairMessage);
         return;
      }

      messages.Insert(insertionIndex + 1, repairMessage);
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
      IReadOnlyList<ToolCall> toolCalls
   )
   {
      return new JsonObject
      {
         ["kind"] = "assistant",
         ["turn"] = turn,
         ["finish_reason"] = GetFinishReason(response),
         ["content"] = NormalizeOutput(ExtractFinalText(response)),
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
      int conversationCharacterCount,
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
         ["conversation_chars"] = conversationCharacterCount,
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
      string? searchProviderDetails = null
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

      var systemIndex = FindPrimarySystemMessageIndex(messages);

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
         WebToolNames.Search,
         StringComparison.Ordinal
      ))
      {
         var query = ExtractQuery(toolCall.Arguments);
         var limit = ExtractLimit(toolCall.Arguments);
         var searchResponse = await WebSearchClient.SearchAsync(
            query,
            limit,
            cancellationToken
         );
         var searchResults = searchResponse.Results;
         toolState.LastSearchProvider = searchResponse.Provider;
         toolState.LastSearchProviderDetails = searchResponse.Details;

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
         return repeatedResult;
      }

      var pageContent = await GetPageContentAsync(
         pageTarget.Url,
         toolState,
         cancellationToken
      );

      string result;

      if(pageContent is null)
      {
         var fetchErrorMessage =
            $"Unable to fetch page content from {pageTarget.Url}.";
         result = FormatPageContentText(
            pageTarget.ReferenceLabel,
            pageTarget.ReferenceValue,
            pageTarget.Title,
            pageTarget.Url,
            pageTarget.SearchSnippet,
            null,
            null,
            null,
            fetchErrorMessage
         );
      }
      else
      {
         result = FormatPageContentText(
            pageTarget.ReferenceLabel,
            pageTarget.ReferenceValue,
            pageContent.Title,
            pageContent.Url,
            pageTarget.SearchSnippet,
            pageContent.PublishedAt,
            pageContent.Headings,
            pageContent.MainText
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
         return repeatedResult;
      }

      var pageContent = await GetPageContentAsync(
         pageTarget.Url,
         toolState,
         cancellationToken
      );

      string result;

      if(pageContent is null)
      {
         var fetchErrorMessage =
            $"Unable to fetch page content from {pageTarget.Url}.";
         result = FormatPageContentText(
            pageTarget.ReferenceLabel,
            pageTarget.ReferenceValue,
            pageTarget.Title,
            pageTarget.Url,
            pageTarget.SearchSnippet,
            null,
            null,
            null,
            fetchErrorMessage
         );
      }

      else
      {
         var matches = FindPageMatches(pageContent, find);

         result = JsonSerializer.Serialize(
            new
            {
               reference_label = pageTarget.ReferenceLabel,
               reference_value = pageTarget.ReferenceValue,
               find,
               title = pageContent.Title,
               url = pageContent.Url,
               published_at = pageContent.PublishedAt?.ToString("O"),
               match_count = matches.Count,
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

   private static List<PageMatch> FindPageMatches(
      WebPageContent pageContent,
      string find
   )
   {
      var matches = new List<PageMatch>();
      var seenSnippets = new HashSet<string>(StringComparer.Ordinal);
      var searchText = GetPageSearchText(pageContent);

      foreach(var snippet in ExtractTextSnippets(searchText, find))
      {
         AddSnippetMatch(matches, seenSnippets, "text", snippet, find);
      }

      return matches;
   }

   private static string GetPageSearchText(WebPageContent pageContent)
   {
      if(!string.IsNullOrWhiteSpace(pageContent.MainTextFull))
      {
         return pageContent.MainTextFull;
      }

      return pageContent.MainText;
   }

   private static void AddSnippetMatch(
      ICollection<PageMatch> matches,
      ISet<string> seenSnippets,
      string section,
      string snippet,
      string find
   )
   {
      if(string.IsNullOrWhiteSpace(snippet) ||
         snippet.IndexOf(find, StringComparison.OrdinalIgnoreCase) < 0)
      {
         return;
      }

      var normalizedSnippet = snippet.Trim();

      if(!seenSnippets.Add(normalizedSnippet))
      {
         return;
      }

      matches.Add(new PageMatch(section, normalizedSnippet));
   }

   private static IEnumerable<string> ExtractTextSnippets(
      string text,
      string find,
      int contextLength = 120,
      int maxMatches = 20
   )
   {
      if(string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(find))
      {
         yield break;
      }

      var searchIndex = 0;
      var matches = 0;

      while(matches < maxMatches)
      {
         var index = text.IndexOf(
            find,
            searchIndex,
            StringComparison.OrdinalIgnoreCase
         );

         if(index < 0)
         {
            yield break;
         }

         var start = Math.Max(0, index - contextLength);
         var end = Math.Min(text.Length, index + find.Length + contextLength);
         var snippet = text[start..end].ReplaceLineEndings(" ").Trim();

         if(start > 0)
         {
            snippet = "..." + snippet;
         }

         if(end < text.Length)
         {
            snippet += "...";
         }

         yield return snippet;

         searchIndex = index + Math.Max(find.Length, 1);
         matches++;
      }
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

   private static string FormatPageContentText(
      string referenceLabel,
      string referenceValue,
      string title,
      string url,
      string? searchSnippet,
      DateTimeOffset? publishedAt,
      IReadOnlyList<string>? headings,
      string? mainText,
      string? fetchErrorMessage = null
   )
   {
      var builder = new StringBuilder();

      builder.AppendLine($"{referenceLabel}: {referenceValue}");
      builder.AppendLine($"Title: {title}");
      builder.AppendLine($"URL: {url}");

      if(publishedAt is not null)
      {
         builder.AppendLine($"Published: {publishedAt:O}");
      }

      if(!string.IsNullOrWhiteSpace(searchSnippet))
      {
         builder.AppendLine("Search snippet:");
         builder.AppendLine(searchSnippet.Trim());
      }

      if(headings is not null && headings.Count > 0)
      {
         builder.AppendLine("Headings:");

         foreach(var heading in headings)
         {
            builder.AppendLine($"- {heading}");
         }
      }

      if(!string.IsNullOrWhiteSpace(mainText))
      {
         builder.AppendLine("Page text:");
         builder.AppendLine(mainText.Trim());
      }
      else if(!string.IsNullOrWhiteSpace(fetchErrorMessage))
      {
         builder.AppendLine("Fetch error:");
         builder.AppendLine(fetchErrorMessage.Trim());
      }
      else if(headings is null || headings.Count == 0)
      {
         builder.AppendLine("Page text: (empty)");
      }

      return builder.ToString().Trim();
   }

   private static void TrimConversationMessages(JsonArray messages)
   {
      var historyEntries = GetConversationHistoryEntries(messages);

      while(EstimateConversationSize(messages) >
         MaxConversationContextCharacters)
      {
         var lastAssistantIndex = FindLastAssistantMessageIndex(messages);

         if(lastAssistantIndex < 0)
         {
            return;
         }

         var firstAssistantIndex = FindFirstAssistantMessageIndex(messages);

         if(firstAssistantIndex < 0 ||
            firstAssistantIndex >= lastAssistantIndex)
         {
            return;
         }

         var nextAssistantIndex = FindNextAssistantMessageIndex(
            messages,
            firstAssistantIndex + 1,
            lastAssistantIndex
         );

         var removeEndIndex = nextAssistantIndex < 0
            ? lastAssistantIndex
            : nextAssistantIndex;

         var removeCount = removeEndIndex - firstAssistantIndex;

         if(removeCount <= 0)
         {
            return;
         }

         historyEntries.AddRange(SummarizeTrimmedMessages(
            messages,
            firstAssistantIndex,
            removeCount
         ));

         for(var index = 0; index < removeCount; index++)
         {
            messages.RemoveAt(firstAssistantIndex);
         }

         UpdateConversationHistorySummary(messages, historyEntries);
      }

      UpdateConversationHistorySummary(messages, historyEntries);
   }

   private static int FindFirstAssistantMessageIndex(JsonArray messages)
   {
      for(var index = 2; index < messages.Count; index++)
      {
         if(messages[index] is JsonObject message &&
            string.Equals(
               message["role"]?.GetValue<string>(),
               "assistant",
               StringComparison.Ordinal
            ))
         {
            return index;
         }
      }

      return -1;
   }

   private static int FindLastAssistantMessageIndex(JsonArray messages)
   {
      for(var index = messages.Count - 1; index >= 2; index--)
      {
         if(messages[index] is JsonObject message &&
            string.Equals(
               message["role"]?.GetValue<string>(),
               "assistant",
               StringComparison.Ordinal
            ))
         {
            return index;
         }
      }

      return -1;
   }

   private static int FindNextAssistantMessageIndex(
      JsonArray messages,
      int startIndex,
      int stopIndexExclusive
   )
   {
      for(var index = startIndex;
         index < messages.Count && index < stopIndexExclusive;
         index++)
      {
         if(messages[index] is JsonObject message &&
            string.Equals(
               message["role"]?.GetValue<string>(),
               "assistant",
               StringComparison.Ordinal
            ))
         {
            return index;
         }
      }

      return -1;
   }

   private static int FindPrimarySystemMessageIndex(JsonArray messages)
   {
      for(var index = 0; index < messages.Count; index++)
      {
         if(messages[index] is not JsonObject message)
         {
            continue;
         }

         if(!string.Equals(
            message["role"]?.GetValue<string>(),
            "system",
            StringComparison.Ordinal
         ))
         {
            continue;
         }

         if(IsConversationHistorySummaryMessage(message))
         {
            continue;
         }

         return index;
      }

      return -1;
   }

   private static int FindConversationHistorySummaryMessageIndex(
      JsonArray messages
   )
   {
      for(var index = 0; index < messages.Count; index++)
      {
         if(messages[index] is not JsonObject message)
         {
            continue;
         }

         if(IsConversationHistorySummaryMessage(message))
         {
            return index;
         }
      }

      return -1;
   }

   private static bool IsConversationHistorySummaryMessage(JsonObject message)
   {
      if(!string.Equals(
         message["role"]?.GetValue<string>(),
         "system",
         StringComparison.Ordinal
      ))
      {
         return false;
      }

      var content = message["content"]?.GetValue<string>() ?? "";
      return content.StartsWith(
         ConversationHistorySummaryPrefix,
         StringComparison.Ordinal
      );
   }

   private static List<string> GetConversationHistoryEntries(JsonArray messages)
   {
      var summaryIndex = FindConversationHistorySummaryMessageIndex(messages);

      if(summaryIndex < 0 || messages[summaryIndex] is not JsonObject message)
      {
         return [];
      }

      var content = message["content"]?.GetValue<string>() ?? "";
      return ParseConversationHistoryEntries(content);
   }

   private static void UpdateConversationHistorySummary(
      JsonArray messages,
      IReadOnlyList<string> entries
   )
   {
      var summaryIndex = FindConversationHistorySummaryMessageIndex(messages);

      if(entries.Count == 0)
      {
         if(summaryIndex >= 0)
         {
            messages.RemoveAt(summaryIndex);
         }

         return;
      }

      var summaryMessage = new JsonObject
      {
         ["role"] = "system",
         ["content"] = BuildConversationHistorySummary(entries)
      };

      if(summaryIndex >= 0)
      {
         messages[summaryIndex] = summaryMessage;
         return;
      }

      var insertionIndex = FindPrimarySystemMessageIndex(messages);
      if(insertionIndex < 0)
      {
         messages.Insert(0, summaryMessage);
         return;
      }

      messages.Insert(insertionIndex + 1, summaryMessage);
   }

   private static string BuildConversationHistorySummary(
      IReadOnlyList<string> entries
   )
   {
      var builder = new StringBuilder();

      builder.AppendLine(ConversationHistorySummaryPrefix);

      foreach(var entry in entries)
      {
         builder.AppendLine($"- {entry}");
      }

      return builder.ToString().TrimEnd();
   }

   private static List<string> ParseConversationHistoryEntries(string content)
   {
      if(string.IsNullOrWhiteSpace(content) ||
         !content.StartsWith(
            ConversationHistorySummaryPrefix,
            StringComparison.Ordinal
         ))
      {
         return [];
      }

      var entries = new List<string>();
      var lines = content.ReplaceLineEndings("\n").Split('\n');

      foreach(var line in lines.Skip(1))
      {
         var entry = line.Trim();

         if(string.IsNullOrWhiteSpace(entry))
         {
            continue;
         }

         if(entry.StartsWith("- ", StringComparison.Ordinal))
         {
            entry = entry[2..].Trim();
         }

         if(!string.IsNullOrWhiteSpace(entry))
         {
            entries.Add(entry);
         }
      }

      return entries;
   }

   private static List<string> SummarizeTrimmedMessages(
      JsonArray messages,
      int startIndex,
      int removeCount
   )
   {
      var entries = new List<string>();
      var endIndex = startIndex + removeCount;
      IReadOnlyList<ToolCall> currentToolCalls = [];
      var currentToolIndex = 0;

      for(var index = startIndex; index < endIndex; index++)
      {
         if(messages[index] is not JsonObject message)
         {
            continue;
         }

         var role = message["role"]?.GetValue<string>() ?? "";

         if(string.Equals(role, "assistant", StringComparison.Ordinal))
         {
            currentToolCalls = ParseMessageToolCalls(message);
            currentToolIndex = 0;

            if(currentToolCalls.Count == 0)
            {
               var assistantContent = message["content"]?.GetValue<string>() ??
                  "";

               if(!string.IsNullOrWhiteSpace(assistantContent))
               {
                  entries.Add(
                     $"assistant: {TruncateForSummary(assistantContent)}"
                  );
               }
            }

            continue;
         }

         if(!string.Equals(role, "tool", StringComparison.Ordinal))
         {
            continue;
         }

         var toolName = message["name"]?.GetValue<string>() ?? "";
         var toolContent = message["content"]?.GetValue<string>() ?? "";

         if(currentToolIndex < currentToolCalls.Count)
         {
            var toolCall = currentToolCalls[currentToolIndex];
            entries.Add(
               $"{FormatConversationToolCall(toolCall)} -> " +
               $"{TruncateForSummary(toolContent)}"
            );
            currentToolIndex++;
            continue;
         }

         entries.Add(
            $"{toolName}: {TruncateForSummary(toolContent)}"
         );
      }

      return entries;
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

   private static string FormatConversationToolCall(ToolCall toolCall)
   {
      var query = ExtractQuery(toolCall.Arguments);
      var limit = ExtractLimit(toolCall.Arguments);
      var url = ExtractUrl(toolCall.Arguments);
      var find = ExtractFind(toolCall.Arguments);

      return toolCall.Name switch
      {
         WebToolNames.Search =>
            $"{toolCall.Name}(query={FormatSummaryQuoted(query)}, " +
            $"limit={limit})",
         WebToolNames.GetPage =>
            FormatConversationPageToolCall(toolCall.Name, url, ""),
         WebToolNames.FindInPage =>
            FormatConversationPageToolCall(toolCall.Name, url, find),
         _ => $"{toolCall.Name}({TruncateForSummary(toolCall.Arguments)})"
      };
   }

   private static string FormatConversationPageToolCall(
      string toolName,
      string url,
      string find
   )
   {
      var parts = new List<string>();

      if(!string.IsNullOrWhiteSpace(url))
      {
         parts.Add($"url={FormatSummaryQuoted(url)}");
      }

      if(!string.IsNullOrWhiteSpace(find))
      {
         parts.Add($"find={FormatSummaryQuoted(find)}");
      }

      return $"{toolName}({string.Join(", ", parts)})";
   }

   private static string FormatSummaryQuoted(string value)
   {
      return "'" + value.Replace("'", "\\'", StringComparison.Ordinal) + "'";
   }

   private static string TruncateForSummary(
      string value,
      int maxLength = 220
   )
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return "";
      }

      var normalized = value.ReplaceLineEndings(" ").Trim();

      if(normalized.Length <= maxLength)
      {
         return normalized;
      }

      return normalized[..maxLength] + "...";
   }

   private static int EstimateConversationSize(JsonArray messages)
   {
      return messages.ToJsonString(JsonOptions).Length;
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

      repeatedResult = record.Result;
      return true;
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

   private sealed record PageMatch(
      string Section,
      string Snippet
   );

   private const string ConversationHistorySummaryPrefix =
      "Conversation history summary:";

   private sealed class ToolLoopState
   {
      public string? LastSearchProvider { get; set; }

      public string? LastSearchProviderDetails { get; set; }

      public Dictionary<string, WebPageContent?> PageContentCache { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      public Dictionary<string, ToolCallRecord> ToolCallHistory { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      public Dictionary<string, ToolCallRecord> PageCallHistory { get; } =
         new(StringComparer.OrdinalIgnoreCase);
   }

   private sealed record ToolCallRecord(
      int Turn,
      string Result
   );

}
