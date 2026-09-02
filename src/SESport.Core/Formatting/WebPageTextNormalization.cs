using System.Text.RegularExpressions;

namespace SESport.Core.Formatting;

public static class WebPageTextNormalization
{
   private static readonly Regex GluedGolfClubRegex = new(
      @"(?<=[\p{Ll}])(?=[\p{Lu}][\p{L}'’&.\- ]*" +
      @"\s+(?:GC|G&CC|G&SC|GK|CC|Club|Links|Estate|Resort)\b)",
      RegexOptions.CultureInvariant | RegexOptions.Compiled
   );
   private static readonly Regex GluedDuplicateCellSuffixRegex = new(
      @"(?<=[\p{Ll}])(?<suffix>[\p{Lu}][\p{L}'’&.\- ]{2,})" +
      @"\s+\|\s+\k<suffix>\b",
      RegexOptions.CultureInvariant | RegexOptions.Compiled
   );
   private static readonly Regex AdjacentPipeCellDuplicateRegex = new(
      @"(?<prefix>^|\|\s*)(?<value>[^|\r\n]+?)\s+\|\s+\k<value>" +
      @"(?=\s*(?:\||$))",
      RegexOptions.CultureInvariant | RegexOptions.Compiled
   );
   private static readonly Regex PipeSeparatorWhitespaceRegex = new(
      @"[^\S\r\n]*\|[^\S\r\n]*",
      RegexOptions.CultureInvariant | RegexOptions.Compiled
   );

   public static string NormalizeGluedTableCellText(string text)
   {
      text = PipeSeparatorWhitespaceRegex.Replace(text, " | ");
      text = GluedGolfClubRegex.Replace(text, " | ");
      text = GluedDuplicateCellSuffixRegex.Replace(
         text,
         " | ${suffix} | ${suffix}"
      );

      return AdjacentPipeCellDuplicateRegex.Replace(
         text,
         "${prefix}${value}"
      );
   }
}
