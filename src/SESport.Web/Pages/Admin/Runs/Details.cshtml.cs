using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.AI.Jobs;
using SESport.Core.AI;
using SESport.Core.Formatting;

using System.Globalization;
using System.Text.Json;

namespace SESport.Web.Pages.Admin.Runs;

public class DetailsModel(
   AiRepository repository
) : PageModel
{
   private const string ConversationHistorySummaryPrefix =
      "Conversation history summary:";
   private const string DiagnosticPayloadPurgedMessage =
      "Removed by retention policy.";
   private const string OpenCodeReasoningEventType = "reasoning";

   public AiRunDetail? Run { get; private set; }

   public TokenUsageViewModel? TokenUsage { get; private set; }

   public string SystemPromptText { get; private set; } = string.Empty;

   public string ConversationHistorySummaryText { get; private set; } =
      string.Empty;

   public string UserPromptTemplateText { get; private set; } = string.Empty;

   public string RenderedPromptText { get; private set; } = string.Empty;

   public IReadOnlyList<SelectListItem> ExecutionEnvironmentOptions
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   private ISet<string> KnownExecutionEnvironmentValues { get; set; } =
      new HashSet<string>(StringComparer.Ordinal);

   [BindProperty(SupportsGet = true, Name = RouteKeys.JobId)]
   public string? JobId { get; set; }

   [BindProperty(SupportsGet = true, Name = RouteKeys.Status)]
   public string[]? StatusIds { get; set; } =
      AiJobRunStatusIds.DefaultRunListStatuses;

   [BindProperty(SupportsGet = true, Name = RouteKeys.Date)]
   public DateOnly? Date { get; set; }

   [BindProperty]
   public string? ExecutionEnvironment { get; set; }

   public string DateText => Date is null
      ? string.Empty
      : DateDisplay.Format(Date.Value);

   public bool CanEditExecutionEnvironment
   {
      get
      {
         return Run is not null &&
            string.Equals(
               Run.StatusId,
               AiJobRunStatusIds.Pending,
               StringComparison.Ordinal
            );
      }
   }

   public async Task<IActionResult> OnGetAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      StatusIds = NormalizeStatusIds(StatusIds);
      Run = await repository.GetRunAsync(id, cancellationToken);

      if(Run is not null)
      {
         TokenUsage = BuildTokenUsage(Run);
         ExecutionEnvironment = Run.ExecutionEnvironment;
         await LoadExecutionEnvironmentOptionsAsync(
            Run.ExecutionEnvironment,
            cancellationToken
         );
         ConversationHistorySummaryText =
            Run.DiagnosticPayloadPurgedAt is not null
               ? DiagnosticPayloadPurgedMessage
               : GetConversationHistorySummaryText(Run.RawRequestJson);
         SystemPromptText = GetRenderedSystemPromptText(Run);
         UserPromptTemplateText = Run.UserPromptTemplate;
         RenderedPromptText = Run.RenderedPrompt;
      }

      return Run is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnPostUpdateExecutionEnvironmentAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      StatusIds = NormalizeStatusIds(StatusIds);
      Run = await repository.GetRunAsync(id, cancellationToken);

      if(Run is null)
      {
         return NotFound();
      }

      TokenUsage = BuildTokenUsage(Run);

      var requestedExecutionEnvironment = string.IsNullOrWhiteSpace(
         ExecutionEnvironment
      )
         ? null
         : ExecutionEnvironment.Trim();

      await LoadExecutionEnvironmentOptionsAsync(
         requestedExecutionEnvironment,
         cancellationToken
      );

      if(!CanEditExecutionEnvironment)
      {
         ExecutionEnvironment = Run.ExecutionEnvironment;
         await LoadExecutionEnvironmentOptionsAsync(
            ExecutionEnvironment,
            cancellationToken
         );
         LoadError =
            "Execution environment can only be changed while the run is " +
            "pending.";
         return Page();
      }

      if(requestedExecutionEnvironment is not null &&
         !KnownExecutionEnvironmentValues.Contains(
            requestedExecutionEnvironment
         ))
      {
         LoadError = "Select a valid execution environment.";
         return Page();
      }

      try
      {
         await repository.UpdateRunExecutionEnvironmentAsync(
            id,
            requestedExecutionEnvironment,
            cancellationToken
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
         return Page();
      }

      return RedirectToPage(
         "./Index",
         GetFilterRouteValues()
      );
   }

   private async Task LoadExecutionEnvironmentOptionsAsync(
      string? selectedExecutionEnvironment,
      CancellationToken cancellationToken
   )
   {
      var executionEnvironments =
         await repository.GetExecutionEnvironmentOptionsAsync(
            cancellationToken
         );

      ExecutionEnvironmentOptions = BuildExecutionEnvironmentOptions(
         executionEnvironments,
         selectedExecutionEnvironment,
         SESport.Core.Configuration.ExecutionEnvironment.Current
      );
      KnownExecutionEnvironmentValues = ExecutionEnvironmentOptions
         .Select(option => option.Value ?? string.Empty)
         .ToHashSet(StringComparer.Ordinal);
   }

   internal static IReadOnlyList<SelectListItem>
      BuildExecutionEnvironmentOptions(
         IEnumerable<string> executionEnvironments,
         string? selectedExecutionEnvironment,
         string currentExecutionEnvironment,
         bool includeUnsetOption = true
      )
   {
      var options = new List<SelectListItem>();
      var normalizedSelectedExecutionEnvironment =
         string.IsNullOrWhiteSpace(selectedExecutionEnvironment)
            ? null
            : selectedExecutionEnvironment.Trim();

      if(includeUnsetOption)
      {
         options.Add(
            new SelectListItem(
               "Not set",
               string.Empty,
               normalizedSelectedExecutionEnvironment is null
            )
         );
      }

      var values = new HashSet<string>(StringComparer.Ordinal);

      void AddOption(string value)
      {
         if(string.IsNullOrWhiteSpace(value) || !values.Add(value))
         {
            return;
         }

         options.Add(
            new SelectListItem(
               FormatExecutionEnvironmentDisplayName(value),
               value,
               string.Equals(
                  value,
                  normalizedSelectedExecutionEnvironment,
                  StringComparison.Ordinal
               )
            )
         );
      }

      foreach(var executionEnvironment in executionEnvironments)
      {
         AddOption(executionEnvironment);
      }

      AddOption(currentExecutionEnvironment);
      AddOption(selectedExecutionEnvironment ?? string.Empty);

      return options;
   }

   public static string FormatJson(string? value)
   {
      return AiRunJsonFormatter.Format(value);
   }

   public static string FormatJsonOrRetentionNotice(
      string? value,
      DateTimeOffset? diagnosticPayloadPurgedAt
   )
   {
      return diagnosticPayloadPurgedAt is not null
         ? DiagnosticPayloadPurgedMessage
         : FormatJson(value);
   }

   internal static string GetRenderedSystemPromptText(AiRunDetail run)
   {
      if(!string.IsNullOrWhiteSpace(run.RenderedSystemPrompt))
      {
         return run.RenderedSystemPrompt.Trim();
      }

      try
      {
         var prompt = new AiPromptDefinition(
            run.PromptId,
            run.JobId,
            run.PromptVersion,
            run.SystemPrompt,
            run.UserPromptTemplate,
            null,
            string.Empty,
            run.PromptTemperature,
            run.PromptMaxOutputTokens,
            null,
            true
         );

         var renderedPrompt = new TemplatePromptRenderer().Render(
            prompt,
            run.InputPayloadJson
         );

         return renderedPrompt.SystemPrompt ?? string.Empty;
      }
      catch(JsonException)
      {
         return run.SystemPrompt.Trim();
      }
   }

   public static string FormatExecutionEnvironmentDisplayName(
      string? executionEnvironment
   )
   {
      if(string.IsNullOrWhiteSpace(executionEnvironment))
      {
         return "-";
      }

      var value = executionEnvironment.Trim();
      var segments = value
         .Split(
            '-',
            StringSplitOptions.RemoveEmptyEntries
               | StringSplitOptions.TrimEntries
         )
         .Where(segment => segment.Length > 0)
         .ToArray();

      if(segments.Length == 0)
      {
         return value;
      }

      var firstPart = segments[0].Length <= 3
         ? segments[0]
         : segments[0][..3];
      var lastPart = segments[^1].Length <= 3
         ? segments[^1]
         : segments[^1][^3..];

      return string.Equals(firstPart, lastPart, StringComparison.Ordinal)
         ? firstPart
         : $"{firstPart}-{lastPart}";
   }

   private static string GetConversationHistorySummaryText(
      string? rawRequestJson
   )
   {
      if(string.IsNullOrWhiteSpace(rawRequestJson))
      {
         return string.Empty;
      }

      try
      {
         using var document = JsonDocument.Parse(rawRequestJson);

         if(document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty(
               "messages",
               out var messages
            ) ||
            messages.ValueKind != JsonValueKind.Array)
         {
            return string.Empty;
         }

         foreach(var message in messages.EnumerateArray())
         {
            if(message.ValueKind != JsonValueKind.Object)
            {
               continue;
            }

            if(!string.Equals(
               GetString(message, "role"),
               "system",
               StringComparison.Ordinal
            ))
            {
               continue;
            }

            var content = GetString(message, "content") ?? string.Empty;

            if(!content.StartsWith(
               ConversationHistorySummaryPrefix,
               StringComparison.Ordinal
            ))
            {
               continue;
            }

            return content[
               ConversationHistorySummaryPrefix.Length..
            ].TrimStart();
         }
      }
      catch(JsonException)
      {
      }

      return string.Empty;
   }

   private static string? GetString(JsonElement element, string name)
   {
      if(!element.TryGetProperty(name, out var property))
      {
         return null;
      }

      return property.ValueKind == JsonValueKind.String
         ? property.GetString()
         : property.ToString();
   }

   public Dictionary<string, string> GetFilterRouteValues()
   {
      var routeValues = new Dictionary<string, string>();

      if(Date is not null)
      {
         routeValues[RouteKeys.Date] = DateDisplay.Format(Date.Value);
      }

      if(!string.IsNullOrWhiteSpace(JobId))
      {
         routeValues[RouteKeys.JobId] = JobId;
      }

      AddStatusRouteValues(routeValues, StatusIds);
      return routeValues;
   }

   private static string[] NormalizeStatusIds(
      IReadOnlyCollection<string>? statusIds
   )
   {
      var normalizedStatusIds = statusIds?
         .Where(statusId => !string.IsNullOrWhiteSpace(statusId))
         .Select(statusId => statusId.Trim())
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList()
         ?? [];

      return normalizedStatusIds.Count > 0
         ? normalizedStatusIds.ToArray()
         : AiJobRunStatusIds.DefaultRunListStatuses;
   }

   private static void AddStatusRouteValues(
      IDictionary<string, string> routeValues,
      IReadOnlyList<string>? statusIds
   )
   {
      var normalizedStatusIds = NormalizeStatusIds(statusIds);

      var index = 0;
      foreach(var statusId in normalizedStatusIds)
      {
         routeValues[$"{RouteKeys.Status}[{index}]"] = statusId;
         index++;
      }
   }

   public static string FormatDuration(decimal? durationSeconds)
   {
      return FormatDuration(
         durationSeconds,
         DateTimeOffset.MinValue,
         string.Empty
      );
   }

   public static string FormatDuration(AiRunListItem run)
   {
      return FormatDuration(
         run.DurationSeconds,
         run.StartedAt,
         run.StatusId
      );
   }

   public static string FormatDuration(AiRunDetail run)
   {
      return FormatDuration(
         run.DurationSeconds,
         run.StartedAt,
         run.StatusId
      );
   }

   public static string FormatTemperature(AiRunDetail run)
   {
      var temperature = run.PromptTemperature;

      return temperature is null
         ? "Not set"
         : temperature.Value.ToString(CultureInfo.InvariantCulture);
   }

   public static string FormatMaxOutputTokens(AiRunDetail run)
   {
      return run.MaxOutputTokens.ToString(CultureInfo.InvariantCulture);
   }

   public static string FormatTokenCount(int? tokenCount)
   {
      return tokenCount?.ToString(
         "N0",
         CultureInfo.GetCultureInfo(PrimaryCountry.CultureName)
      ) ?? "-";
   }

   public static TokenUsageViewModel? BuildTokenUsage(AiRunDetail run)
   {
      var usage = ParseTokenUsage(run.RawResponseJson);
      var isOpenCode = string.Equals(
         run.ProviderKind,
         AiProviderKinds.OpenCodeCli,
         StringComparison.Ordinal
      );
      var inputTokens = run.InputTokens ?? ReadTokenCount(
         usage,
         "input_tokens",
         "prompt_tokens",
         "tokens_prompt"
      );
      var cachedInputTokens = ReadTokenCount(
         usage,
         "cached_input_tokens",
         "input_cached_tokens",
         "cache_read_input_tokens"
      ) ?? ReadNestedTokenCount(
         usage,
         "input_tokens_details",
         "cached_tokens"
      );
      var cacheWriteInputTokens = ReadTokenCount(
         usage,
         "cache_write_input_tokens",
         "input_cache_write_tokens",
         "cache_creation_input_tokens"
      ) ?? ReadNestedTokenCount(
         usage,
         "input_tokens_details",
         "cache_write_tokens",
         "cache_creation_tokens"
      );

      if(isOpenCode && inputTokens is not null)
      {
         inputTokens = inputTokens.Value +
            (cachedInputTokens ?? 0) +
            (cacheWriteInputTokens ?? 0);
      }

      var uncachedInputTokens = ReadTokenCount(
         usage,
         "input_uncached_tokens",
         "uncached_input_tokens"
      );
      var outputTokens = run.OutputTokens ?? ReadTokenCount(
         usage,
         "output_tokens",
         "completion_tokens",
         "tokens_completion"
      );
      var reasoningTokens = run.ReasoningTokens ?? ReadTokenCount(
         usage,
         "reasoning_output_tokens",
         "reasoning_tokens",
         "tokens_reasoning"
      ) ?? ReadNestedTokenCount(
         usage,
         "output_tokens_details",
         "reasoning_tokens"
      );
      var hasUnreportedReasoning = isOpenCode &&
         reasoningTokens == 0 &&
         HasOpenCodeReasoningContent(run.RawResponseJson);

      if(hasUnreportedReasoning)
      {
         reasoningTokens = null;
      }

      if(uncachedInputTokens is null &&
         inputTokens is not null &&
         cachedInputTokens is not null)
      {
         var calculatedUncachedInputTokens =
            inputTokens.Value - cachedInputTokens.Value -
            (cacheWriteInputTokens ?? 0);

         if(calculatedUncachedInputTokens >= 0)
         {
            uncachedInputTokens = calculatedUncachedInputTokens;
         }
      }

      if(inputTokens is null &&
         cachedInputTokens is null &&
         uncachedInputTokens is null &&
         cacheWriteInputTokens is null &&
         outputTokens is null &&
         reasoningTokens is null &&
         !hasUnreportedReasoning)
      {
         return null;
      }

      return new TokenUsageViewModel(
         inputTokens,
         cachedInputTokens,
         uncachedInputTokens,
         cacheWriteInputTokens,
         outputTokens,
         reasoningTokens,
         hasUnreportedReasoning
      );
   }

   public static string FormatMaxToolRounds(AiRunDetail run)
   {
      var maxToolRounds = run.PromptMaxToolRounds ?? (
         run.JobRequiresWebSearch
            ? LlamaServerDefaults.DefaultMaxToolRounds
            : null
      );

      return maxToolRounds?.ToString(
         CultureInfo.InvariantCulture
      ) ?? "Not set";
   }

   public static string FormatMinToolRounds(AiRunDetail run)
   {
      return run.PromptMinToolRounds?.ToString(
         CultureInfo.InvariantCulture
      ) ?? "Not set";
   }

   public static int GetToolRoundCount(AiRunDetail run)
   {
      return run.ToolRoundCount;
   }

   public static int GetToolRoundCount(int toolRoundCount)
   {
      return toolRoundCount;
   }

   private static JsonElement? ParseTokenUsage(string? rawResponseJson)
   {
      if(string.IsNullOrWhiteSpace(rawResponseJson))
      {
         return null;
      }

      try
      {
         using var document = JsonDocument.Parse(rawResponseJson);
         var root = document.RootElement;

         if(root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("usage", out var usage) &&
            usage.ValueKind == JsonValueKind.Object)
         {
            return usage.Clone();
         }

         return root.ValueKind == JsonValueKind.Object
            ? root.Clone()
            : null;
      }
      catch(JsonException)
      {
         return null;
      }
   }

   private static bool HasOpenCodeReasoningContent(
      string? rawResponseJson
   )
   {
      if(string.IsNullOrWhiteSpace(rawResponseJson))
      {
         return false;
      }

      try
      {
         using var document = JsonDocument.Parse(rawResponseJson);
         var root = document.RootElement;

         if(!root.TryGetProperty("events", out var events) ||
            events.ValueKind != JsonValueKind.Array)
         {
            return false;
         }

         foreach(var eventNode in events.EnumerateArray())
         {
            if(!eventNode.TryGetProperty("type", out var type) ||
               type.ValueKind != JsonValueKind.String ||
               !string.Equals(
                  type.GetString(),
                  OpenCodeReasoningEventType,
                  StringComparison.Ordinal
               ) ||
               !eventNode.TryGetProperty("part", out var part) ||
               part.ValueKind != JsonValueKind.Object ||
               !part.TryGetProperty("text", out var text) ||
               text.ValueKind != JsonValueKind.String)
            {
               continue;
            }

            if(!string.IsNullOrWhiteSpace(text.GetString()))
            {
               return true;
            }
         }
      }
      catch(JsonException)
      {
      }

      return false;
   }

   private static int? ReadNestedTokenCount(
      JsonElement? usage,
      string objectName,
      params string[] propertyNames
   )
   {
      if(usage is null ||
         usage.Value.ValueKind != JsonValueKind.Object ||
         !usage.Value.TryGetProperty(objectName, out var details) ||
         details.ValueKind != JsonValueKind.Object)
      {
         return null;
      }

      return ReadTokenCount(details, propertyNames);
   }

   private static int? ReadTokenCount(
      JsonElement? usage,
      params string[] propertyNames
   )
   {
      return usage is null
         ? null
         : ReadTokenCount(usage.Value, propertyNames);
   }

   private static int? ReadTokenCount(
      JsonElement usage,
      params string[] propertyNames
   )
   {
      if(usage.ValueKind != JsonValueKind.Object)
      {
         return null;
      }

      foreach(var propertyName in propertyNames)
      {
         if(!usage.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Number ||
            !property.TryGetInt32(out var value))
         {
            continue;
         }

         return value;
      }

      return null;
   }

   public static int GetMaxPayloadCharacterCount(AiRunDetail run)
   {
      return GetMaxPayloadCharacterCount(
         run.ConversationCharacterCount,
         run.ToolTraceJson
      );
   }

   public static int GetMaxPayloadCharacterCount(
      int payloadCharacterCount,
      string? toolTraceJson
   )
   {
      var maxRoundPayloadCharacters =
         AiRunToolTracePresenter.ParseToolTrace(toolTraceJson)
         .Select(turn => turn.RoundPayloadCharacterCount ?? 0)
         .DefaultIfEmpty(0)
         .Max();

      return Math.Max(
         payloadCharacterCount,
         maxRoundPayloadCharacters
      );
   }

   private static string FormatDuration(
      decimal? durationSeconds,
      DateTimeOffset startedAt,
      string statusId
   )
   {
      var totalSeconds = durationSeconds is not null
         ? (int)Math.Round(durationSeconds.Value)
         : 0;

      if(string.Equals(
         statusId,
         AiJobRunStatusIds.Running,
         StringComparison.Ordinal
      ))
      {
         totalSeconds = (int)Math.Round(
            (DateTimeOffset.UtcNow - startedAt).TotalSeconds
         );
      }

      if(totalSeconds < 0)
      {
         totalSeconds = 0;
      }

      var timeSpan = TimeSpan.FromSeconds(totalSeconds);

      if(timeSpan.TotalHours >= 1)
      {
         return string.Format(
            "{0}h {1:00}m {2:00}s",
            (int)timeSpan.TotalHours,
            timeSpan.Minutes,
            timeSpan.Seconds
         );
      }

      if(timeSpan.TotalMinutes >= 1)
      {
         return string.Format(
            "{0}m {1:00}s",
            (int)timeSpan.TotalMinutes,
            timeSpan.Seconds
         );
      }

      return $"{timeSpan.Seconds}s";
   }

   public sealed record TokenUsageViewModel(
      int? InputTokens,
      int? CachedInputTokens,
      int? UncachedInputTokens,
      int? CacheWriteInputTokens,
      int? OutputTokens,
      int? ReasoningTokens,
      bool HasUnreportedReasoning = false
   );
}
