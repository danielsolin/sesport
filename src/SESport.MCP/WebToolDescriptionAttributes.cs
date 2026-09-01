using System.Globalization;

using SESport.Core.Configuration;

namespace SESport.MCP;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class WebGetPageDescriptionAttribute : DescriptionAttribute
{
   public WebGetPageDescriptionAttribute()
      : base(WebToolDescriptions.GetPage)
   {
   }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class WebFindInPageDescriptionAttribute : DescriptionAttribute
{
   public WebFindInPageDescriptionAttribute()
      : base(WebToolDescriptions.FindInPage)
   {
   }
}

internal static class WebToolDescriptions
{
   private static string FetchDurationNotice =>
      "Fetching can take tens of seconds, especially for JavaScript-heavy " +
      "pages. The server stops trying after about " +
      TotalFetchTimeoutText +
      " seconds and returns a " +
      "timeout result. " +
      "The pipeline may load the page in a browser, wait for network " +
      "activity, " +
      "scroll lazy-loaded content, and wait for the content to stabilize. " +
      "Keep the call open while it completes instead of canceling just " +
      "because " +
      "no result has arrived yet.";

   private static string MaxResponseCharactersText =>
      WebPageFetchDefaults.MaxResponseCharacters.ToString(
         "N0",
         CultureInfo.InvariantCulture
      );

   private static string TotalFetchTimeoutText =>
      WebPageFetchDefaults.TotalFetchTimeout.TotalSeconds.ToString(
         "N0",
         CultureInfo.InvariantCulture
      );

   public static string GetPage =>
      "Fetches an HTML page or PDF through SESport's existing web page " +
      "content pipeline and returns its text, headings, and metadata. " +
      FetchDurationNotice + " " +
      "HTML pages are rendered in a browser when the initial response is " +
      "not sufficient. " +
      "The returned main text is limited to " +
      MaxResponseCharactersText +
      " characters. When more content exists, it ends with " +
      "[CUTOFF]; use web_find_in_page with the same URL to find text " +
      "beyond the cutoff. RenderWarning indicates likely placeholder " +
      "content such as TBD or Loading.";

   public static string FindInPage =>
      "Searches the full fetched web page case-insensitively and returns " +
      "compact matching lines, including text beyond web_get_page's " +
      MaxResponseCharactersText +
      "-character limit. Use this after web_get_page when its main text " +
      "ends with [CUTOFF], or when you need to locate a specific passage " +
      "without retrieving the whole page. " +
      FetchDurationNotice;
}
