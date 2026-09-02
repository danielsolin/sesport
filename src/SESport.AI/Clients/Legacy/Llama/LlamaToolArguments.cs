using System.Text.Json;

namespace SESport.AI.Llama;

internal static class LlamaToolArguments
{
   public static string ExtractQuery(string arguments)
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

   public static int ExtractLimit(string arguments)
   {
      if(string.IsNullOrWhiteSpace(arguments))
      {
         return 10;
      }

      try
      {
         using var document = JsonDocument.Parse(arguments);
         var root = document.RootElement;

         if(
            root.TryGetProperty("limit", out var maxResultsNode) &&
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

      return 10;
   }

   public static string ExtractUrl(string arguments)
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
            TryGetStringProperty(root, "url", out var url) &&
            !string.IsNullOrWhiteSpace(url)
         )
         {
            return url;
         }
      }
      catch(JsonException)
      {
      }

      return "";
   }

   public static string ExtractFind(string arguments)
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
            TryGetStringProperty(root, "find", out var find) &&
            !string.IsNullOrWhiteSpace(find)
         )
         {
            return find;
         }
      }
      catch(JsonException)
      {
      }

      return "";
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
}
