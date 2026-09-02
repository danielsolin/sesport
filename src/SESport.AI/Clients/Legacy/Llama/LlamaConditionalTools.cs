using System.Text.Json;
using System.Text.Json.Nodes;

using SESport.Core.AI;

namespace SESport.AI.Llama;

internal sealed record LlamaConditionalTool(
   string Name,
   string? Behavior,
   JsonObject Tool
);

internal static class LlamaConditionalTools
{
   private const string PromptOutputSchemaRef = "prompt.output_schema";

   public static IReadOnlyList<LlamaConditionalTool> Resolve(
      string? conditionalToolsJson,
      AiJobDefinition job,
      AiPromptDefinition prompt
   )
   {
      if(string.IsNullOrWhiteSpace(conditionalToolsJson))
      {
         return [];
      }

      JsonArray rules;

      try
      {
         rules = JsonNode.Parse(conditionalToolsJson) as JsonArray
            ?? throw new JsonException(
               "Conditional tools JSON must be an array."
            );
      }
      catch(JsonException)
      {
         throw new InvalidOperationException(
            "Conditional tools JSON must be a JSON array."
         );
      }

      var tools = new List<LlamaConditionalTool>();
      var seenNames = new HashSet<string>(StringComparer.Ordinal);

      foreach(var ruleNode in rules.OfType<JsonObject>())
      {
         if(!MatchesCondition(ruleNode["when"], job, prompt))
         {
            continue;
         }

         var behavior = GetString(ruleNode, "behavior");
         var toolTemplates = GetToolTemplates(ruleNode).ToArray();

         foreach(var toolTemplate in toolTemplates)
         {
            var resolvedTool = ResolveTemplateNode(toolTemplate, prompt);

            if(resolvedTool is not JsonObject toolObject)
            {
               continue;
            }

            ApplyToolPatches(
               toolObject,
               GetArray(ruleNode, "tool_patches"),
               prompt
            );

            var name = GetToolName(toolObject);

            if(string.IsNullOrWhiteSpace(name) ||
               !seenNames.Add(name))
            {
               continue;
            }

            tools.Add(new LlamaConditionalTool(name, behavior, toolObject));
         }
      }

      return tools;
   }

   private static IEnumerable<JsonNode?> GetToolTemplates(JsonObject ruleNode)
   {
      if(ruleNode["tools"] is JsonArray tools)
      {
         return tools.ToArray();
      }

      if(ruleNode["tool"] is not null)
      {
         return [ruleNode["tool"]];
      }

      return [];
   }

   private static bool MatchesCondition(
      JsonNode? whenNode,
      AiJobDefinition job,
      AiPromptDefinition prompt
   )
   {
      if(whenNode is not JsonObject when)
      {
         return true;
      }

      if(!MatchesStringCondition(when, "job_id", job.Id))
      {
         return false;
      }

      if(!MatchesStringCondition(when, "output_mode", job.OutputMode))
      {
         return false;
      }

      if(!MatchesBooleanCondition(
         when,
         "requires_web_search",
         job.RequiresWebSearch
      ))
      {
         return false;
      }

      if(!MatchesBooleanCondition(
         when,
         "prompt_output_schema_present",
         !string.IsNullOrWhiteSpace(prompt.OutputSchemaJson)
      ))
      {
         return false;
      }

      return true;
   }

   private static bool MatchesStringCondition(
      JsonObject when,
      string propertyName,
      string value
   )
   {
      if(!when.TryGetPropertyValue(propertyName, out var condition))
      {
         return true;
      }

      if(condition is JsonValue jsonValue &&
         jsonValue.TryGetValue<string>(out var text))
      {
         return string.Equals(text, value, StringComparison.Ordinal);
      }

      if(condition is JsonArray values)
      {
         return values.OfType<JsonValue>().Any(item =>
            item.TryGetValue<string>(out var text) &&
            string.Equals(text, value, StringComparison.Ordinal)
         );
      }

      return false;
   }

