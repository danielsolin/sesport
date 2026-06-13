namespace SESport.AI.Models;

public sealed class AiProviderExecutionException : Exception
{
   public AiProviderExecutionException(
      string message,
      Exception? innerException = null,
      string? rawRequestJson = null,
      string? rawResponseJson = null,
      string? toolTraceJson = null
   )
      : base(message, innerException)
   {
      RawRequestJson = rawRequestJson;
      RawResponseJson = rawResponseJson;
      ToolTraceJson = toolTraceJson;
   }

   public string? RawRequestJson { get; }

   public string? RawResponseJson { get; }

   public string? ToolTraceJson { get; }
}
