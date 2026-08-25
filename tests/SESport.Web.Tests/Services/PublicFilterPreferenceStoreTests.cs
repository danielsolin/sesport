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
   public void SaveWritesTheScopedPublicActivityUrl(
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
      var expectedCookieName = watched
         ? PublicFilterPreferenceStore.WatchedCookieName
         : PublicFilterPreferenceStore.ScheduleCookieName;

      Assert.Contains(
         $"{expectedCookieName}={expectedUrl}",
         setCookie
      );
   }

   [Fact]
   public void SaveStatisticsWritesTheStatisticsUrl()
   {
      var context = new DefaultHttpContext();

      PublicFilterPreferenceStore.SaveStatistics(
         context.Response,
         "2026-08",
         "football"
      );

      var setCookie = Uri.UnescapeDataString(
         context.Response.Headers.SetCookie.ToString()
      );

      Assert.Contains(
         $"{PublicFilterPreferenceStore.StatisticsCookieName}=" +
            "/statistik?month=2026-08&sport=football",
         setCookie
      );
   }

   [Fact]
   public void ReadScheduleUrlAcceptsTheScheduleRoute()
   {
      var context = new DefaultHttpContext();
      context.Request.Headers.Cookie =
         $"{PublicFilterPreferenceStore.ScheduleCookieName}=" +
         "/?date=2026-08-25&sport=football";

      var value = PublicFilterPreferenceStore.ReadScheduleUrl(
         context.Request
      );

      Assert.Equal(
         "/?date=2026-08-25&sport=football",
         value
      );
   }

   [Fact]
   public void ReadWatchedUrlAcceptsTheWatchedRoute()
   {
      var context = new DefaultHttpContext();
      context.Request.Headers.Cookie =
         $"{PublicFilterPreferenceStore.WatchedCookieName}=" +
         "/bevakade?sport=football";

      var value = PublicFilterPreferenceStore.ReadWatchedUrl(
         context.Request
      );

      Assert.Equal("/bevakade?sport=football", value);
   }

   [Fact]
   public void ReadStatisticsUrlAcceptsTheStatisticsRoute()
   {
      var context = new DefaultHttpContext();
      context.Request.Headers.Cookie =
         $"{PublicFilterPreferenceStore.StatisticsCookieName}=" +
         "/statistik?month=2026-08&sport=football";

      var value = PublicFilterPreferenceStore.ReadStatisticsUrl(
         context.Request
      );

      Assert.Equal(
         "/statistik?month=2026-08&sport=football",
         value
      );
   }

   [Fact]
   public void ReadScheduleUrlRejectsTheWatchedRoute()
   {
      var context = new DefaultHttpContext();
      context.Request.Headers.Cookie =
         $"{PublicFilterPreferenceStore.ScheduleCookieName}=" +
         "/bevakade?sport=football";

      var value = PublicFilterPreferenceStore.ReadScheduleUrl(
         context.Request
      );

      Assert.Null(value);
   }

   [Fact]
   public void ReadWatchedUrlRejectsExternalPaths()
   {
      var context = new DefaultHttpContext();
      context.Request.Headers.Cookie =
         $"{PublicFilterPreferenceStore.WatchedCookieName}=" +
         "https://example.com/bevakade";

      var value = PublicFilterPreferenceStore.ReadWatchedUrl(
         context.Request
      );

      Assert.Null(value);
   }
}
