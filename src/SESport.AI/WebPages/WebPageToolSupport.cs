using SESport.AI.Llama;

namespace SESport.AI.WebPages;

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
         LlamaPageToolFormatter.ExtractMatchingCountryEntries(
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

      return LlamaPageToolFormatter.FormatFindMatchesForTool(
         LlamaPageToolFormatter.FindPageMatches(pageContent, find)
      );
   }
}
