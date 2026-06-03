namespace SESport.Web.Formatting
{
   public static class UrlDisplayFormatter
   {
      public static string ToShortDisplayUrl(string? uri)
      {
         var shortUrl = uri ?? string.Empty;

         shortUrl = shortUrl.Replace("https://", "");
         shortUrl = shortUrl.Replace("http://", "");
         shortUrl = shortUrl.Replace("www.", "");

         if(shortUrl.Contains('/'))
            shortUrl = shortUrl[..shortUrl.IndexOf('/')];

         shortUrl += "↗";

         return shortUrl;
      }
   }
}
