namespace SESport.AI.WebPages;

internal enum WebPageBlockSource
{
   HtmlFallback,
   CurlFallback,
   Browser
}

internal static class WebPageBlockDetection
{
   private static readonly string[] FallbackSignatures =
   [
      "access denied",
      "you do not have permission to access",
      "you don't have permission to access",
      "errors edgesuite net",
      "reference #",
      "performing security verification",
      "incompatible browser extension or network configuration",
      "this website uses a security service to protect against " +
      "malicious bots",
      "checking your browser"
   ];

   private static readonly string[] BrowserSignatures =
   [
      "performing security verification",
      "incompatible browser extension or network configuration",
      "this website uses a security service to protect against " +
      "malicious bots",
      "checking your browser"
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

      var signatures = source switch
      {
         WebPageBlockSource.Browser => BrowserSignatures,
         _ => FallbackSignatures
      };

      return signatures.FirstOrDefault(signature =>
         combinedText.Contains(signature, StringComparison.OrdinalIgnoreCase));
   }
}
