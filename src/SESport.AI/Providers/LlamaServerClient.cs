using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SESport.AI.Abstractions;
using SESport.AI.Models;
using SESport.AI.Validation;

namespace SESport.AI.Providers;

public sealed class LlamaServerClient : IAiProviderClient
{
   // Rough character budget for the in-memory chat history.
   // Keep this comfortably below the llama-server token limit.
   private const int MaxConversationContextCharacters = 12000;

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
      Func<string?, CancellationToken, Task>? toolTraceUpdated = null
   )
   {
      var request = CreateRequestPayload(
         provider,
         job,
         prompt,
         renderedPrompt
      );
      var requestJson = AiRequestJsonSerializer.Serialize(request);
      JsonObject? responseJson = null;
      var rawResponse = "";
      var toolTrace = new JsonArray();
      var searchResultsById = new Dictionary<string, WebSearchResult>(
         StringComparer.OrdinalIgnoreCase
      );
      var toolState = new ToolLoopState();
      var messages = (JsonArray)request["messages"]!;
      var baseSystemPrompt = renderedPrompt.SystemPrompt?.Trim();
      var toolRoundCount = 0;
      var turn = 0;
      var toolBudgetExhausted = false;

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
            ApplyToolBudgetPrompt(
               messages,
               baseSystemPrompt,
               prompt.MaxToolRounds,
               toolRoundCount
            );
            toolTrace.Add(
               CreateToolBudgetTraceEntry(
                  turn,
                  prompt.MaxToolRounds,
                  toolRoundCount
               )
            );
            await ReportToolTraceProgressAsync(
               toolTrace,
               toolTraceUpdated,
               cancellationToken
            );
            LogToolBudget(
               turn,
               prompt.MaxToolRounds,
               toolRoundCount
            );
            requestJson = AiRequestJsonSerializer.Serialize(request);

            var response = await SendAsync(
               provider,
               request,
               cancellationToken
            );
            rawResponse = await response.Content.ReadAsStringAsync(
               cancellationToken
            );

            if(!response.IsSuccessStatusCode)
            {
               throw new HttpRequestException(
                  CreateFailureMessage(response.StatusCode, rawResponse),
                  null,
                  response.StatusCode
               );
            }

            responseJson = JsonDocument.Parse(rawResponse).RootElement
               .Deserialize<JsonObject>(JsonOptions);

            if(responseJson is null)
            {
               throw new InvalidOperationException(
                  "llama-server returned an empty response."
               );
            }

            LogResponse("turn", turn, responseJson);

            if(!TryGetToolCalls(responseJson, out var toolCalls))
            {
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
               toolTraceUpdated,
               cancellationToken
            );
            AppendAssistantMessage(messages, responseJson);

            foreach(var toolCall in toolCalls)
            {
               LogToolCall(turn, toolCall);

               var toolResult = await ExecuteToolCallAsync(
                  toolCall,
                  searchResultsById,
                  toolState,
                  cancellationToken
               );

               toolTrace.Add(
                  CreateToolTraceEntry(turn, toolCall, toolResult)
               );
               await ReportToolTraceProgressAsync(
                  toolTrace,
                  toolTraceUpdated,
                  cancellationToken
               );

               messages.Add(
                  new JsonObject
                  {
                     ["role"] = "tool",
                     ["tool_call_id"] = toolCall.Id,
                     ["content"] = toolResult
                  }
               );
            }

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
            toolTrace.Add(
               CreateToolBudgetTraceEntry(
                  turn + 1,
                  prompt.MaxToolRounds,
                  prompt.MaxToolRounds ?? 0
               )
            );
            await ReportToolTraceProgressAsync(
               toolTrace,
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
            requestJson = AiRequestJsonSerializer.Serialize(request);
            turn++;

            var finalResponse = await SendAsync(
               provider,
               request,
               cancellationToken
            );
            rawResponse = await finalResponse.Content.ReadAsStringAsync(
               cancellationToken
            );

            if(!finalResponse.IsSuccessStatusCode)
            {
               throw new HttpRequestException(
                  CreateFailureMessage(
                     finalResponse.StatusCode,
                     rawResponse
                  ),
                  null,
                  finalResponse.StatusCode
               );
            }

            responseJson = JsonDocument.Parse(rawResponse).RootElement
               .Deserialize<JsonObject>(JsonOptions);

            if(responseJson is null)
            {
               throw new InvalidOperationException(
                  "llama-server returned an empty response."
               );
            }

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
            requestJson,
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
      catch(Exception exception)
      {
         var toolTraceJson = toolTrace.Count == 0
            ? null
            : JsonSerializer.Serialize(toolTrace, JsonOptions);

         throw new AiProviderExecutionException(
            exception.Message,
            exception,
            requestJson,
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
      int toolRoundCount
   )
   {
      if(maxToolRounds is null)
      {
         return new JsonObject
         {
            ["kind"] = "budget",
            ["turn"] = turn,
            ["enabled"] = false
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
         ["content"] = $"Tool calls remaining: {remainingToolCalls} of " +
            $"{maxToolRounds.Value}."
      };
   }

   private static JsonObject CreateToolTraceEntry(
      int turn,
      ToolCall toolCall,
      string toolResult
   )
   {
      var isSearchTool = string.Equals(
         toolCall.Name,
         "web_search",
         StringComparison.Ordinal
      ) || string.Equals(
         toolCall.Name,
         "altra/web-search",
         StringComparison.Ordinal
      ) || string.Equals(
         toolCall.Name,
         "altra_web_search",
         StringComparison.Ordinal
      );

      var isGetPageTool = string.Equals(
         toolCall.Name,
         "web_get_page",
         StringComparison.Ordinal
      );
      var isFindInPageTool = string.Equals(
         toolCall.Name,
         "web_find_in_page",
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
         ["id"] = isGetPageTool ? ExtractId(toolCall.Arguments) : null,
         ["url"] = isGetPageTool ? ExtractUrl(toolCall.Arguments) : null,
         ["find"] = isFindInPageTool || !string.IsNullOrWhiteSpace(find)
            ? find
            : null,
         ["result"] = toolResult
      };
   }

   private static async Task ReportToolTraceProgressAsync(
      JsonArray toolTrace,
      Func<string?, CancellationToken, Task>? toolTraceUpdated,
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

      await toolTraceUpdated(toolTraceJson, cancellationToken);
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
      var id = ExtractId(toolCall.Arguments);
      var find = ExtractFind(toolCall.Arguments);

      Logger.LogDebug(
         "llama-server tool:{Step} name={Name} query={Query} " +
         "limit={Limit} id={Id} find={Find}",
         step,
         toolCall.Name,
         TruncateForLog(query, 240),
         limit,
         TruncateForLog(id, 120),
         TruncateForLog(find, 120)
      );
   }

   private void LogSearchResults(
      string query,
      int limit,
      IReadOnlyList<WebSearchResult> searchResults
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
         "llama-server search query={Query} limit={Limit} " +
         "results={ResultCount} first_result={FirstResult}",
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

      var systemIndex = FindSystemMessageIndex(messages);

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
      IDictionary<string, WebSearchResult> searchResultsById,
      ToolLoopState toolState,
      CancellationToken cancellationToken
   )
   {
      if(
         string.Equals(toolCall.Name, "web_search", StringComparison.Ordinal) ||
         string.Equals(toolCall.Name, "altra/web-search",
            StringComparison.Ordinal) ||
         string.Equals(toolCall.Name, "altra_web_search",
            StringComparison.Ordinal)
      )
      {
         var query = ExtractQuery(toolCall.Arguments);
         var limit = ExtractLimit(toolCall.Arguments);
         var searchResults = await WebSearchClient.SearchAsync(
            query,
            limit,
            cancellationToken
         );

         LogSearchResults(query, limit, searchResults);

         toolState.SearchSequence++;
         return FormatSearchResults(
            searchResults,
            toolState.SearchSequence,
            searchResultsById
         );
      }

      if(string.Equals(toolCall.Name, "web_get_page", StringComparison.Ordinal))
      {
         var id = ExtractId(toolCall.Arguments);
         var url = ExtractUrl(toolCall.Arguments);
         var find = ExtractFind(toolCall.Arguments);

         if(!string.IsNullOrWhiteSpace(find))
         {
            return await FormatPageFindResultsAsync(
               id,
               url,
               find,
               searchResultsById,
               toolState,
               cancellationToken
            );
         }

         return await FormatPageContentAsync(
            id,
            url,
            searchResultsById,
            toolState,
            cancellationToken
         );
      }

      if(string.Equals(
         toolCall.Name,
         "web_find_in_page",
         StringComparison.Ordinal
      ))
      {
         var id = ExtractId(toolCall.Arguments);
         var url = ExtractUrl(toolCall.Arguments);
         var find = ExtractFind(toolCall.Arguments);

         return await FormatPageFindResultsAsync(
            id,
            url,
            find,
            searchResultsById,
            toolState,
            cancellationToken
         );
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
      var content = ExtractMessageContent(response);
      var assistantMessage = new JsonObject
      {
         ["role"] = "assistant",
         ["content"] = content
      };

      if(TryGetAssistantToolCalls(response, out var toolCalls))
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

      messages.Add(assistantMessage);
   }

   private static int FindSystemMessageIndex(JsonArray messages)
   {
      for(var index = 0; index < messages.Count; index++)
      {
         if(messages[index] is not JsonObject message)
         {
            continue;
         }

         if(string.Equals(
            message["role"]?.GetValue<string>(),
            "system",
            StringComparison.Ordinal
         ))
         {
            return index;
         }
      }

      return -1;
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

   private static string ExtractId(string arguments)
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
            TryGetStringProperty(root, "id", out var id) &&
            !string.IsNullOrWhiteSpace(id)
         )
         {
            return id;
         }
      }
      catch(JsonException)
      {
      }

      return arguments.Trim();
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
      IReadOnlyList<WebSearchResult> searchResults,
      int searchSequence,
      IDictionary<string, WebSearchResult> searchResultsById
   )
   {
      if(searchResults.Count == 0)
      {
         return "[]";
      }

      var output = searchResults
         .Select((searchResult, index) =>
         {
            var id = $"s{searchSequence}_{index + 1}";
            searchResultsById[id] = searchResult;

            return new
            {
               id,
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
      string id,
      string url,
      IDictionary<string, WebSearchResult> searchResultsById,
      ToolLoopState toolState,
      CancellationToken cancellationToken
   )
   {
      if(!TryResolvePageTarget(
         id,
         url,
         searchResultsById,
         out var pageTarget,
         out var error
      ))
      {
         return JsonSerializer.Serialize(
            new
            {
               error,
               id,
               url
            },
            JsonOptions
         );
      }

      var pageContent = await GetPageContentAsync(
         pageTarget.Url,
         toolState,
         cancellationToken
      );

      if(pageContent is null)
      {
         return FormatPageContentText(
            pageTarget.ReferenceLabel,
            pageTarget.ReferenceValue,
            pageTarget.Title,
            pageTarget.Url,
            pageTarget.SearchSnippet,
            null,
            null,
            "Unable to fetch page content."
         );
      }

      return FormatPageContentText(
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

   private async Task<string> FormatPageFindResultsAsync(
      string id,
      string url,
      string find,
      IDictionary<string, WebSearchResult> searchResultsById,
      ToolLoopState toolState,
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

      if(!TryResolvePageTarget(
         id,
         url,
         searchResultsById,
         out var pageTarget,
         out var error
      ))
      {
         return JsonSerializer.Serialize(
            new
            {
               error,
               id,
               url,
               find
            },
            JsonOptions
         );
      }

      var pageContent = await GetPageContentAsync(
         pageTarget.Url,
         toolState,
         cancellationToken
      );

      if(pageContent is null)
      {
         return FormatPageContentText(
            pageTarget.ReferenceLabel,
            pageTarget.ReferenceValue,
            pageTarget.Title,
            pageTarget.Url,
            pageTarget.SearchSnippet,
            null,
            null,
            "Unable to fetch page content."
         );
      }

      var matches = FindPageMatches(pageContent, find);

      return JsonSerializer.Serialize(
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

   private static bool TryResolvePageTarget(
      string id,
      string url,
      IDictionary<string, WebSearchResult> searchResultsById,
      out PageTarget pageTarget,
      out string error
   )
   {
      pageTarget = null!;
      error = "";

      if(!string.IsNullOrWhiteSpace(id) &&
         searchResultsById.TryGetValue(id, out var searchResult))
      {
         pageTarget = new PageTarget(
            "Page id",
            id,
            searchResult.Url,
            searchResult.Title,
            searchResult.Snippet
         );
         return true;
      }

      if(string.IsNullOrWhiteSpace(url))
      {
         error = "Missing search result id or URL.";
         return false;
      }

      if(!TryValidatePageUrl(url, out var normalizedUrl, out error))
      {
         return false;
      }

      pageTarget = new PageTarget(
         "Page URL",
         normalizedUrl,
         normalizedUrl,
         normalizedUrl,
         null
      );
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

      AddSnippetMatch(
         matches,
         seenSnippets,
         "title",
         pageContent.Title,
         find
      );

      foreach(var heading in pageContent.Headings)
      {
         AddSnippetMatch(matches, seenSnippets, "heading", heading, find);
      }

      foreach(var snippet in ExtractTextSnippets(pageContent.MainText, find))
      {
         AddSnippetMatch(matches, seenSnippets, "text", snippet, find);
      }

      return matches;
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
      int maxMatches = 5
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
      string? mainText
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
      else if(headings is null || headings.Count == 0)
      {
         builder.AppendLine("Page text: (empty)");
      }

      return builder.ToString().Trim();
   }

   private static void TrimConversationMessages(JsonArray messages)
   {
      while(EstimateConversationSize(messages) >
         MaxConversationContextCharacters)
      {
         var firstAssistantIndex = FindFirstAssistantMessageIndex(messages);

         if(firstAssistantIndex < 0)
         {
            return;
         }

         var nextAssistantIndex = FindNextAssistantMessageIndex(
            messages,
            firstAssistantIndex + 1
         );

         var removeCount = nextAssistantIndex < 0
            ? messages.Count - firstAssistantIndex
            : nextAssistantIndex - firstAssistantIndex;

         if(removeCount <= 0)
         {
            return;
         }

         for(var index = 0; index < removeCount; index++)
         {
            messages.RemoveAt(firstAssistantIndex);
         }
      }
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

   private static int FindNextAssistantMessageIndex(
      JsonArray messages,
      int startIndex
   )
   {
      for(var index = startIndex; index < messages.Count; index++)
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

   private sealed class ToolLoopState
   {
      public int SearchSequence { get; set; }

      public Dictionary<string, WebPageContent?> PageContentCache { get; } =
         new(StringComparer.OrdinalIgnoreCase);
   }

}
