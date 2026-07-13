using Microsoft.AspNetCore.Http;

using SESport.Web.Services;

namespace SESport.Core.Tests.Services;

public sealed class DatePreferenceStoreTests
{
   [Fact]
   public void AdminAndRunStoresUseDifferentCookies()
   {
      var context = CreateContext(
         "sesport.admin.date=2026-06-18; sesport.admin.runs.date=2026-06-17"
      );

      var adminDate = new AdminDatePreferenceStore()
         .ResolveDate(context, null);
      var runDate = new RunDatePreferenceStore().ResolveDate(context, null);

      Assert.Equal(new DateOnly(2026, 6, 18), adminDate);
      Assert.Equal(new DateOnly(2026, 6, 17), runDate);
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
