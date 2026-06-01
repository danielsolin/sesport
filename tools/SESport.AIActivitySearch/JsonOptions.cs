using System.Text.Json;
using System.Text.Json.Serialization;

namespace SESport.Tools.AIActivitySearch;

internal static class JsonOptions
{
   public static JsonSerializerOptions Value { get; } = new(
      JsonSerializerDefaults.Web
   )
   {
      WriteIndented = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };
}
