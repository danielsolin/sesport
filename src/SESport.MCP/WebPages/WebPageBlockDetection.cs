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
      "access denied",
      "you do not have permission to access",
      "you don't have permission to access",
      "errors edgesuite net",
      "performing security verification",
      "incompatible browser extension or network configuration",
      "this website uses a security service to protect against " +
      "malicious bots",
      "checking your browser"
   ];
   private static readonly string[] StrongSoftErrorSignatures =
   [
      "this page does not exist",
      "this page doesn't exist",
      "requested page could not be found",
      "the requested page could not be found",
      "sidan kunde inte hittas",
      "sidan finns inte",
      "sidan hittades inte"
   ];
   private static readonly string[] ShortPageSoftErrorSignatures =
   [
      "404 not found",
      "error 404",
      "page not found",
      "content not found",
      "page is unavailable",
      "page could not be found"
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

   internal static string? FindSoftErrorSignature(
      string? title,
      string visibleText
   )
   {
      var normalizedTitle = WebPageContentFetchSupport.NormalizeText(title);
      var normalizedText = WebPageContentFetchSupport.NormalizeText(
         visibleText
      );

      var titleSignature = StrongSoftErrorSignatures
         .Concat(ShortPageSoftErrorSignatures)
         .FirstOrDefault(signature =>
            normalizedTitle.Contains(
               signature,
               StringComparison.OrdinalIgnoreCase
            ));

      if(titleSignature is not null)
      {
         return titleSignature;
      }

      var strongSignature = StrongSoftErrorSignatures.FirstOrDefault(
         signature => normalizedText.Contains(
            signature,
            StringComparison.OrdinalIgnoreCase
         )
      );

      if(strongSignature is not null)
      {
         return strongSignature;
      }

      if(normalizedText.Length >
         WebPageFetchDefaults.SoftErrorMaximumTextCharacters)
      {
         return null;
      }

      return ShortPageSoftErrorSignatures.FirstOrDefault(signature =>
         normalizedText.Contains(
            signature,
            StringComparison.OrdinalIgnoreCase
         ));
   }
}
