using SESport.AI.Llama;

namespace SESport.AI.WebPages;

public static class WebPageToolSupport
{
   public static bool TryValidateUrl(
      string url,
      out Uri absoluteUrl,
      out string error
   )
   {
      return WebPageUrlPolicy.TryValidate(
         url,
         out absoluteUrl,
         out error
      );
   }

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

   public static IReadOnlyList<string> ExtractPrimaryCountryRows(
      WebPageContent pageContent
   )
   {
      var searchText = string.IsNullOrWhiteSpace(pageContent.MainTextFull)
         ? pageContent.MainText
         : pageContent.MainTextFull;

      return LlamaPageToolFormatter.ExtractMatchingRows(
         searchText,
         [
            PrimaryCountry.CountryName,
            PrimaryCountry.LocalDisplayName,
            PrimaryCountry.ThreeLetterCode
         ]
      );
   }
}
