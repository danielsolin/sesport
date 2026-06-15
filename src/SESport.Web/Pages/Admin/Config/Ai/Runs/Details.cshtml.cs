using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text.Json.Nodes;
using SESport.AI.Models;
using SESport.AI.Persistence;
using SESport.Core.Formatting;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Config.Ai.Runs;

public class DetailsModel(AiRepository repository) : PageModel
{
   public AiRunDetail? Run { get; private set; }

   public string SystemPromptText { get; private set; } = string.Empty;

   public string RenderedPromptText { get; private set; } = string.Empty;

   public IReadOnlyList<ToolTraceTurnViewModel> ToolTraceTurns { get; private
      set; } = [];

   public IReadOnlyList<ToolTraceBadgeViewModel> ToolTraceSummaryBadges
   {
      get
      {
         return BuildToolTraceSummaryBadges(ToolTraceTurns);
      }
   }

   [BindProperty(SupportsGet = true)]
   public string? JobId { get; set; }

   [BindProperty(SupportsGet = true)]
   public string? StatusId { get; set; }

   [BindProperty(SupportsGet = true, Name = RouteKeys.Date)]
   public DateOnly? Date { get; set; }

   public string DateText => DateDisplay.Format(SelectedDate);

   public DateOnly SelectedDate { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      SelectedDate = Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
      Run = await repository.GetRunAsync(id, cancellationToken);

      if(Run is not null)
      {
         ToolTraceTurns = ParseToolTrace(Run.ToolTraceJson);
         SystemPromptText = Run.SystemPrompt;
         RenderedPromptText = Run.RenderedPrompt;
      }

      return Run is null ? NotFound() : Page();
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

      if(string.Equals(toolCall.Name, "web_search", StringComparison.Ordinal))
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
         "web_find_in_page",
         StringComparison.Ordinal
      ) || string.Equals(
         toolCall.Name,
         "web_get_page",
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

            if(string.Equals(kind, "assistant", StringComparison.Ordinal))
            {
               builder.FinishReason = GetString(entry, "finish_reason");
               builder.AssistantContent = GetString(entry, "content");
               builder.ToolCalls.AddRange(ParseToolCalls(entry));
               continue;
            }

            if(string.Equals(kind, "tool", StringComparison.Ordinal))
            {
               builder.ToolResults.Add(ParseToolResult(entry));
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
         badges.Add(new(
            $"{toolCallGroup.Key} × {toolCallGroup.Count()}",
            "tool-trace-badge-tool"
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
      string Result
   );

   public sealed record ToolTraceTurnViewModel(
      int Turn,
      string? FinishReason,
      string? AssistantContent,
      IReadOnlyList<ToolTraceCallViewModel> ToolCalls,
      IReadOnlyList<ToolTraceToolResultViewModel> ToolResults,
      IReadOnlyList<ToolTraceBadgeViewModel> CompactBadges,
      string? AssistantPreview
   );

   private sealed class ToolTraceTurnBuilder(int turn)
   {
      public int Turn { get; } = turn;

      public string? FinishReason { get; set; }

      public string? AssistantContent { get; set; }

      public List<ToolTraceCallViewModel> ToolCalls { get; } = [];

      public List<ToolTraceToolResultViewModel> ToolResults { get; } = [];

      public ToolTraceTurnViewModel ToViewModel()
      {
         return new ToolTraceTurnViewModel(
            Turn,
            FinishReason,
            AssistantContent,
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

         if(!string.IsNullOrWhiteSpace(AssistantContent))
         {
            badges.Add(new("Assistant", "tool-trace-badge-assistant"));
         }

         foreach(var toolCallGroup in ToolCalls
            .GroupBy(call => call.Name, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
         {
            badges.Add(new(
               $"{toolCallGroup.Key} × {toolCallGroup.Count()}",
               "tool-trace-badge-tool"
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
