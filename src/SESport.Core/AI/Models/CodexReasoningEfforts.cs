namespace SESport.Core.AI;

public static class CodexReasoningEfforts
{
   public const string Low = "low";

   public const string Medium = "medium";

   public const string High = "high";

   public const string XHigh = "xhigh";

   public const string Max = "max";

   public const string Default = Medium;

   public static IReadOnlyList<string> Values { get; } =
   [
      Low,
      Medium,
      High,
      XHigh,
      Max
   ];

   public static bool IsSupported(string? value)
   {
      return value is Low or Medium or High or XHigh or Max;
   }
}
