using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SESport.Core.AI;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.AI;
using SESport.Web.Services;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SESport.Web.Pages.Admin.Runs;

public class DetailsModel(
   AiRepository repository
) : PageModel
{
   private const string ConversationHistorySummaryPrefix =
      "Conversation history summary:";

   public AiRunDetail? Run { get; private set; }

   public string SystemPromptText { get; private set; } = string.Empty;

   public string ConversationHistorySummaryText { get; private set; } =
      string.Empty;

   public string UserPromptTemplateText { get; private set; } = string.Empty;

   public string RenderedPromptText { get; private set; } = string.Empty;

   public IReadOnlyList<ToolTraceTurnViewModel> ToolTraceTurns
   {
      get; private
      set;
   } = [];

   public IReadOnlyList<SelectListItem> ExecutionEnvironmentOptions
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   private ISet<string> KnownExecutionEnvironmentValues { get; set; } =
      new HashSet<string>(StringComparer.Ordinal);

   public IReadOnlyList<ToolTraceBadgeViewModel> ToolTraceSummaryBadges
   {
      get
      {
         return BuildToolTraceSummaryBadges(ToolTraceTurns);
      }
   }

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
         ExecutionEnvironment = Run.ExecutionEnvironment;
         await LoadExecutionEnvironmentOptionsAsync(
            Run.ExecutionEnvironment,
            cancellationToken
         );
         ToolTraceTurns = ParseToolTrace(Run.ToolTraceJson);
         ConversationHistorySummaryText =
            GetConversationHistorySummaryText(Run.RawRequestJson);
         SystemPromptText = Run.SystemPrompt;
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
      {
         LoadError = exception.Message;
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
         SESport.Core.AI.ExecutionEnvironment.Current
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
      if(string.IsNullOrWhiteSpace(value))
      {
         return "";
      }

      if(TryPrettyPrintJson(value, out var prettyPrinted))
      {
         return prettyPrinted;
      }

      return value.Trim();
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

   public static int GetToolRoundCount(AiRunDetail run)
   {
      return run.ToolRoundCount;
   }

   public static int GetToolRoundCount(int toolRoundCount)
   {
      return toolRoundCount;
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
      var maxRoundPayloadCharacters = ParseToolTrace(toolTraceJson)
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

   public static string FormatToolCall(ToolTraceCallViewModel toolCall)
   {
      var arguments = TryParseArguments(toolCall.Arguments);

      if(arguments is null || arguments.Count == 0)
      {
         return toolCall.Name;
      }

      var parts = new List<string>();
      var find = GetString(arguments, "find");
      var query = GetString(arguments, "query");
      var id = GetString(arguments, "id");
      var url = GetString(arguments, "url");
      var limit = GetInt32(arguments, "limit");

      if(string.Equals(
         toolCall.Name,
         WebToolNames.Search,
         StringComparison.Ordinal
      ))
      {
         if(!string.IsNullOrWhiteSpace(query))
         {
            parts.Add(FormatQuoted(query));
         }

         if(limit is not null)
         {
            parts.Add(limit.Value.ToString());
         }
      }
      else if(string.Equals(
         toolCall.Name,
         WebToolNames.FindInPage,
         StringComparison.Ordinal
      ) || string.Equals(
         toolCall.Name,
         WebToolNames.GetPage,
         StringComparison.Ordinal
      ))
      {
         if(!string.IsNullOrWhiteSpace(id))
         {
            parts.Add(FormatQuoted(id));
         }
         else if(!string.IsNullOrWhiteSpace(url))
         {
            parts.Add(FormatQuoted(url));
         }

         if(!string.IsNullOrWhiteSpace(find))
         {
            parts.Add(FormatQuoted(find));
         }
      }
      else
      {
         parts.Add(FormatCompactJson(arguments));
      }

      return parts.Count == 0
         ? toolCall.Name
         : $"{toolCall.Name}({string.Join(",", parts)})";
   }

   public static string FormatToolCallSummary(
      string toolName,
      int toolCallCount,
      int uniqueToolCallCount
   )
   {
      return $"{toolName} x {toolCallCount} ({uniqueToolCallCount})";
   }

   public static string GetToolBadgeCssClass(string toolName)
   {
      return string.Equals(
         toolName,
         WebToolNames.SubmitReport,
         StringComparison.Ordinal
      )
         ? "tool-trace-badge tool-trace-badge-submit-report"
         : "tool-trace-badge tool-trace-badge-tool";
   }

   private static IReadOnlyList<ToolTraceTurnViewModel> ParseToolTrace(
      string? toolTraceJson
   )
   {
      if(string.IsNullOrWhiteSpace(toolTraceJson))
      {
         return [];
      }

      try
      {
         using var document = JsonDocument.Parse(toolTraceJson);
         if(document.RootElement.ValueKind != JsonValueKind.Array)
         {
            return [];
         }

         var turns = new Dictionary<int, ToolTraceTurnBuilder>();

         foreach(var entry in document.RootElement.EnumerateArray())
         {
            if(entry.ValueKind != JsonValueKind.Object)
            {
               continue;
            }

            var kind = GetString(entry, "kind");
            var turn = GetInt32(entry, "turn") ?? 0;
            var builder = GetOrCreateTurn(turns, turn);
            var payloadChars = GetInt32(entry, "payload_chars");

            if(payloadChars is not null)
            {
               builder.RoundPayloadCharacterCount = Math.Max(
                  builder.RoundPayloadCharacterCount ?? 0,
                  payloadChars.Value
               );
            }

            if(string.Equals(kind, "assistant", StringComparison.Ordinal))
            {
               builder.FinishReason = GetString(entry, "finish_reason");
               builder.AssistantContent = GetString(entry, "content");
               builder.AssistantReasoningContent =
                  GetString(entry, "reasoning_content");
               builder.AssistantValidationStatus =
                  GetString(entry, "validation_status");
               builder.AssistantValidationError =
                  GetString(entry, "validation_error");
               builder.ToolCalls.AddRange(ParseToolCalls(entry));
               continue;
            }

            if(string.Equals(kind, "budget", StringComparison.Ordinal))
            {
               builder.Temperature ??= GetDecimal(entry, "temperature");
               continue;
            }

            if(string.Equals(kind, "tool", StringComparison.Ordinal))
            {
               builder.ToolResults.Add(ParseToolResult(entry));
               continue;
            }

            if(string.Equals(kind, "repair_prompt", StringComparison.Ordinal))
            {
               var repairPrompt = GetString(entry, "content");

               if(!string.IsNullOrWhiteSpace(repairPrompt))
               {
                  builder.RepairPrompts.Add(repairPrompt);
               }
            }
         }

         return turns
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value.ToViewModel())
            .ToArray();
      }
      catch(JsonException)
      {
         return [];
      }
   }

   private static IReadOnlyList<ToolTraceBadgeViewModel>
      BuildToolTraceSummaryBadges(
         IReadOnlyList<ToolTraceTurnViewModel> turns
      )
   {
      if(turns.Count == 0)
      {
         return [];
      }

      var badges = new List<ToolTraceBadgeViewModel>
      {
         new(
            $"{turns.Count} round{(turns.Count == 1 ? "" : "s")}",
            "tool-trace-badge-round"
         )
      };

      var toolCalls = turns
         .SelectMany(turn => turn.ToolCalls)
         .GroupBy(call => call.Name, StringComparer.Ordinal)
         .OrderBy(group => group.Key, StringComparer.Ordinal)
         .ToArray();

      foreach(var toolCallGroup in toolCalls)
      {
         var uniqueToolCallCount = toolCallGroup
            .Select(FormatToolCall)
            .Distinct(StringComparer.Ordinal)
            .Count();
         badges.Add(new(
            FormatToolCallSummary(
               toolCallGroup.Key,
               toolCallGroup.Count(),
               uniqueToolCallCount
            ),
            GetToolBadgeCssClass(toolCallGroup.Key)
         ));
      }

      var totalToolResults = turns.Sum(turn => turn.ToolResults.Count);
      if(totalToolResults > 0)
      {
         badges.Add(new(
            $"{totalToolResults} result{(totalToolResults == 1 ? "" : "s")}",
            "tool-trace-badge-result"
         ));
      }

      var finalTurn = turns.LastOrDefault();
      if(finalTurn is not null &&
         !string.IsNullOrWhiteSpace(finalTurn.FinishReason))
      {
         badges.Add(new(
            $"Finish: {finalTurn.FinishReason}",
            "tool-trace-badge-finish"
         ));
      }

      return badges;
   }

   private static ToolTraceTurnBuilder GetOrCreateTurn(
      IDictionary<int, ToolTraceTurnBuilder> turns,
      int turn
   )
   {
      if(!turns.TryGetValue(turn, out var builder))
      {
         builder = new ToolTraceTurnBuilder(turn);
         turns[turn] = builder;
      }

      return builder;
   }

   private static IReadOnlyList<ToolTraceCallViewModel> ParseToolCalls(
      JsonElement entry
   )
   {
      if(!TryGetArray(entry, "tool_calls", out var toolCalls))
      {
         return [];
      }

      return toolCalls
         .Where(call => call.ValueKind == JsonValueKind.Object)
         .Select(call => new ToolTraceCallViewModel(
            GetString(call, "id") ?? "",
            GetString(call, "name") ?? "",
            FormatDisplayValue(GetProperty(call, "arguments"))
         ))
         .ToArray();
   }

   private static ToolTraceToolResultViewModel ParseToolResult(
      JsonElement entry
   )
   {
      return new ToolTraceToolResultViewModel(
         GetString(entry, "tool_call_id") ?? "",
         GetString(entry, "name") ?? "",
         FormatDisplayValue(GetProperty(entry, "arguments")),
         GetString(entry, "query"),
         GetString(entry, "id"),
         GetString(entry, "search_provider"),
         GetString(entry, "search_provider_details"),
         GetString(entry, "search_engine"),
         GetString(entry, "fetcher"),
         FormatDisplayValue(GetProperty(entry, "result"))
      );
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

   private static int? GetInt32(JsonElement element, string name)
   {
      if(!element.TryGetProperty(name, out var property))
      {
         return null;
      }

      return property.ValueKind == JsonValueKind.Number &&
         property.TryGetInt32(out var value)
         ? value
         : null;
   }

   private static decimal? GetDecimal(JsonElement element, string name)
   {
      if(!element.TryGetProperty(name, out var property))
      {
         return null;
      }

      return property.ValueKind == JsonValueKind.Number &&
         property.TryGetDecimal(out var value)
         ? value
         : null;
   }

   private static bool TryGetArray(
      JsonElement element,
      string name,
      out IEnumerable<JsonElement> values
   )
   {
      values = [];

      if(!element.TryGetProperty(name, out var property) ||
         property.ValueKind != JsonValueKind.Array)
      {
         return false;
      }

      values = property.EnumerateArray().ToArray();
      return true;
   }

   private static JsonElement? GetProperty(JsonElement element, string name)
   {
      return element.TryGetProperty(name, out var property) ? property : null;
   }

   private static JsonObject? TryParseArguments(string arguments)
   {
      if(string.IsNullOrWhiteSpace(arguments))
      {
         return null;
      }

      try
      {
         return JsonDocument.Parse(arguments).RootElement.ValueKind ==
            JsonValueKind.Object
            ? JsonSerializer.Deserialize<JsonObject>(arguments)
            : null;
      }
      catch(JsonException)
      {
         return null;
      }
   }

   private static string? GetString(JsonObject? element, string name)
   {
      if(element is null || !element.TryGetPropertyValue(name, out var value))
      {
         return null;
      }

      return value is JsonValue jsonValue &&
         jsonValue.TryGetValue<string>(out var text)
         ? text
         : value?.ToString();
   }

   private static int? GetInt32(JsonObject? element, string name)
   {
      if(element is null || !element.TryGetPropertyValue(name, out var value))
      {
         return null;
      }

      return value is JsonValue jsonValue &&
         jsonValue.TryGetValue<int>(out var number)
         ? number
         : null;
   }

   private static string FormatQuoted(string value)
   {
      return "'" + value.Replace("'", "\\'") + "'";
   }

   private static string FormatCompactJson(JsonObject value)
   {
      return JsonSerializer.Serialize(value);
   }

   private static string FormatDisplayValue(JsonElement? value)
   {
      if(value is null)
      {
         return "";
      }

      var text = value.Value.ValueKind switch
      {
         JsonValueKind.String => value.Value.GetString() ?? "",
         JsonValueKind.Null => "",
         _ => value.Value.GetRawText()
      };

      if(TryPrettyPrintJson(text, out var prettyPrinted))
      {
         return prettyPrinted;
      }

      return text.Trim();
   }

   private static bool TryPrettyPrintJson(
      string value,
      out string prettyPrinted
   )
   {
      prettyPrinted = "";

      if(string.IsNullOrWhiteSpace(value))
      {
         return false;
      }

      try
      {
         using var document = JsonDocument.Parse(value);
         prettyPrinted = JsonSerializer.Serialize(
            document.RootElement,
            new JsonSerializerOptions
            {
               WriteIndented = true
            }
         );
         return true;
      }
      catch(JsonException)
      {
         return false;
      }
   }

   public sealed record ToolTraceCallViewModel(
      string Id,
      string Name,
      string Arguments
   );

   public sealed record ToolTraceBadgeViewModel(
      string Text,
      string CssClass
   );

   public sealed record ToolTraceToolResultViewModel(
      string ToolCallId,
      string Name,
      string Arguments,
      string? Query,
      string? Id,
      string? SearchProvider,
      string? SearchProviderDetails,
      string? SearchEngine,
      string? Fetcher,
      string Result
   );

   public sealed record ToolTraceTurnViewModel(
      int Turn,
      int? RoundPayloadCharacterCount,
      decimal? Temperature,
      string? FinishReason,
      string? AssistantContent,
      string? AssistantReasoningContent,
      string? AssistantValidationStatus,
      string? AssistantValidationError,
      IReadOnlyList<string> RepairPrompts,
      IReadOnlyList<ToolTraceCallViewModel> ToolCalls,
      IReadOnlyList<ToolTraceToolResultViewModel> ToolResults,
      IReadOnlyList<ToolTraceBadgeViewModel> CompactBadges,
      string? AssistantPreview
   );

   private sealed class ToolTraceTurnBuilder(int turn)
   {
      public int Turn { get; } = turn;

      public int? RoundPayloadCharacterCount { get; set; }

      public decimal? Temperature { get; set; }

      public string? FinishReason { get; set; }

      public string? AssistantContent { get; set; }

      public string? AssistantReasoningContent { get; set; }

      public string? AssistantValidationStatus { get; set; }

      public string? AssistantValidationError { get; set; }

      public List<string> RepairPrompts { get; } = [];

      public List<ToolTraceCallViewModel> ToolCalls { get; } = [];

      public List<ToolTraceToolResultViewModel> ToolResults { get; } = [];

      public ToolTraceTurnViewModel ToViewModel()
      {
         return new ToolTraceTurnViewModel(
            Turn,
            RoundPayloadCharacterCount,
            Temperature,
            FinishReason,
            AssistantContent,
            AssistantReasoningContent,
            AssistantValidationStatus,
            AssistantValidationError,
            RepairPrompts,
            ToolCalls,
            ToolResults,
            BuildCompactBadges(),
            BuildAssistantPreview()
         );
      }

      private IReadOnlyList<ToolTraceBadgeViewModel> BuildCompactBadges()
      {
         var badges = new List<ToolTraceBadgeViewModel>
         {
            new($"Round {Turn}", "tool-trace-badge-round")
         };

         if(RoundPayloadCharacterCount is not null)
         {
            badges.Add(new(
               $"Payload chars " +
               $"{RoundPayloadCharacterCount.Value:N0}",
               "tool-trace-badge-count"
            ));
         }

         if(Temperature is not null)
         {
            badges.Add(new(
               $"Temp " +
               $"{Temperature.Value.ToString(CultureInfo.InvariantCulture)}",
               "tool-trace-badge-temperature"
            ));
         }

         if(!string.IsNullOrWhiteSpace(AssistantContent))
         {
            badges.Add(new("Assistant", "tool-trace-badge-assistant"));
         }

         if(!string.IsNullOrWhiteSpace(AssistantReasoningContent))
         {
            badges.Add(new("Reasoning", "tool-trace-badge-assistant"));
         }

         if(!string.IsNullOrWhiteSpace(AssistantValidationStatus))
         {
            badges.Add(new(
               $"Validation: {AssistantValidationStatus}",
               "tool-trace-badge-result"
            ));
         }

         foreach(var toolCallGroup in ToolCalls
            .GroupBy(call => call.Name, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
         {
            badges.Add(new(
               $"{toolCallGroup.Key} × {toolCallGroup.Count()}",
               GetToolBadgeCssClass(toolCallGroup.Key)
            ));
         }

         if(ToolResults.Count > 0)
         {
            badges.Add(new(
               $"{ToolResults.Count} result{(ToolResults.Count == 1 ? "" :
                  "s")}",
               "tool-trace-badge-result"
            ));
         }

         if(!string.IsNullOrWhiteSpace(FinishReason))
         {
            badges.Add(new(
               $"Finish: {FinishReason}",
               "tool-trace-badge-finish"
            ));
         }

         return badges;
      }

      private string? BuildAssistantPreview()
      {
         return BuildCompactPreview(AssistantContent, 120);
      }
   }

   private static string? BuildCompactPreview(
      string? value,
      int maxLength
   )
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      var text = string.Join(
         " ",
         value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
      ).Trim();

      if(text.Length <= maxLength)
      {
         return text;
      }

      return text[..(maxLength - 3)] + "...";
   }
}
