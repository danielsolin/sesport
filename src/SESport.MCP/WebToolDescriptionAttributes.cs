using System.ComponentModel;
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
   private static string MaxResponseCharactersText =>
      WebPageFetchDefaults.MaxResponseCharacters.ToString(
         "N0",
         CultureInfo.InvariantCulture
      );

   public static string GetPage =>
      "Fetches a web page through SESport's existing web page " +
      "content pipeline and returns its text and page metadata. " +
      "The returned main text is limited to " +
      MaxResponseCharactersText +
      " characters. When more content exists, it ends with " +
      "[CUTOFF]; use web_find_in_page with the same URL to find text " +
      "beyond the cutoff.";

   public static string FindInPage =>
      "Searches the full fetched web page case-insensitively and returns " +
      "compact matching lines, including text beyond web_get_page's " +
      MaxResponseCharactersText +
      "-character limit. Use this after web_get_page when its main text " +
      "ends with [CUTOFF], or when you need to locate a specific passage " +
      "without retrieving the whole page.";
}
