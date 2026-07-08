namespace SESport.AI.WebPages;

internal enum WebPageBlockSource
{
   HtmlFallback,
   CurlFallback
}

internal static class WebPageBlockDetection
{
   private static readonly string[] HtmlFallbackSignatures =
   [
      "access denied",
      "you do not have permission to access",
      "you don't have permission to access",
      "errors edgesuite net",
      "reference #"
   ];

   private static readonly string[] CurlFallbackSignatures =
   [
      "access denied",
      "you do not have permission to access",
      "you don't have permission to access",
      "errors edgesuite net",
      "reference #"
   ];

   internal static bool IsBlocked(
      string? title,
      string text,
      WebPageBlockSource source
   )
   {
      return FindBlockedSignature(title, text, source) is not null;
   }

   internal static string? FindBlockedSignature(
      string? title,
      string text,
      WebPageBlockSource source
   )
   {
      var combinedText =
         WebPageContentFetchSupport.NormalizeText($"{title} {text}");

      var signatures = source == WebPageBlockSource.CurlFallback
         ? CurlFallbackSignatures
         : HtmlFallbackSignatures;

      return signatures.FirstOrDefault(signature =>
         combinedText.Contains(signature, StringComparison.OrdinalIgnoreCase));
   }
}
