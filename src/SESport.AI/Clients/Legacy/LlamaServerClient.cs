using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using SESport.AI.Llama;
using SESport.AI.Protocols;
using SESport.AI.WebPages;
using SESport.AI.WebSearch;
using SESport.Core.AI;

namespace SESport.AI.Clients;

[Obsolete(
   "Legacy provider retained for compatibility; use the external harness."
)]
public sealed class LlamaServerClient : IAiProviderClient
{
   // Rough character budget for the in-memory chat history.
   // Keep this comfortably below the llama-server token limit.
   private const int MaxConversationContextCharacters =
      LlamaServerDefaults.MaxConversationContextCharacters;
   private const int MaxTransientRetryAttempts =
      LlamaServerDefaults.MaxTransientRetryAttempts;
   private const int MaxFormatRepairAttempts =
      LlamaServerDefaults.MaxFormatRepairAttempts;
   private const int MaxFinalReportCorrectionAttempts =
      LlamaServerDefaults.MaxFinalReportCorrectionAttempts;
   private const int MaxToolFormatFallbackAttempts =
      LlamaServerDefaults.MaxToolFormatFallbackAttempts;
   private const int DefaultMaxToolRounds =
      LlamaServerDefaults.DefaultMaxToolRounds;
   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   )
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   public IReadOnlyCollection<string> Kinds =>
      [AiProviderKinds.LlamaServer];

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
      return CreateRequestState(
         provider,
         job,
         prompt,
         renderedPrompt
      ).Request;
   }

   private LlamaRequestState CreateRequestState(
      AiProviderDefinition provider,
      AiJobDefinition job,
      AiPromptDefinition prompt,
      AiRenderedPrompt renderedPrompt
   )
   {
      return LlamaRequestFactory.CreateInitial(
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
      var requestState = CreateRequestState(
         provider,
         job,
         prompt,
         renderedPrompt
      );
      var request = requestState.Request;
      var conditionalTools = requestState.ConditionalTools;
      var submissionToolNames = conditionalTools
         .Where(tool => string.Equals(
            tool.Behavior,
            LlamaReportSubmission.ToolName,
            StringComparison.Ordinal
         ))
         .Select(tool => tool.Name)
         .ToHashSet(StringComparer.Ordinal);

      if(submissionToolNames.Count == 0)
      {
         submissionToolNames.Add(LlamaReportSubmission.ToolName);
      }

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
      var finalizeWithoutTools = false;
      var repeatedToolCallStreak = 0;
      var toolFormatFallbackStreak = 0;
      var formatRepairAttempts = 0;
      var validationContinuationAttempts = 0;
      var finalReportCorrectionAttempts = 0;
      var reportSubmissionPending = false;
      var corruptedParticipantRetryUsed = false;
      var maxToolRounds = job.RequiresWebSearch
         ? prompt.MaxToolRounds ?? DefaultMaxToolRounds
         : prompt.MaxToolRounds;
      var minToolRounds = prompt.MinToolRounds ?? 0;

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
            var continueWithTools = false;
            finalizeWithoutTools = false;

            while(true)
            {
               turn++;
               LlamaTemperature.ApplyTemperature(
                  request,
                  prompt.Temperature,
                  repeatedToolCallStreak
               );
               LlamaRequestFactory.ApplyToolBudgetPrompt(
                  messages,
                  baseSystemPrompt,
                  maxToolRounds,
                  toolRoundCount
               );
               var payloadCharacterCount =
                  LlamaConversationTrimmer.EstimateRequestPayloadSize(
                     request,
                     JsonOptions
                  );
               toolTrace.Add(
                  LlamaToolTrace.CreateToolBudgetTraceEntry(
                     turn,
                     maxToolRounds,
                     toolRoundCount,
                     payloadCharacterCount,
                     LlamaTemperature.GetRequestTemperature(request),
                     conditionalTools
                  )
               );
               await LlamaToolTrace.ReportProgressAsync(
                  toolTrace,
                  toolRoundCount,
                  JsonOptions,
                  toolTraceUpdated,
                  cancellationToken
               );
               LogToolBudget(
                  turn,
                  maxToolRounds,
                  toolRoundCount
               );
               rawFinalRequestJson = AiRequestJsonSerializer.Serialize(request);
               ResponseEnvelope responseEnvelope;

               try
               {
                  responseEnvelope = await SendWithStructuredOutputRepairAsync(
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
               }
               catch(HttpRequestException exception) when(
                  RequestUsesTools(request) &&
                  (
                     LlamaStructuredOutputRepair.IsPegNativeFormatFailure(
                        exception
                     ) ||
                     LlamaStructuredOutputRepair
                        .IsToolCallArgumentsParseFailure(exception)
                  )
               )
               {
                  RemoveMalformedToolCallMessages(messages);
                  var continueAfterToolFormatFailure =
                     ShouldContinueWithToolsAfterToolFormatFailure(
                        request,
                        maxToolRounds,
                        toolRoundCount,
                        toolFormatFallbackStreak
                     );
                  toolTrace.Add(
                     LlamaToolTrace.CreateToolFormatFallbackTraceEntry(
                        turn,
                        exception.Message,
                        continueAfterToolFormatFailure
                     )
                  );
                  await LlamaToolTrace.ReportProgressAsync(
                     toolTrace,
                     toolRoundCount,
                     JsonOptions,
                     toolTraceUpdated,
                     cancellationToken
                  );

                  if(continueAfterToolFormatFailure)
                  {
                     toolFormatFallbackStreak++;
                     LlamaRequestFactory.AddToolFormatFeedbackPrompt(
                        messages,
                        exception.Message
                     );
                     request["tool_choice"] = "required";
                     LlamaConversationTrimmer.TrimMessages(
                        request,
                        messages,
                        MaxConversationContextCharacters,
                        JsonOptions
                     );
                     continue;
                  }

                  finalizeWithoutTools = true;
                  break;
               }

               responseJson = responseEnvelope.ResponseJson;
               rawResponse = responseEnvelope.RawResponseJson;

               LogResponse("turn", turn, responseJson);

               if(!LlamaResponseReader.TryGetToolCalls(
                  responseJson,
                  out var toolCalls
               ))
               {
                  toolFormatFallbackStreak = 0;
                  repeatedToolCallStreak = 0;
                  toolTrace.Add(
                     LlamaToolTrace.CreateAssistantTraceEntry(
                        turn,
                        responseJson,
                        [],
                        JsonOptions
                     )
                  );
                  break;
               }

               if(TryGetMalformedToolCallReason(
                  toolCalls,
                  out var malformedToolCallReason
               ))
               {
                  var continueAfterToolFormatFailure =
                     ShouldContinueWithToolsAfterToolFormatFailure(
                        request,
                        maxToolRounds,
                        toolRoundCount,
                        toolFormatFallbackStreak
                     );
                  toolTrace.Add(
                     LlamaToolTrace.CreateAssistantTraceEntry(
                        turn,
                        responseJson,
                        toolCalls,
                        JsonOptions,
                        validationStatus: "rejected",
                        validationError: malformedToolCallReason
                     )
                  );
                  toolTrace.Add(
                     LlamaToolTrace.CreateToolFormatFallbackTraceEntry(
                        turn,
                        malformedToolCallReason,
                        continueAfterToolFormatFailure
                     )
                  );
                  await LlamaToolTrace.ReportProgressAsync(
                     toolTrace,
                     toolRoundCount,
                     JsonOptions,
                     toolTraceUpdated,
                     cancellationToken
                  );

                  if(continueAfterToolFormatFailure)
                  {
                     toolFormatFallbackStreak++;
                     LlamaRequestFactory.AddToolFormatFeedbackPrompt(
                        messages,
                        malformedToolCallReason
                     );
                     request["tool_choice"] = "required";
                     LlamaConversationTrimmer.TrimMessages(
                        request,
                        messages,
                        MaxConversationContextCharacters,
                        JsonOptions
                     );
                     continue;
                  }

                  finalizeWithoutTools = true;
                  break;
               }

               toolFormatFallbackStreak = 0;

               if(LlamaReportSubmission.TryGetSubmission(
                  toolCalls,
                  submissionToolNames,
                  out var reportSubmission
               ))
               {
                  toolTrace.Add(
                     LlamaToolTrace.CreateToolSubmissionTraceEntry(
                        turn,
                        reportSubmission
                     )
                  );

                  if(!corruptedParticipantRetryUsed &&
                     LlamaReportSubmission
                        .TryGetCorruptedParticipantNameReason(
                           reportSubmission.Arguments,
                           out var retryReason
                        ))
                  {
                     corruptedParticipantRetryUsed = true;
                     toolTrace.Add(
                        LlamaToolTrace.CreateAssistantTraceEntry(
                           turn,
                           responseJson,
                           toolCalls,
                           JsonOptions,
                           validationStatus: "rejected",
                           validationError: retryReason
                        )
                     );
                     toolTrace.Add(
                        LlamaStructuredOutputRepair
                           .CreateRepairPromptTraceEntry(
                              turn,
                              LlamaReportSubmission
                                 .GetCorruptedParticipantNamePrompt()
                           )
                     );
                     await LlamaToolTrace.ReportProgressAsync(
                        toolTrace,
                        toolRoundCount,
                        JsonOptions,
                        toolTraceUpdated,
                        cancellationToken
                     );
                     LlamaResponseReader.AppendAssistantMessage(
                        messages,
                        responseJson,
                        JsonOptions
                     );
                     LlamaRequestFactory
                        .AddCorruptedParticipantNameRetryPrompt(messages);
                     LlamaRequestFactory.ApplyToolChoice(
                        request,
                        minToolRounds,
                        toolRoundCount
                     );
                     continueWithTools = true;
                     break;
                  }

                  reportSubmissionPending = true;
                  await LlamaToolTrace.ReportProgressAsync(
                     toolTrace,
                     toolRoundCount,
                     JsonOptions,
                     toolTraceUpdated,
                     cancellationToken
                  );
                  responseJson = LlamaReportSubmission.CreateFinalResponse(
                     responseJson,
                     reportSubmission
                  );
                  break;
               }

               toolRoundCount++;
               toolTrace.Add(
                  LlamaToolTrace.CreateAssistantTraceEntry(
                     turn,
                     responseJson,
                     toolCalls,
                     JsonOptions
                  )
               );
               await LlamaToolTrace.ReportProgressAsync(
                  toolTrace,
                  toolRoundCount,
                  JsonOptions,
                  toolTraceUpdated,
                  cancellationToken
               );
               LlamaResponseReader.AppendAssistantMessage(
                  messages,
                  responseJson,
                  JsonOptions
               );

               var repeatedToolCallDetectedThisTurn = false;
               var repeatedToolCallCountThisTurn = 0;
               foreach(var toolCall in toolCalls)
               {
                  LogToolCall(turn, toolCall);

                  if(LlamaToolCallHistory.TryGetRepeatedToolResult(
                     toolCall,
                     toolState.ToolCallHistory,
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
                     cancellationToken,
                     job.IncludeSocialMedia
                  );

                  messages.Add(
                     LlamaResponseReader.CreateToolMessage(
                        toolCall.Id,
                        toolResult
                     )
                  );

                  toolTrace.Add(
                     LlamaToolTrace.CreateToolTraceEntry(
                        turn,
                        toolCall,
                        toolResult,
                        toolState.LastSearchProvider,
                        toolState.LastSearchProviderDetails,
                        toolState.LastSearchEngine,
                        toolState.LastPageFetcher,
                        toolState.LastBrowserStrategy
                     )
                  );
                  await LlamaToolTrace.ReportProgressAsync(
                     toolTrace,
                     toolRoundCount,
                     JsonOptions,
                     toolTraceUpdated,
                     cancellationToken
                  );
               }

               repeatedToolCallStreak = repeatedToolCallDetectedThisTurn
                  ? repeatedToolCallStreak + repeatedToolCallCountThisTurn
                  : 0;

               if(repeatedToolCallCountThisTurn < toolCalls.Count)
               {
                  validationContinuationAttempts = 0;
               }

               LlamaRequestFactory.ApplyToolChoice(
                  request,
                  minToolRounds,
                  toolRoundCount
               );

               LlamaConversationTrimmer.TrimMessages(
                  request,
                  messages,
                  MaxConversationContextCharacters,
                  JsonOptions
               );

               if(maxToolRounds is not null &&
                  toolRoundCount >= maxToolRounds.Value)
               {
                  toolBudgetExhausted = true;
                  break;
               }
            }

            if(continueWithTools)
            {
               continue;
            }

            if(toolBudgetExhausted || finalizeWithoutTools)
            {
               if(toolBudgetExhausted)
               {
                  LlamaRequestFactory.ApplyToolBudgetPrompt(
                     messages,
                     baseSystemPrompt,
                     maxToolRounds,
                     maxToolRounds ?? 0
                  );
                  var payloadCharacterCount =
                     LlamaConversationTrimmer.EstimateRequestPayloadSize(
                        request,
                        JsonOptions
                     );
                  toolTrace.Add(
                     LlamaToolTrace.CreateToolBudgetTraceEntry(
                        turn + 1,
                        maxToolRounds,
                        maxToolRounds ?? 0,
                        payloadCharacterCount,
                        LlamaTemperature.GetRequestTemperature(request),
                        conditionalTools
                     )
                  );
                  await LlamaToolTrace.ReportProgressAsync(
                     toolTrace,
                     toolRoundCount,
                     JsonOptions,
                     toolTraceUpdated,
                     cancellationToken
                  );
                  LogToolBudget(
                     turn + 1,
                     maxToolRounds,
                     maxToolRounds ?? 0
                  );
               }
               else
               {
                  LlamaRequestFactory.ApplyNoMoreToolsPrompt(
                     messages,
                     baseSystemPrompt
                  );
               }

               request = LlamaRequestFactory.CreateFinal(
                  request,
                  job,
                  prompt
               );
               messages = (JsonArray)request["messages"]!;
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
            AiJobResult BuildAcceptedResult(string finalOutputText)
            {
               LogResponse("final", turn, responseJson);

               if(LlamaResponseReader.TryGetToolCalls(
                  responseJson,
                  out var finalToolCalls
               ))
               {
                  toolTrace.Add(
                     LlamaToolTrace.CreateAssistantTraceEntry(
                        turn,
                        responseJson,
                        finalToolCalls,
                        JsonOptions,
                        validationStatus: "accepted"
                     )
                  );
               }
               else
               {
                  toolTrace.Add(
                     LlamaToolTrace.CreateAssistantTraceEntry(
                        turn,
                        responseJson,
                        [],
                        JsonOptions,
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

            while(true)
            {
               if(responseJson is null)
               {
                  throw new InvalidOperationException(
                     "llama-server returned no response."
                  );
               }

               var finalOutputText = LlamaResponseReader.NormalizeOutput(
                  LlamaResponseReader.ExtractFinalText(
                     responseJson,
                     JsonOptions
                  )
               );

               if(finalReportCorrectionAttempts >=
                  MaxFinalReportCorrectionAttempts)
               {
                  throw new InvalidOperationException(
                     "Final AI output remained invalid after the maximum " +
                     "number of correction attempts."
                  );
               }

               try
               {
                  finalOutputText =
                     ResponsesOutputValidator.ValidateStructuredOutput(
                        finalOutputText,
                        job.OutputMode,
                        prompt.OutputSchemaJson,
                        LlamaResponseReader.GetFinishReason(responseJson)
                     );
                  return BuildAcceptedResult(finalOutputText);
               }
               catch(InvalidOperationException exception) when(
                  LlamaStructuredOutputRepair.IsInvalidStructuredOutputFailure(
                     exception
                  ) &&
                  job.RequiresWebSearch &&
                  (
                     !RequestUsesTools(request) ||
                     finalReportCorrectionAttempts + 1 >=
                        MaxFinalReportCorrectionAttempts
                  ) &&
                  finalReportCorrectionAttempts <
                     MaxFinalReportCorrectionAttempts
               )
               {
                  finalReportCorrectionAttempts++;
                  toolTrace.Add(
                     LlamaToolTrace.CreateAssistantTraceEntry(
                        turn,
                        responseJson,
                        [],
                        JsonOptions,
                        validationStatus: "rejected",
                        validationError: exception.Message
                     )
                  );
                  toolTrace.Add(
                     LlamaToolTrace.CreateValidationFeedbackTraceEntry(
                        turn,
                        exception.Message,
                        toolsRemain: false
                     )
                  );
                  await LlamaToolTrace.ReportProgressAsync(
                     toolTrace,
                     toolRoundCount,
                     JsonOptions,
                     toolTraceUpdated,
                     cancellationToken
                  );

                  if(finalReportCorrectionAttempts >=
                     MaxFinalReportCorrectionAttempts)
                  {
                     throw;
                  }

                  LlamaResponseReader.AppendAssistantMessage(
                     messages,
                     responseJson,
                     JsonOptions
                  );
                  LlamaRequestFactory.AddFinalReportCorrectionPrompt(
                     messages,
                     exception.Message,
                     reportSubmissionPending
                  );
                  request = LlamaRequestFactory.CreateFinal(
                     request,
                     job,
                     prompt
                  );
                  ExpandTruncatedFinalCorrectionBudget(
                     request,
                     responseJson
                  );
                  messages = (JsonArray)request["messages"]!;
                  rawFinalRequestJson = AiRequestJsonSerializer.Serialize(
                     request
                  );
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
               catch(InvalidOperationException exception) when(
                  ShouldContinueWithToolsAfterValidationFailure(
                     request,
                     maxToolRounds,
                     toolRoundCount,
                     validationContinuationAttempts,
                     exception
                  )
               )
               {
                  validationContinuationAttempts++;
                  var reportSubmissionAttempt = reportSubmissionPending;

                  if(reportSubmissionPending)
                  {
                     foreach(var toolName in submissionToolNames)
                     {
                        LlamaReportSubmission.RemoveTool(request, toolName);
                     }

                     reportSubmissionPending = false;
                  }

                  toolTrace.Add(
                     LlamaToolTrace.CreateAssistantTraceEntry(
                        turn,
                        responseJson,
                        [],
                        JsonOptions,
                        validationStatus: "rejected",
                        validationError: exception.Message
                     )
                  );
                  toolTrace.Add(
                     LlamaToolTrace.CreateValidationFeedbackTraceEntry(
                        turn,
                        exception.Message,
                        toolsRemain: true
                     )
                  );
                  await LlamaToolTrace.ReportProgressAsync(
                     toolTrace,
                     toolRoundCount,
                     JsonOptions,
                     toolTraceUpdated,
                     cancellationToken
                  );
                  LlamaResponseReader.AppendAssistantMessage(
                     messages,
                     responseJson,
                     JsonOptions
                  );
                  LlamaRequestFactory.AddValidationFeedbackPrompt(
                     messages,
                     exception.Message,
                     reportSubmissionAttempt
                  );

                  LlamaRequestFactory.ApplyToolChoice(
                     request,
                     minToolRounds,
                     toolRoundCount
                  );

                  continueWithTools = true;
                  break;
               }
               catch(InvalidOperationException exception) when(
                  (
                     !LlamaStructuredOutputRepair
                        .IsInvalidStructuredOutputFailure(exception) ||
                     !job.RequiresWebSearch
                  ) &&
                  structuredOutputRepairAttempts < MaxFormatRepairAttempts &&
                  LlamaStructuredOutputRepair.IsInvalidStructuredOutputFailure(
                     exception
                  ) &&
                  LlamaStructuredOutputRepair.CanRepair(job.OutputMode, prompt)
               )
               {
                  structuredOutputRepairAttempts++;
                  toolTrace.Add(
                     LlamaToolTrace.CreateAssistantTraceEntry(
                        turn,
                        responseJson,
                        [],
                        JsonOptions,
                        validationStatus: "rejected",
                        validationError: exception.Message
                     )
                  );
                  toolTrace.Add(
                     LlamaStructuredOutputRepair.CreateRepairPromptTraceEntry(
                        turn
                     )
                  );
                  await LlamaToolTrace.ReportProgressAsync(
                     toolTrace,
                     toolRoundCount,
                     JsonOptions,
                     toolTraceUpdated,
                     cancellationToken
                  );
                  LlamaStructuredOutputRepair.ApplyRepairPrompt(messages);
                  request = LlamaRequestFactory.CreateFinal(
                     request,
                     job,
                     prompt
                  );
                  messages = (JsonArray)request["messages"]!;
                  rawFinalRequestJson =
                     AiRequestJsonSerializer.Serialize(request);
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

            if(continueWithTools)
            {
               continue;
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
         ApplyDefaultMaxTokens(request);
         var responseEnvelope = await SendWithRetryAsync(
            provider,
            request,
            turn,
            stage,
            cancellationToken
         );

         return responseEnvelope;
      }
      catch(HttpRequestException exception) when(
         formatRepairAttempts < MaxFormatRepairAttempts &&
         LlamaStructuredOutputRepair.IsPegNativeFormatFailure(exception) &&
         LlamaStructuredOutputRepair.CanRepair(outputMode, prompt) &&
         !RequestUsesTools(request)
      )
      {
         incrementFormatRepairAttempts();
         LlamaStructuredOutputRepair.ApplyRepairPrompt(messages);
         toolTrace.Add(
            LlamaStructuredOutputRepair.CreateRepairPromptTraceEntry(turn)
         );
         var repairRequest = CreateUnconstrainedRepairRequest(request);
         return await SendWithStructuredOutputRepairAsync(
            provider,
            repairRequest,
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
   }

   private static bool RequestUsesTools(JsonObject request)
   {
      return request["tools"] is JsonArray tools && tools.Count > 0;
   }

   private static bool TryGetMalformedToolCallReason(
      IReadOnlyList<LlamaToolCall> toolCalls,
      out string reason
   )
   {
      foreach(var toolCall in toolCalls)
      {
         if(IsValidToolArguments(toolCall.Arguments))
         {
            continue;
         }

         reason =
            $"Failed to parse tool call arguments as JSON for tool "
            + $"'{toolCall.Name}'.";
         return true;
      }

      reason = "";
      return false;
   }

   private static bool IsValidToolArguments(string arguments)
   {
      if(string.IsNullOrWhiteSpace(arguments))
      {
         return true;
      }

      try
      {
         using var document = JsonDocument.Parse(arguments);
         return document.RootElement.ValueKind == JsonValueKind.Object;
      }
      catch(JsonException)
      {
         return false;
      }
   }

   private static void RemoveMalformedToolCallMessages(
      JsonArray messages
   )
   {
      for(var index = messages.Count - 1; index >= 0; index--)
      {
         if(messages[index] is not JsonObject message ||
            !IsMessageRole(message, "assistant") ||
            !ContainsMalformedToolCall(message))
         {
            continue;
         }

         messages.RemoveAt(index);
         while(index < messages.Count &&
               IsMessageRole(messages[index], "tool"))
         {
            messages.RemoveAt(index);
         }
      }
   }

   private static bool ContainsMalformedToolCall(JsonObject message)
   {
      if(message["tool_calls"] is not JsonArray toolCalls)
      {
         return false;
      }

      foreach(var toolCallNode in toolCalls)
      {
         if(toolCallNode is not JsonObject toolCall ||
            toolCall["function"] is not JsonObject function ||
            function["arguments"] is not JsonValue argumentsValue ||
            !argumentsValue.TryGetValue<string>(out var arguments))
         {
            continue;
         }

         if(!IsValidToolArguments(arguments))
         {
            return true;
         }
      }

      return false;
   }

   private static bool IsMessageRole(JsonNode? message, string role)
   {
      if(message is not JsonObject objectMessage ||
         objectMessage["role"] is not JsonValue roleValue ||
         !roleValue.TryGetValue<string>(out var messageRole))
      {
         return false;
      }

      return string.Equals(
         messageRole,
         role,
         StringComparison.Ordinal
      );
   }

   private static void ExpandTruncatedFinalCorrectionBudget(
      JsonObject request,
      JsonObject response
   )
   {
      if(!string.Equals(
         LlamaResponseReader.GetFinishReason(response),
         "length",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return;
      }

      var currentMaxTokens = request["max_tokens"]?.GetValue<int>() ?? 0;
      if(currentMaxTokens < AiDefaults.DefaultMaxOutputTokens)
      {
         request["max_tokens"] = AiDefaults.DefaultMaxOutputTokens;
      }
   }

   private static bool ShouldContinueWithToolsAfterValidationFailure(
      JsonObject request,
      int? maxToolRounds,
      int toolRoundCount,
      int validationContinuationAttempts,
      Exception exception
   )
   {
      if(!RequestUsesTools(request))
      {
         return false;
      }

      if(LlamaStructuredOutputRepair.IsEmptyOutputFailure(exception))
      {
         return false;
      }

      if(maxToolRounds is not null && toolRoundCount >= maxToolRounds.Value)
      {
         return false;
      }

      if(validationContinuationAttempts >=
         GetRemainingValidationContinuationBudget(
            maxToolRounds,
            toolRoundCount
         ))
      {
         return false;
      }

      return LlamaStructuredOutputRepair.IsInvalidStructuredOutputFailure(
         exception
      );
   }

   private static bool ShouldContinueWithToolsAfterToolFormatFailure(
      JsonObject request,
      int? maxToolRounds,
      int toolRoundCount,
      int toolFormatFallbackStreak
   )
   {
      if(!RequestUsesTools(request) ||
         toolFormatFallbackStreak >= MaxToolFormatFallbackAttempts)
      {
         return false;
      }

      return maxToolRounds is null || toolRoundCount < maxToolRounds.Value;
   }

   private static int GetRemainingValidationContinuationBudget(
      int? maxToolRounds,
      int toolRoundCount
   )
   {
      if(maxToolRounds is null)
      {
         return DefaultMaxToolRounds;
      }

      return Math.Max(maxToolRounds.Value - toolRoundCount, 0);
   }

   private static JsonObject CreateUnconstrainedRepairRequest(
      JsonObject request
   )
   {
      var repairRequest = (JsonObject)request.DeepClone();
      repairRequest.Remove("response_format");

      return repairRequest;
   }

   private static void ApplyDefaultMaxTokens(JsonObject request)
   {
      if(request.ContainsKey("max_tokens"))
      {
         return;
      }

      request["max_tokens"] = AiDefaults.DefaultMaxOutputTokens;
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
               if(LlamaRetryPolicy.IsTransientFailure(
                  response.StatusCode,
                  rawResponse
               ) &&
                  attempt < MaxTransientRetryAttempts)
               {
                  await DelayTransientRetryAsync(
                     stage,
                     turn,
                     attempt,
                     LlamaRetryPolicy.CreateFailureMessage(
                        response.StatusCode,
                        rawResponse
                     ),
                     cancellationToken
                  );
                  continue;
               }

               throw new HttpRequestException(
                  LlamaRetryPolicy.CreateFailureMessage(
                     response.StatusCode,
                     rawResponse
                  ),
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
         catch(Exception exception) when(
            LlamaRetryPolicy.IsTransientFailure(
               exception,
               rawResponse,
               cancellationToken
            ) &&
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
      var delay = LlamaRetryPolicy.GetRetryDelay(attempt);

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

      var finishReason = LlamaResponseReader.GetFinishReason(response);
      var reasoningContent =
         LlamaResponseReader.ExtractReasoningContent(response);
      var content = LlamaResponseReader.NormalizeOutput(
         LlamaResponseReader.ExtractFinalText(response, JsonOptions)
      );
      var toolCalls = LlamaResponseReader.ExtractToolCallNames(response);

      Logger.LogDebug(
         "llama-server {Stage}:{Step} finish_reason={FinishReason} " +
         "reasoning={HasReasoning} tool_calls={ToolCalls} content={Content}",
         stage,
         step,
         string.IsNullOrWhiteSpace(finishReason) ? "null" : finishReason,
         string.IsNullOrWhiteSpace(reasoningContent) ? "false" : "true",
         toolCalls.Length == 0 ? "[]" : string.Join(",", toolCalls),
         LlamaLogFormatting.Truncate(content, 800)
      );
   }

   private void LogToolCall(
      int step,
      LlamaToolCall toolCall
   )
   {
      if(!Logger.IsEnabled(LogLevel.Debug))
      {
         return;
      }

      var query = LlamaToolArguments.ExtractQuery(toolCall.Arguments);
      var limit = LlamaToolArguments.ExtractLimit(toolCall.Arguments);
      var find = LlamaToolArguments.ExtractFind(toolCall.Arguments);

      Logger.LogDebug(
         "llama-server tool:{Step} name={Name} query={Query} " +
         "limit={Limit} find={Find}",
         step,
         toolCall.Name,
         LlamaLogFormatting.Truncate(
            query,
            LlamaServerDefaults.PreviewSnippetCharacters
         ),
         limit,
         LlamaLogFormatting.Truncate(find, 120)
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
         LlamaLogFormatting.Truncate(
            query,
            LlamaServerDefaults.PreviewSnippetCharacters
         ),
         limit,
         searchResults.Count,
         LlamaLogFormatting.Truncate(
            firstResult,
            LlamaServerDefaults.PreviewSnippetCharacters
         )
      );
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
         LlamaLogFormatting.Truncate(prompt, 120)
      );
   }

   private async Task<string> ExecuteToolCallAsync(
      LlamaToolCall toolCall,
      ToolLoopState toolState,
      int turn,
      CancellationToken cancellationToken,
      bool includeSocialMedia
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
            cancellationToken,
            includeSocialMedia
         );
      }

      if(LlamaToolCallHistory.TryGetRepeatedToolResult(
         toolCall,
         toolState.ToolCallHistory,
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
         var url = LlamaToolArguments.ExtractUrl(toolCall.Arguments);

         var pageResult = await FormatPageContentAsync(
            url,
            toolState,
            turn,
            cancellationToken
         );

         LlamaToolCallHistory.RecordToolCallResult(
            toolCall,
            toolState.ToolCallHistory,
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
         var url = LlamaToolArguments.ExtractUrl(toolCall.Arguments);
         var find = LlamaToolArguments.ExtractFind(toolCall.Arguments);

         var result = await FormatPageFindResultsAsync(
            url,
            find,
            toolState,
            turn,
            cancellationToken
         );

         LlamaToolCallHistory.RecordToolCallResult(
            toolCall,
            toolState.ToolCallHistory,
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
      LlamaToolCall toolCall,
      ToolLoopState toolState,
      int turn,
      CancellationToken cancellationToken,
      bool includeSocialMedia
   )
   {
      var query = LlamaToolArguments.ExtractQuery(toolCall.Arguments);
      var limit = LlamaToolArguments.ExtractLimit(toolCall.Arguments);
      var signature = LlamaToolCallHistory.BuildToolCallSignature(toolCall);
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
         return LlamaToolCallHistory.CreateRepeatedToolReplayMessage(
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
         searchAttempt,
         includeSocialMedia
      );
      var searchResults = searchResponse.Results;
      toolState.LastSearchProvider = searchResponse.Provider;
      toolState.LastSearchProviderDetails = searchResponse.Details;
      toolState.LastSearchEngine =
         LlamaSearchResultFormatter.GetSearchEngine(searchResponse.Details);
      toolState.SearchCallCount = searchAttempt + 1;
      toolState.SearchAttemptCounts[signature] = repeatedSearchCount + 1;

      LogSearchResults(
         query,
         limit,
         searchResults,
         toolState.LastSearchProvider
      );

      var result = LlamaSearchResultFormatter.FormatSearchResults(
         searchResults,
         JsonOptions
      );

      LlamaToolCallHistory.RecordToolCallResult(
         toolCall,
         toolState.ToolCallHistory,
         turn,
         result
      );
      return result;
   }

   private async Task<string> FormatPageContentAsync(
      string url,
      ToolLoopState toolState,
      int turn,
      CancellationToken cancellationToken
   )
   {
      if(!LlamaPageToolSupport.TryValidatePageUrl(
         url,
         out var normalizedUrl,
         out var error
      ))
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

      var pageTarget = new LlamaPageTarget(
         "Page URL",
         normalizedUrl,
         normalizedUrl,
         normalizedUrl,
         null
      );

      var signature = LlamaToolCallHistory.BuildPageCallSignature(
         WebToolNames.GetPage,
         pageTarget.Url,
         ""
      );

      // A repeated call returns the recorded result only when clean
      // successful content is cached. Failed attempts stay retryable.
      if(LlamaToolCallHistory.TryGetRepeatedResult(
         signature,
         toolState.PageCallHistory,
         out var repeatedResult
      ) && HasCleanCachedContent(toolState, pageTarget.Url))
      {
         toolState.LastPageFetcher =
            LlamaPageToolSupport.TryGetCachedPageFetcher(
               toolState.PageContentCache,
               pageTarget.Url
            );
         toolState.LastBrowserStrategy =
            LlamaPageToolSupport.TryGetCachedPageBrowserStrategy(
               toolState.PageContentCache,
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
      toolState.LastBrowserStrategy = pageContent?.BrowserStrategy;

      string result;

      if(pageContent is null)
      {
         result = LlamaPageToolSupport.FormatFetchErrorText(
            pageTarget,
            null,
            null
         );
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
            $"Detected rows for {PrimaryCountry.CountryName}",
            LlamaPageToolFormatter.ExtractMatchingRows(
               pageContent.MainTextFull,
               [
                  PrimaryCountry.CountryName,
                  PrimaryCountry.LocalDisplayName,
                  PrimaryCountry.ThreeLetterCode
               ]
            ),
            pageContent.MainText,
            pageContent.FetchErrorMessage,
            pageContent.FetchErrorKind,
            pageContent.RenderWarning
         );
      }

      LlamaToolCallHistory.RecordResult(
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

      if(!LlamaPageToolSupport.TryValidatePageUrl(
         url,
         out var normalizedUrl,
         out var error
      ))
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

      var pageTarget = new LlamaPageTarget(
         "Page URL",
         normalizedUrl,
         normalizedUrl,
         normalizedUrl,
         null
      );

      var signature = LlamaToolCallHistory.BuildPageCallSignature(
         WebToolNames.FindInPage,
         pageTarget.Url,
        find
      );

      // A repeated call returns the recorded result only when clean
      // successful content is cached. Failed attempts stay retryable.
      if(LlamaToolCallHistory.TryGetRepeatedResult(
         signature,
         toolState.PageCallHistory,
         out var repeatedResult
      ) && HasCleanCachedContent(toolState, pageTarget.Url))
      {
         toolState.LastPageFetcher =
            LlamaPageToolSupport.TryGetCachedPageFetcher(
               toolState.PageContentCache,
               pageTarget.Url
            );
         toolState.LastBrowserStrategy =
            LlamaPageToolSupport.TryGetCachedPageBrowserStrategy(
               toolState.PageContentCache,
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
      toolState.LastBrowserStrategy = pageContent?.BrowserStrategy;

      string result;

      if(pageContent is null)
      {
         result = LlamaPageToolSupport.FormatFetchErrorText(
            pageTarget,
            null,
            null
         );
      }
      else if(!string.IsNullOrWhiteSpace(pageContent.FetchErrorMessage))
      {
         result = LlamaPageToolSupport.FormatFetchErrorText(
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
         var matchingCountryEntries =
            LlamaPageToolFormatter.ExtractMatchingCountryEntries(
               string.IsNullOrWhiteSpace(pageContent.MainTextFull)
                  ? pageContent.MainText
                  : pageContent.MainTextFull,
               find
            );

         result = matchingCountryEntries.Count > 0
            ? string.Join(
               Environment.NewLine,
               matchingCountryEntries
            )
            : LlamaPageToolFormatter.FormatFindMatchesForTool(
               matches
            );
      }

      LlamaToolCallHistory.RecordResult(
         signature,
         toolState.PageCallHistory,
         turn,
         result
      );

      return result;
   }

   private async Task<WebPageContent?> GetPageContentAsync(
      string url,
      ToolLoopState toolState,
      CancellationToken cancellationToken
   )
   {
      // Only clean successful content is cached as page content. Null,
      // timeout, blocked and error results are never cached, so a failed
      // fetch can be attempted again later in the same loop.
      if(toolState.PageContentCache.TryGetValue(url, out var cachedContent)
         && IsCachablePageContent(cachedContent))
      {
         return cachedContent;
      }

      Task<WebPageContent?>? fetchTask;
      lock(toolState.PageInFlight)
      {
         if(!toolState.PageInFlight.TryGetValue(url, out var inFlight))
         {
            // Negative throttle: once the attempt budget for this URL is
            // exhausted the last failure is replayed instead of fetching
            // again. The budget is per loop and distinct from the clean
            // content cache.
            if(toolState.PageFetchAttempts.GetValueOrDefault(url) >=
               AiDefaults.LlamaPageFetchMaxAttemptsPerUrl &&
               toolState.PageFailureContent.TryGetValue(
                  url,
                  out var recordedFailure
               ))
            {
               return recordedFailure;
            }

            toolState.PageFetchAttempts[url] =
               toolState.PageFetchAttempts.GetValueOrDefault(url) + 1;

            fetchTask = FetchAndRecordAsync(
               url,
               toolState,
               cancellationToken
            );
            toolState.PageInFlight[url] = fetchTask;
         }
         else
         {
            fetchTask = inFlight;
         }
      }

      if(fetchTask is null)
      {
         throw new InvalidOperationException(
            "No in-flight fetch task was found or started."
         );
      }

      // Concurrent duplicate fetches for one URL share the same task.
      var pageContent = await fetchTask;

      lock(toolState.PageInFlight)
      {
         toolState.PageInFlight.Remove(url);
      }

      return pageContent;
   }

   private async Task<WebPageContent?> FetchAndRecordAsync(
      string url,
      ToolLoopState toolState,
      CancellationToken cancellationToken
   )
   {
      var pageContent = await WebPageContentClient.FetchAsync(
         url,
         cancellationToken
      );

      if(IsCachablePageContent(pageContent))
      {
         toolState.PageContentCache[url] = pageContent;
      }
      else
      {
         // The last failure is kept separately so the loop can replay
         // it once the attempt budget for this URL is exhausted.
         toolState.PageFailureContent[url] = pageContent;
      }

      return pageContent;
   }

   private static bool HasCleanCachedContent(
      ToolLoopState toolState,
      string url
   )
   {
      return toolState.PageContentCache.TryGetValue(url, out var cached)
         && IsCachablePageContent(cached);
   }

   private static bool IsCachablePageContent(WebPageContent? pageContent)
   {
      return pageContent is not null &&
         pageContent.FetchErrorMessage is null &&
         pageContent.FetchErrorKind is null;
   }

   private sealed record ResponseEnvelope(
      JsonObject ResponseJson,
      string RawResponseJson
   );

   private sealed class ToolLoopState
   {
      public string? LastSearchProvider { get; set; }

      public string? LastSearchProviderDetails { get; set; }

      public string? LastSearchEngine { get; set; }

      public string? LastPageFetcher { get; set; }

      public string? LastBrowserStrategy { get; set; }

      // Clean successful page content only. Failed attempts are never
      // stored here so they stay retryable within the loop.
      public Dictionary<string, WebPageContent?> PageContentCache { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      // In-flight fetches shared by concurrent duplicate calls for the
      // same URL.
      public Dictionary<string, Task<WebPageContent?>> PageInFlight { get; }
         = new(StringComparer.OrdinalIgnoreCase);

      // Last failed fetch per URL. Distinct from the clean content
      // cache; replayed when the attempt budget for the URL is spent.
      public Dictionary<string, WebPageContent?> PageFailureContent { get; }
         = new(StringComparer.OrdinalIgnoreCase);

      // Number of fetch attempts made per URL in this loop.
      public Dictionary<string, int> PageFetchAttempts { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      public Dictionary<string, LlamaToolCallRecord> ToolCallHistory { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      public int SearchCallCount { get; set; }

      public Dictionary<string, int> SearchAttemptCounts { get; } =
         new(StringComparer.OrdinalIgnoreCase);

      public Dictionary<string, LlamaToolCallRecord> PageCallHistory { get; } =
         new(StringComparer.OrdinalIgnoreCase);
   }

}
