using System.Text.Json;

namespace SESport.Web.Pages.Admin.Runs;

internal static class AiRunJsonFormatter
{
   private static readonly JsonSerializerOptions IndentedJsonOptions = new()
   {
      WriteIndented = true
   };

   internal static string Format(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return "";
      }

      if(TryPrettyPrint(value, out var prettyPrinted))
      {
         return prettyPrinted;
      }

      return value.Trim();
   }

   internal static bool TryPrettyPrint(
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
            IndentedJsonOptions
         );
         return true;
      }
      catch(JsonException)
      {
         return false;
      }
   }
}
