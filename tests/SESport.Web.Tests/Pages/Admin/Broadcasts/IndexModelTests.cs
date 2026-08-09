using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

using SESport.Web.Pages.Admin.Broadcasts;

namespace SESport.Core.Tests.Pages.Admin.Broadcasts;

public sealed class IndexModelTests
{
   [Fact]
   public async Task OnGetAsyncRestoresFilterPreferencesFromCookies()
   {
      var model = CreateModel();
      model.PageContext = new PageContext
      {
         HttpContext = CreateContext(
            $"{IndexModel.SportFilterCookieName}=football|tennis; " +
            $"{IndexModel.ShowHiddenFilterCookieName}=true; " +
            $"{IndexModel.HideReplaysFilterCookieName}=true"
         )
      };

      await model.OnGetAsync(CancellationToken.None);

      Assert.Equal(["football", "tennis"], model.SelectedSports);
      Assert.True(model.ShowHidden);
      Assert.True(model.HideReplays);
   }

   [Fact]
   public async Task OnGetAsyncUsesExplicitFalseFilterOverCookie()
   {
      var model = CreateModel();
      model.ShowHidden = false;
      model.HideReplays = false;
      model.PageContext = new PageContext
      {
         HttpContext = CreateContext(
            $"{IndexModel.SportFilterCookieName}=football; " +
            $"{IndexModel.ShowHiddenFilterCookieName}=true; " +
            $"{IndexModel.HideReplaysFilterCookieName}=true"
         )
      };
      model.PageContext.HttpContext.Request.QueryString =
         new QueryString(
            "?SelectedSports=&showHidden=false&hideReplays=false"
         );

      await model.OnGetAsync(CancellationToken.None);

      Assert.Equal([string.Empty], model.SelectedSports);
      Assert.False(model.ShowHidden);
      Assert.False(model.HideReplays);
   }

   private static IndexModel CreateModel()
   {
      return new IndexModel(
         null!,
         null!,
         new BroadcastDatePreferenceStore(),
         new FilterPreferenceStore(),
         null!,
         null!
      );
   }

   private static DefaultHttpContext CreateContext(string cookieHeader)
   {
      var context = new DefaultHttpContext();
      context.Request.Headers["Cookie"] = cookieHeader;
      context.RequestServices = new ServiceCollection()
         .AddLogging()
         .BuildServiceProvider();
      return context;
   }
}
