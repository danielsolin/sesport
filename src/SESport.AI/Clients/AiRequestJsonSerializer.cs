using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SESport.AI.Clients;

internal static class AiRequestJsonSerializer
{
   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   )
   {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };

   public static string Serialize(JsonObject payload)
   {
      return JsonSerializer.Serialize(payload, JsonOptions);
   }
}
