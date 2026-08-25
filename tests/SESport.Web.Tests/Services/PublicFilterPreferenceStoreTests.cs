using Microsoft.AspNetCore.Http;

namespace SESport.Core.Tests.Services;

public sealed class PublicFilterPreferenceStoreTests
{
   [Theory]
   [InlineData(
      false,
      "/?date=2026-08-25&sport=football"
   )]
   [InlineData(
      true,
      "/bevakade?sport=football"
   )]
   public void SaveWritesTheCurrentPublicActivityUrl(
      bool watched,
      string expectedUrl
   )
   {
      var context = new DefaultHttpContext();

      PublicFilterPreferenceStore.Save(
         context.Response,
         new DateOnly(2026, 8, 25),
         "football",
         watched
      );

      var setCookie = Uri.UnescapeDataString(
         context.Response.Headers.SetCookie.ToString()
      );

      Assert.Contains(
         $"{PublicFilterPreferenceStore.CookieName}={expectedUrl}",
         setCookie
      );
   }

   [Fact]
   public void ReadPublicActivityUrlAcceptsTheWatchedRoute()
   {
      var context = new DefaultHttpContext();
      context.Request.Headers.Cookie =
         $"{PublicFilterPreferenceStore.CookieName}=" +
         "/bevakade?sport=football";

      var value = PublicFilterPreferenceStore.ReadPublicActivityUrl(
         context.Request
      );

      Assert.Equal("/bevakade?sport=football", value);
   }

   [Fact]
   public void ReadPublicActivityUrlRejectsExternalPaths()
   {
      var context = new DefaultHttpContext();
      context.Request.Headers.Cookie =
         $"{PublicFilterPreferenceStore.CookieName}=" +
         "https://example.com/bevakade";

      var value = PublicFilterPreferenceStore.ReadPublicActivityUrl(
         context.Request
      );

      Assert.Null(value);
   }
}
