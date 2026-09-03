namespace SESport.Web.Routing;

public static class PublicRoutePaths
{
   public const string Home = "/";
   public const string Watched = "/bevakningar";
   public const string Settings = "/installningar";
   public const string Statistics = "/statistik";

   public static string BuildAbsoluteUrl(
      string canonicalHomeUrl,
      string path
   )
   {
      var baseUrl = canonicalHomeUrl.EndsWith(
         "/",
         StringComparison.Ordinal
      )
         ? canonicalHomeUrl
         : canonicalHomeUrl + "/";
      return new Uri(
         new Uri(baseUrl, UriKind.Absolute),
         path.TrimStart('/')
      ).ToString();
   }
}
