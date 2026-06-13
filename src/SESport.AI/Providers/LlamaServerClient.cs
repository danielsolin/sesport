using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SESport.AI.Abstractions;
using SESport.AI.Models;

namespace SESport.AI.Providers;

public sealed class LlamaServerClient : IAiProviderClient
{
   private const int MaxToolCalls = 6;

   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   )
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   public string Kind => "llama-server";

   public LlamaServerClient(
      HttpClient httpClient,
      IWebSearchClient webSearchClient
   )
   {
      HttpClient = httpClient;
      WebSearchClient = webSearchClient;
   }

   private HttpClient HttpClient { get; }

   private IWebSearchClient WebSearchClient { get; }

   public JsonObject CreateRequestPayload(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      string renderedPrompt
   )
   {
      var payload = new JsonObject
      {
         ["model"] = provider.Model,
         ["messages"] = new JsonArray
         {
            new JsonObject
            {
               ["role"] = "user",
               ["content"] = renderedPrompt
            }
         },
         ["tools"] = new JsonArray
         {
            new JsonObject
            {
               ["type"] = "function",
               ["function"] = new JsonObject
               {
                  ["name"] = "web_search",
                  ["description"] =
                     "Search the web for current or factual information.",
                  ["parameters"] = new JsonObject
                  {
                     ["type"] = "object",
                     ["properties"] = new JsonObject
                     {
                        ["query"] = new JsonObject
                        {
                           ["type"] = "string"
                        },
                        ["max_results"] = new JsonObject
                        {
                           ["type"] = "integer",
                           ["minimum"] = 1,
                           ["maximum"] = 10
                        }
                     },
                     ["required"] = new JsonArray { "query" },
                     ["additionalProperties"] = false
                  }
               }
            }
         },
         ["tool_choice"] = "auto"
      };

      if(prompt.MaxOutputTokens is not null)
      {
         payload["max_tokens"] = prompt.MaxOutputTokens.Value;
      }

      if(prompt.Temperature is not null)
      {
         payload["temperature"] = prompt.Temperature.Value;
      }

      ResponsesRequestFormat.Apply(
         payload,
         job.OutputMode,
         prompt.OutputSchemaJson,
         $"prompt_{prompt.Id:N}"
      );

      MergeRequestOptions(payload, provider.RequestOptionsJson);
      MergeRequestOptions(payload, prompt.RequestOptionsJson);
      return payload;
   }

   public async Task<AiJobResult> GenerateAsync(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      string renderedPrompt,
      string inputPayloadJson,
      CancellationToken cancellationToken
   )
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

      var request = CreateRequestPayload(provider, job, prompt, renderedPrompt);
      var messages = (JsonArray)request["messages"]!;
      JsonObject? lastResponse = null;

      for(var toolCallIndex = 1; toolCallIndex <= MaxToolCalls; toolCallIndex++)
      {
         var response = await SendAsync(provider, request, cancellationToken);
         var rawResponse = await response.Content.ReadAsStringAsync(
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

         lastResponse = JsonDocument.Parse(rawResponse).RootElement
            .Deserialize<JsonObject>(JsonOptions);

         if(lastResponse is null)
         {
            throw new InvalidOperationException(
               "llama-server returned an empty response."
            );
         }

         if(!TryGetToolCalls(lastResponse, out var toolCalls))
         {
            var outputText = NormalizeOutput(ExtractFinalText(lastResponse));

            return new AiJobResult(
               Guid.NewGuid(),
               job.Id,
               provider.Id,
               provider.Model,
               renderedPrompt,
               AiRequestJsonSerializer.Serialize(request),
               outputText,
               rawResponse,
               null
            );
         }

         AppendAssistantMessage(messages, lastResponse);

         foreach(var toolCall in toolCalls)
         {
            var toolResponse = await ExecuteToolCallAsync(
               toolCall,
               cancellationToken
            );

            messages.Add(
               new JsonObject
               {
                  ["role"] = "tool",
                  ["tool_call_id"] = toolCall.Id,
                  ["content"] = toolResponse
               }
            );
         }

         request["messages"] = messages;
      }

      throw new InvalidOperationException(
         $"Provider '{provider.Id}' exceeded the maximum tool calls."
      );
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

   private async Task<string> ExecuteToolCallAsync(
      ToolCall toolCall,
      CancellationToken cancellationToken
   )
   {
      if(!IsWebSearchTool(toolCall.Name))
      {
         throw new InvalidOperationException(
            $"Unsupported tool call '{toolCall.Name}'."
         );
      }

      var query = ExtractQuery(toolCall.Arguments);
      var maxResults = ExtractMaxResults(toolCall.Arguments);
      var searchResults = await WebSearchClient.SearchAsync(
         query,
         maxResults,
         cancellationToken
      );

      return FormatSearchResults(query, searchResults);
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

   private static int ExtractMaxResults(string arguments)
   {
      if(string.IsNullOrWhiteSpace(arguments))
      {
         return 5;
      }

      try
      {
         using var document = JsonDocument.Parse(arguments);
         var root = document.RootElement;

         if(
            root.TryGetProperty("max_results", out var maxResultsNode) &&
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

      return 5;
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
      string query,
      IReadOnlyList<WebSearchResult> searchResults
   )
   {
      var builder = new System.Text.StringBuilder();
      builder.AppendLine($"Search query: {query}");

      if(searchResults.Count == 0)
      {
         builder.Append("No web search results were found.");
         return builder.ToString();
      }

      for(var index = 0; index < searchResults.Count; index++)
      {
         var result = searchResults[index];
         builder.AppendLine();
         builder.AppendLine($"{index + 1}. {result.Title}");
         builder.AppendLine($"URL: {result.Url}");

         if(!string.IsNullOrWhiteSpace(result.Snippet))
         {
            builder.AppendLine($"Snippet: {result.Snippet}");
         }
      }

      return builder.ToString().TrimEnd();
   }

   private static bool IsWebSearchTool(string name)
   {
      return string.Equals(name, "web_search", StringComparison.Ordinal) ||
         string.Equals(name, "altra/web-search", StringComparison.Ordinal) ||
         string.Equals(name, "altra_web_search", StringComparison.Ordinal);
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
      catch (JsonException)
      {
      }
   }

   private sealed record ToolCall(
      string Id,
      string Name,
      string Arguments
   );
}
