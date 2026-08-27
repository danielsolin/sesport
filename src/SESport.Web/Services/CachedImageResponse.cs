using System.Security.Cryptography;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace SESport.Web.Services;

internal static class CachedImageResponse
{
   public static FileContentResult Create(
      HttpResponse response,
      byte[] data,
      string mimeType,
      TimeSpan cacheDuration
   )
   {
      var cacheMaxAge = (int)cacheDuration.TotalSeconds;
      response.Headers.CacheControl =
         $"private, max-age={cacheMaxAge}";
      var entityTag = new EntityTagHeaderValue(
         "\"" + Convert.ToHexString(
            SHA256.HashData(data)
         ).ToLowerInvariant() + "\""
      );

      return new FileContentResult(data, mimeType)
      {
         EntityTag = entityTag
      };
   }
}
