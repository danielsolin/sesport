namespace SESport.Data.Models;

public sealed record EntityImageReplacement(
   string SourceUrl,
   string SourceAssetId,
   byte[] ImageData,
   string MimeType,
   int PixelWidth,
   int PixelHeight,
   string ContentSha256,
   byte[] ThumbnailData,
   string ThumbnailMimeType,
   int ThumbnailPixelWidth,
   int ThumbnailPixelHeight,
   string ThumbnailContentSha256,
   string ThumbnailSourceMediaUrl,
   string SourceMediaUrl,
   string SourceTitle,
   string? CreatorName,
   string? CreatorUrl,
   string LicenseName,
   string? LicenseUrl,
   string? CopyrightNotice,
   string AttributionText,
   string ModificationDescription,
   string ReviewNote
);
