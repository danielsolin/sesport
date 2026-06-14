namespace SESport.AI.Models;

public sealed class AiProviderExecutionException : Exception
{
   public AiProviderExecutionException(
      string message,
      Exception? innerException = null,
      string? rawRequestJson = null,
      string? rawResponseJson = null,
      string? toolTraceJson = null,
      int toolRoundCount = 0,
      int conversationCharacterCount = 0
   )
      : base(message, innerException)
   {
      RawRequestJson = rawRequestJson;
      RawResponseJson = rawResponseJson;
      ToolTraceJson = toolTraceJson;
      ToolRoundCount = toolRoundCount;
      ConversationCharacterCount = conversationCharacterCount;
   }

   public string? RawRequestJson { get; }

   public string? RawResponseJson { get; }

   public string? ToolTraceJson { get; }

   public int ToolRoundCount { get; }

   public int ConversationCharacterCount { get; }
}
