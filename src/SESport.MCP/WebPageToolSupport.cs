namespace SESport.MCP;

public static class WebPageToolSupport
{
   public static string FindInPage(
      WebPageContent pageContent,
      string find
   )
   {
      var searchText = string.IsNullOrWhiteSpace(pageContent.MainTextFull)
         ? pageContent.MainText
         : pageContent.MainTextFull;
      var matchingCountryEntries =
         WebPageFindSupport.ExtractMatchingCountryEntries(
            searchText,
            find
         );

      if(matchingCountryEntries.Count > 0)
      {
         return string.Join(
            Environment.NewLine,
            matchingCountryEntries
         );
      }

      return WebPageFindSupport.FormatFindMatchesForTool(
         WebPageFindSupport.FindPageMatches(pageContent, find)
      );
   }
}
