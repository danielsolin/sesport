namespace SESport.Core.Ingestion;

public static class ActivityProposalDisplayFormatter
{
   public static bool HasAiPrompt(
      string producerTypeId,
      string? prompt
   )
   {
      return string.Equals(
         producerTypeId,
         ActivityProposalProducerType.AiSearch.ToString(),
         StringComparison.Ordinal
      ) &&
      !string.IsNullOrWhiteSpace(prompt);
   }
}
