using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SESport.Web.Extensions;

internal static class PageModelResponseExtensions
{
   internal static bool WantsHtmlResponse(this PageModel pageModel) =>
      pageModel.PageContext?.HttpContext?.Request.Headers.Accept.ToString().Contains(
         "text/html",
         StringComparison.OrdinalIgnoreCase
      ) == true;
}
