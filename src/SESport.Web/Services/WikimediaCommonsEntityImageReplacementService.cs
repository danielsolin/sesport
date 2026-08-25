using SESport.Core.Sources;
using SESport.Data.Models;
using System.Security.Cryptography;

namespace SESport.Web.Services;

internal sealed class WikimediaCommonsEntityImageReplacementService(
   AdminRepository repository,
   WikimediaCommonsImageClient client
) : IEntityImageReplacementService
{
   public async Task ReplaceAsync(
      Guid entityId,
      WikimediaCommonsImageReference source,
      CancellationToken cancellationToken
   )
   {
      var image = await client.FetchAsync(source, cancellationToken);
      var attribution = string.Join(
         ", ",
         new[] { image.CreatorName, image.LicenseName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
      );
      var replacement = new EntityImageReplacement(
         image.Source.Url,
         $"{image.PageId}:{image.Source.RevisionId}:width-" +
            image.Image.PixelWidth,
         image.Image.Data,
         image.Image.MimeType,
         image.Image.PixelWidth,
         image.Image.PixelHeight,
         Convert.ToHexString(
            SHA256.HashData(image.Image.Data)
         ).ToLowerInvariant(),
         image.Thumbnail.Data,
         image.Thumbnail.MimeType,
         image.Thumbnail.PixelWidth,
         image.Thumbnail.PixelHeight,
         Convert.ToHexString(
            SHA256.HashData(image.Thumbnail.Data)
         ).ToLowerInvariant(),
         image.Thumbnail.MediaUrl.ToString(),
         image.Image.MediaUrl.ToString(),
         image.SourceTitle,
         image.CreatorName,
         image.CreatorUrl,
         image.LicenseName,
         image.LicenseUrl,
         image.CopyrightNotice,
         attribution,
         "Downloaded Wikimedia Commons thumbnails; no local " +
            "image processing was applied.",
         "Replaced by an administrator from a Wikimedia Commons " +
            "file revision URL."
      );

      await repository.ReplacePrimaryEntityImageAsync(
         entityId,
         replacement,
         cancellationToken
      );
   }
}