   private static bool MatchesBooleanCondition(
      JsonObject when,
      string propertyName,
      bool value
   )
   {
      if(!when.TryGetPropertyValue(propertyName, out var condition))
      {
         return true;
      }

      return condition is JsonValue jsonValue &&
         jsonValue.TryGetValue<bool>(out var expected)
         ? expected == value
         : false;
   }

   private static JsonArray? GetArray(JsonObject ruleNode, string name)
   {
      return ruleNode.TryGetPropertyValue(name, out var value) &&
         value is JsonArray array
         ? array
         : null;
   }

   private static void ApplyToolPatches(
      JsonObject tool,
      JsonArray? patches,
      AiPromptDefinition prompt
   )
   {
      if(patches is null)
      {
         return;
      }

      foreach(var patchNode in patches.OfType<JsonObject>())
      {
         var path = GetString(patchNode, "path");

         if(string.IsNullOrWhiteSpace(path))
         {
            continue;
         }

         if(!patchNode.TryGetPropertyValue("value", out var valueNode))
         {
            continue;
         }

         ApplyPatch(tool, path, ResolveTemplateNode(valueNode, prompt));
      }
   }

   private static void ApplyPatch(
      JsonNode node,
      string path,
      JsonNode? value
   )
   {
      var segments = path.Split(
         '.',
         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
      );

      ApplyPatch(node, segments, 0, value);
   }

   private static void ApplyPatch(
      JsonNode node,
      string[] segments,
      int index,
      JsonNode? value
   )
   {
      if(index >= segments.Length || node is not JsonObject obj)
      {
         return;
      }

      var segment = segments[index];

      if(index == segments.Length - 1)
      {
         obj[segment] = value?.DeepClone();
         return;
      }

      if(!obj.TryGetPropertyValue(segment, out var child) || child is null)
      {
         child = new JsonObject();
         obj[segment] = child;
      }

      ApplyPatch(child, segments, index + 1, value);
   }

   private static JsonNode? ResolveTemplateNode(
      JsonNode? node,
      AiPromptDefinition prompt
   )
   {
      if(node is null)
      {
         return null;
      }

      if(node is JsonObject obj)
      {
         if(TryResolveReference(obj, prompt, out var reference))
         {
            return reference;
         }

         var resolved = new JsonObject();

         foreach(var property in obj)
         {
            resolved[property.Key] =
               ResolveTemplateNode(property.Value, prompt);
         }

         return resolved;
      }

      if(node is JsonArray array)
      {
         var resolved = new JsonArray();

         foreach(var item in array)
         {
            resolved.Add(ResolveTemplateNode(item, prompt));
         }

         return resolved;
      }

      return node.DeepClone();
   }

   private static bool TryResolveReference(
      JsonObject node,
      AiPromptDefinition prompt,
      out JsonNode? reference
   )
   {
      reference = null;

      if(node.Count != 1 ||
         !node.TryGetPropertyValue("$ref", out var refValue) ||
         refValue is not JsonValue jsonValue ||
         !jsonValue.TryGetValue<string>(out var referenceName))
      {
         return false;
      }

      if(string.Equals(
         referenceName,
         PromptOutputSchemaRef,
         StringComparison.Ordinal
      ))
      {
         if(string.IsNullOrWhiteSpace(prompt.OutputSchemaJson))
         {
            throw new InvalidOperationException(
               "Conditional tools requested the prompt output schema, " +
               "but the prompt has no output schema JSON."
            );
         }

         reference = JsonNode.Parse(prompt.OutputSchemaJson);
         return reference is not null;
      }

      return false;
   }

   private static string GetToolName(JsonObject tool)
   {
      return GetString(tool["function"] as JsonObject, "name") ?? "";
   }

   private static string? GetString(JsonObject? element, string name)
   {
      if(element is null ||
         !element.TryGetPropertyValue(name, out var value))
      {
         return null;
      }

      return value is JsonValue jsonValue &&
         jsonValue.TryGetValue<string>(out var text)
         ? text
         : value?.ToString();
   }
}
