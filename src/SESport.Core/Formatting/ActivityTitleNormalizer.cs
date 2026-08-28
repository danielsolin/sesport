namespace SESport.Core.Formatting;

public static class ActivityTitleNormalizer
{
   public static string NormalizeForGrouping(string title)
   {
      return string.Join(
         ' ',
         title.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries |
               StringSplitOptions.TrimEntries
         )
      ).ToUpperInvariant();
   }
}
