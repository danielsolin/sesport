using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace SESport.Web.Infrastructure;

internal static class PageModelErrorExtensions
{
   internal const string UnexpectedErrorMessage =
      "An unexpected error occurred. Please try again.";

   internal static string LogUnexpectedError(
      this PageModel pageModel,
      Exception exception
   )
   {
      var loggerFactory = pageModel.HttpContext.RequestServices
         .GetService<ILoggerFactory>();
      var logger = loggerFactory?.CreateLogger(
         pageModel.GetType().FullName ?? pageModel.GetType().Name
      );

      logger?.LogError(
         exception,
         "Unexpected error while handling {PagePath}.",
         pageModel.HttpContext.Request.Path
      );

      return UnexpectedErrorMessage;
   }

   internal static JsonResult UnexpectedJsonError(
      this PageModel pageModel,
      Exception exception
   )
   {
      var message = pageModel.LogUnexpectedError(exception);

      return new JsonResult(new { error = message })
      {
         StatusCode = StatusCodes.Status500InternalServerError
      };
   }
}
