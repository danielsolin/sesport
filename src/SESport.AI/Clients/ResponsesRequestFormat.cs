using System.Text.Json.Nodes;

using SESport.Core.AI;

namespace SESport.AI.Clients;

internal static class ResponsesRequestFormat
{
   public static void Apply(
      JsonObject payload,
      string outputMode,
      string? outputSchemaJson,
      string schemaName
   )
   {
      if(!string.IsNullOrWhiteSpace(outputSchemaJson))
      {
         var schema = JsonNode.Parse(outputSchemaJson) as JsonObject;

         if(schema is not null)
         {
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

            return;
         }
      }

      if(!string.Equals(
         outputMode,
         AiOutputModeIds.JsonObject,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return;
      }

      payload["response_format"] = new JsonObject
      {
         ["type"] = AiOutputModeIds.JsonObject
      };
   }
}
