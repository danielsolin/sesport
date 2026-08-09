using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using SESport.Web.Pages.Admin.Activities;

namespace SESport.Core.Tests.Pages.Admin.Activities;

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
            $"{IndexModel.StatusFilterCookieName}=Published"
         )
      };

      await model.OnGetAsync(CancellationToken.None);

      Assert.Equal("Published", model.Status);
      Assert.Equal(["football", "tennis"], model.SelectedSports);
   }

   [Fact]
   public async Task OnGetAsyncUsesExplicitFiltersOverCookies()
   {
      var model = CreateModel();
      model.Status = ActivityPublicationStatusIds.Draft;
      model.SelectedSports = ["basketball"];
      model.PageContext = new PageContext
      {
         HttpContext = CreateContext(
            $"{IndexModel.SportFilterCookieName}=football; " +
            $"{IndexModel.StatusFilterCookieName}=Published"
         )
      };
      model.PageContext.HttpContext.Request.QueryString =
         new QueryString("?SelectedSports=basketball&status=Draft");

      await model.OnGetAsync(CancellationToken.None);

      Assert.Equal(ActivityPublicationStatusIds.Draft, model.Status);
      Assert.Equal(["basketball"], model.SelectedSports);
   }

   private static IndexModel CreateModel()
   {
      return new IndexModel(
         null!,
         new ActivityIndexPageService(
            null!,
            new ActivityDatePreferenceStore(),
            NullLogger<ActivityIndexPageService>.Instance
         ),
         null!,
         new FilterPreferenceStore()
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
