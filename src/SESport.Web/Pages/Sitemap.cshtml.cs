using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Domain;

namespace SESport.Web.Pages;

public sealed class SitemapModel(
   ActivityRepository repository,
   PublicSiteOptions publicSiteOptions,
   ILogger<SitemapModel> logger
) : PageModel
{
   public IReadOnlyList<string> Urls { get; private set; } = [];

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      Response.ContentType = "application/xml; charset=utf-8";

      var urls = new List<string>
      {
         publicSiteOptions.CanonicalHomeUrl,
         PublicRoutePaths.BuildAbsoluteUrl(
            publicSiteOptions.CanonicalHomeUrl,
            PublicRoutePaths.Statistics
         ),
         PublicRoutePaths.BuildAbsoluteUrl(
            publicSiteOptions.CanonicalHomeUrl,
            "/om"
         )
      };

      try
      {
         var sportToday = SportDay.Today(
            DateTimeOffset.UtcNow
         ).StartDate;
         var publishedActivities =
            await repository.GetPublishedForDateAsync(
               sportToday,
               cancellationToken
            );
         var publishedSportIds = publishedActivities
            .Select(activity => activity.SportId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

         urls.AddRange(
            PublicSportRoutes.All
               .Where(route => publishedSportIds.Contains(route.SportId))
               .Select(route => PublicRoutePaths.BuildAbsoluteUrl(
                  publicSiteOptions.CanonicalHomeUrl,
                  route.Path
               ))
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         logger.LogWarning(
            exception,
            "Could not add sport pages to the public sitemap."
         );
      }

      Urls = urls
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToArray();
   }
}
