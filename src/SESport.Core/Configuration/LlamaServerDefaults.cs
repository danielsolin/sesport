namespace SESport.Core.Configuration;

public static class LlamaServerDefaults
{
   public const int MaxConversationContextCharacters = 250000;
   public const int MaxTransientRetryAttempts = 12;
   public const int MaxFormatRepairAttempts = 3;
   public const int MaxFinalReportCorrectionAttempts = 3;
   public const int MaxToolFormatFallbackAttempts = 5;
   public const int DefaultMaxToolRounds = 10;
   public const int DefaultConversationSummaryCharacters = 220;
   public const int PreviewSnippetCharacters = 240;
   public const int MaxFindInPageSnippetCount = 50;
   public static readonly IReadOnlyList<TimeSpan> TransientRetryDelays =
   [
      TimeSpan.FromSeconds(1),
      TimeSpan.FromSeconds(2),
      TimeSpan.FromSeconds(4),
      TimeSpan.FromSeconds(8),
      TimeSpan.FromSeconds(16),
      TimeSpan.FromSeconds(30)
   ];
}
