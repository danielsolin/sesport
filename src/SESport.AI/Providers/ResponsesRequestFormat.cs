using System.Text.Json.Nodes;

namespace SESport.AI.Providers;

internal static class ResponsesRequestFormat
{
   public static void Apply(
      JsonObject payload,
      string outputMode,
      string? outputSchemaJson,
      string schemaName
   )
   {
      if(string.Equals(
         outputMode,
         "json_object",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         payload["response_format"] = new JsonObject
         {
            ["type"] = "json_object"
         };

         return;
      }

      if(!string.Equals(
         outputMode,
         "json_schema",
         StringComparison.OrdinalIgnoreCase
      ) || string.IsNullOrWhiteSpace(outputSchemaJson))
      {
         return;
      }

      var schema = JsonNode.Parse(outputSchemaJson) as JsonObject;

      if(schema is null)
      {
         return;
      }

      payload["response_format"] = new JsonObject
      {
         ["type"] = "json_schema",
         ["json_schema"] = new JsonObject
         {
            ["name"] = schemaName,
            ["strict"] = true,
            ["schema"] = schema
         }
      };
   }
}
