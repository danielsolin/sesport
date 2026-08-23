using Microsoft.AspNetCore.Http;

using SESport.Web.Pages.Admin.Broadcasts;

namespace SESport.Core.Tests.Pages.Admin.Broadcasts;

public sealed class BroadcastRowViewModelTests
{
   [Theory]
   [InlineData("/Admin/Ajax/Update/BroadcastField")]
   [InlineData("/Admin/Ajax/List/Broadcast")]
   [InlineData("/Admin/Ajax")]
   public void AjaxRequestsDoNotBecomeActivityReturnUrls(string path)
   {
      var context = new DefaultHttpContext();
      context.Request.Path = path;
      context.Request.QueryString = new QueryString("?date=2026-08-23");

      var returnUrl = BroadcastRowViewModel.GetActivityReturnUrl(
         context.Request
      );

      Assert.Null(returnUrl);
   }

   [Fact]
   public void FullPageRequestsKeepTheirActivityReturnUrl()
   {
      var context = new DefaultHttpContext();
      context.Request.Path = "/Admin/Broadcasts/Index";
      context.Request.QueryString = new QueryString(
         "?date=2026-08-23&sortColumn=Time"
      );

      var returnUrl = BroadcastRowViewModel.GetActivityReturnUrl(
         context.Request
      );

      Assert.Equal(
         "/Admin/Broadcasts/Index?date=2026-08-23&sortColumn=Time",
         returnUrl
      );
   }
}
