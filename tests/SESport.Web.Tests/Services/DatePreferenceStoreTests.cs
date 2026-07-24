using Microsoft.AspNetCore.Http;

using SESport.Web.Services;

namespace SESport.Core.Tests.Services;

public sealed class DatePreferenceStoreTests
{
   [Fact]
   public void AdminPagesUseDifferentDateCookies()
   {
      var context = CreateContext(
         "sesport.admin.activities.date=2026-06-18; " +
         "sesport.admin.broadcasts.date=2026-06-19; " +
         "sesport.admin.runs.date=2026-06-17"
      );

      var activityDate = new ActivityDatePreferenceStore()
         .ResolveDate(context, null);
      var broadcastDate = new BroadcastDatePreferenceStore()
         .ResolveDate(context, null);
      var runDate = new RunDatePreferenceStore().ResolveDate(context, null);

      Assert.Equal(new DateOnly(2026, 6, 18), activityDate);
      Assert.Equal(new DateOnly(2026, 6, 19), broadcastDate);
      Assert.Equal(new DateOnly(2026, 6, 17), runDate);
   }

   [Theory]
   [InlineData(
      "sesport.admin.activities.date=2026-06-18",
      "sesport.admin.activities.date=2026-06-18"
   )]
   [InlineData(
      "sesport.admin.broadcasts.date=2026-06-19",
      "sesport.admin.broadcasts.date=2026-06-19"
   )]
   public void ActivityAndBroadcastStoresWriteOwnCookie(
      string cookieHeader,
      string expectedCookie
   )
   {
      var context = CreateContext(cookieHeader);

      if(cookieHeader.Contains(".activities.", StringComparison.Ordinal))
      {
         _ = new ActivityDatePreferenceStore().ResolveDate(context, null);
      }
      else
      {
         _ = new BroadcastDatePreferenceStore().ResolveDate(context, null);
      }

      Assert.Contains(
         expectedCookie,
         context.Response.Headers.SetCookie.ToString()
      );
   }

   [Fact]
   public void RunStoreWritesOwnCookieName()
   {
      var context = CreateContext("sesport.admin.runs.date=2026-06-17");

      _ = new RunDatePreferenceStore().ResolveDate(context, null);

      var setCookie = context.Response.Headers.SetCookie.ToString();

      Assert.Contains("sesport.admin.runs.date=2026-06-17", setCookie);
   }

   private static DefaultHttpContext CreateContext(string cookieHeader)
   {
      var context = new DefaultHttpContext();
      context.Request.Headers.Cookie = cookieHeader;
      return context;
   }
}
