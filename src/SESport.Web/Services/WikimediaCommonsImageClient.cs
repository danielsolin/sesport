using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

using SESport.Core.Sources;

namespace SESport.Web.Services;

internal sealed class WikimediaCommonsImageClient(HttpClient httpClient)
{
   private static readonly HashSet<string> SupportedMimeTypes =
   [
      "image/jpeg",
      "image/png",
      "image/webp",
      "image/gif"
   ];

   public async Task<WikimediaCommonsImageData> FetchAsync(
      WikimediaCommonsImageReference source,
      CancellationToken cancellationToken
   )
   {
      try
      {
         return await FetchCoreAsync(
            source,
            cancellationToken
         );
      }
      catch(EntityImageReplacementException)
      {
         throw;
      }
      catch(HttpRequestException exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         throw new EntityImageReplacementException(
            "Could not retrieve the image from Wikimedia Commons.",
            exception
         );
      }
      catch(JsonException exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         throw new EntityImageReplacementException(
            "Wikimedia Commons returned an unreadable response.",
            exception
         );
      }
      catch(OperationCanceledException exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         throw new EntityImageReplacementException(
            "The Wikimedia Commons request timed out.",
            exception
         );
      }
   }

   private async Task<WikimediaCommonsImageData> FetchCoreAsync(
      WikimediaCommonsImageReference source,
      CancellationToken cancellationToken
   )
   {
      var resolvedSource = source.RevisionId > 0
         ? source
         : await ResolveCurrentRevisionAsync(
            source,
            cancellationToken
         );
      var imageInfo = await GetImageInfoAsync(
         resolvedSource,
         WikimediaImageDefaults.MainImageWidth,
         includeMetadata: true,
         cancellationToken
      );
      var thumbnailInfo = await GetImageInfoAsync(
         resolvedSource,
         WikimediaImageDefaults.ListThumbnailWidth,
         includeMetadata: false,
         cancellationToken
      );
      var image = await DownloadAsync(
         imageInfo,
         WikimediaImageDefaults.MaximumImageBytes,
         cancellationToken
      );
      var thumbnail = await DownloadAsync(
         thumbnailInfo,
         WikimediaImageDefaults.MaximumThumbnailBytes,
         cancellationToken
      );

      return new WikimediaCommonsImageData(
         resolvedSource,
         imageInfo.PageId,
         imageInfo.SourceTitle,
         imageInfo.CreatorName,
         imageInfo.CreatorUrl,
         imageInfo.LicenseName!,
         imageInfo.LicenseUrl,
         imageInfo.CopyrightNotice,
         image,
         thumbnail
      );
   }

   private async Task<WikimediaCommonsImageReference>
      ResolveCurrentRevisionAsync(
         WikimediaCommonsImageReference source,
         CancellationToken cancellationToken
      )
   {
      var parameters = new Dictionary<string, string>
      {
         ["action"] = "query",
         ["titles"] = source.FileTitle,
         ["prop"] = "revisions",
         ["rvprop"] = "ids",
         ["format"] = "json",
         ["formatversion"] = "2"
      };
      var responseBody = await GetJsonWithRetryAsync(
         BuildApiUri(parameters),
         cancellationToken
      );

      using var document = JsonDocument.Parse(responseBody);
      var page = GetFirstPage(document.RootElement);
      if(page is null ||
         !page.Value.TryGetProperty(
            "revisions",
            out var revisions
         ) ||
         revisions.ValueKind != JsonValueKind.Array ||
         revisions.GetArrayLength() == 0 ||
         !revisions[0].TryGetProperty(
            "revid",
            out var revisionIdElement
         ) ||
         !revisionIdElement.TryGetInt64(out var revisionId) ||
         revisionId <= 0)
      {
         throw new EntityImageReplacementException(
            "The Wikimedia Commons file page was not found."
         );
      }

      return WikimediaCommonsSourceUrl.WithRevision(
         source,
         revisionId
      );
   }

   private async Task<WikimediaImageInfo> GetImageInfoAsync(
      WikimediaCommonsImageReference source,
      int thumbnailWidth,
      bool includeMetadata,
      CancellationToken cancellationToken
   )
   {
      var parameters = new Dictionary<string, string>
      {
         ["action"] = "query",
         ["revids"] = source.RevisionId.ToString(
            CultureInfo.InvariantCulture
         ),
         ["prop"] = "imageinfo|revisions",
         ["rvprop"] = "ids",
         ["iiprop"] = includeMetadata
            ? "url|size|mime|extmetadata"
            : "url|size|mime",
         ["iiurlwidth"] = thumbnailWidth.ToString(
            CultureInfo.InvariantCulture
         ),
         ["format"] = "json",
         ["formatversion"] = "2"
      };
      if(includeMetadata)
      {
         parameters["iiextmetadatalanguage"] = "en";
      }

      var responseBody = await GetJsonWithRetryAsync(
         BuildApiUri(parameters),
         cancellationToken
      );

      using var document = JsonDocument.Parse(responseBody);
      var page = GetFirstPage(document.RootElement);
      if(page is null ||
         !page.Value.TryGetProperty(
            "revisions",
            out var revisions
         ) ||
         revisions.ValueKind != JsonValueKind.Array ||
         revisions.GetArrayLength() == 0 ||
         !revisions[0].TryGetProperty(
            "revid",
            out var revisionIdElement
         ) ||
         !revisionIdElement.TryGetInt64(out var revisionId) ||
         revisionId != source.RevisionId)
      {
         throw new EntityImageReplacementException(
            "The Wikimedia Commons file revision was not found."
         );
      }

      if(!page.Value.TryGetProperty(
            "imageinfo",
            out var imageInfoValues
         ) ||
         imageInfoValues.ValueKind != JsonValueKind.Array ||
         imageInfoValues.GetArrayLength() == 0)
      {
         throw new EntityImageReplacementException(
            "The Wikimedia Commons revision is not an image."
         );
      }

      var imageInfo = imageInfoValues[0];
      var pageId = page.Value.TryGetProperty(
            "pageid",
            out var pageIdElement
         ) && pageIdElement.TryGetInt64(out var parsedPageId)
         ? parsedPageId
         : 0;
      if(pageId <= 0)
      {
         throw new EntityImageReplacementException(
            "The Wikimedia Commons file page was not found."
         );
      }

      var mediaUrl = ReadUri(imageInfo, "thumburl");
      var mimeType = NormalizeMimeType(
         ReadString(imageInfo, "thumbmime") ??
         ReadString(imageInfo, "mime")
      );
      var width = ReadInt32(imageInfo, "thumbwidth");
      var height = ReadInt32(imageInfo, "thumbheight");
      if(mediaUrl is null ||
         mimeType is null ||
         width is null ||
         height is null ||
         !SupportedMimeTypes.Contains(mimeType))
      {
         throw new EntityImageReplacementException(
            "The Wikimedia Commons revision has no supported image " +
            "thumbnail."
         );
      }

      var metadata = imageInfo.TryGetProperty(
         "extmetadata",
         out var metadataElement
      )
         ? metadataElement
         : default;
      var licenseName = ReadMetadataValue(
         metadata,
         "LicenseShortName"
      );
      var licenseUrl = ReadMetadataValue(metadata, "LicenseUrl");
      if(includeMetadata &&
         (string.IsNullOrWhiteSpace(licenseName) ||
            !IsFreeLicense(licenseName, licenseUrl)))
      {
         throw new EntityImageReplacementException(
            "The Wikimedia Commons revision does not expose a supported " +
            "free license."
         );
      }

      var artistHtml = ReadMetadataValue(metadata, "Artist");
      return new WikimediaImageInfo(
         pageId,
         mediaUrl,
         mimeType,
         width.Value,
         height.Value,
         ReadString(page.Value, "title") ?? source.FileTitle,
         StripHtml(artistHtml),
         ExtractFirstUrl(artistHtml),
         licenseName,
         licenseUrl,
         StripHtml(
            ReadMetadataValue(metadata, "Copyrighted")
         )
      );
   }

   private async Task<DownloadedWikimediaImage> DownloadAsync(
      WikimediaImageInfo imageInfo,
      int maximumBytes,
      CancellationToken cancellationToken
   )
   {
      using var response = await SendWithRetryAsync(
         () => httpClient.GetAsync(
            imageInfo.MediaUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
         ),
         cancellationToken
      );
      if(!response.IsSuccessStatusCode)
      {
         throw new EntityImageReplacementException(
            "Wikimedia Commons could not download the image."
         );
      }

      if(response.Content.Headers.ContentLength > maximumBytes)
      {
         throw new EntityImageReplacementException(
            "The Wikimedia Commons image is too large for this field."
         );
      }

      var responseMimeType = NormalizeMimeType(
         response.Content.Headers.ContentType?.MediaType
      );
      var mimeType = responseMimeType ?? imageInfo.MimeType;
      if(!SupportedMimeTypes.Contains(mimeType))
      {
         throw new EntityImageReplacementException(
            "Wikimedia Commons returned an unsupported image format."
         );
      }

      await using var responseStream = await response.Content
         .ReadAsStreamAsync(cancellationToken);
      using var imageStream = new MemoryStream();
      var buffer = new byte[81920];
      while(true)
      {
         var bytesRead = await responseStream.ReadAsync(
            buffer,
            cancellationToken
         );
         if(bytesRead == 0)
         {
            break;
         }

         if(imageStream.Length + bytesRead > maximumBytes)
         {
            throw new EntityImageReplacementException(
               "The Wikimedia Commons image is too large for this field."
            );
         }

         await imageStream.WriteAsync(
            buffer.AsMemory(0, bytesRead),
            cancellationToken
         );
      }

      var data = imageStream.ToArray();
      if(data.Length == 0)
      {
         throw new EntityImageReplacementException(
            "Wikimedia Commons returned an empty image."
         );
      }

      return new DownloadedWikimediaImage(
         data,
         mimeType,
         imageInfo.PixelWidth,
         imageInfo.PixelHeight,
         imageInfo.MediaUrl
      );
   }

   private async Task<string> GetJsonWithRetryAsync(
      Uri requestUri,
      CancellationToken cancellationToken
   )
   {
      for(var attempt = 0; ; attempt++)
      {
         using var response = await httpClient.GetAsync(
            requestUri,
            cancellationToken
         );
         var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken
         );

         if(!IsRateLimited(response, responseBody))
         {
            response.EnsureSuccessStatusCode();
            return responseBody;
         }

         await DelayBeforeRetryAsync(
            response,
            attempt,
            cancellationToken
         );
      }
   }

   private async Task<HttpResponseMessage> SendWithRetryAsync(
      Func<Task<HttpResponseMessage>> send,
      CancellationToken cancellationToken
   )
   {
      for(var attempt = 0; ; attempt++)
      {
         var response = await send();
         if(response.StatusCode != HttpStatusCode.TooManyRequests)
         {
            return response;
         }

         await DelayBeforeRetryAsync(
            response,
            attempt,
            cancellationToken
         );
      }
   }

   private static async Task DelayBeforeRetryAsync(
      HttpResponseMessage response,
      int attempt,
      CancellationToken cancellationToken
   )
   {
      if(attempt >= WikimediaImageDefaults.MaximumRetryAttempts)
      {
         response.Dispose();
         throw new EntityImageReplacementException(
            "Wikimedia Commons rate limiting persisted after retries."
         );
      }

      var delay = response.Headers.RetryAfter?.Delta;
      if(delay is null || delay <= TimeSpan.Zero)
      {
         var seconds = Math.Min(
            WikimediaImageDefaults.MaximumRetryDelaySeconds,
            WikimediaImageDefaults.RetryBackoffBaseSeconds *
               Math.Pow(2, attempt)
         );
         delay = TimeSpan.FromSeconds(seconds);
      }

      response.Dispose();
      await Task.Delay(delay.Value, cancellationToken);
   }

   private static bool IsRateLimited(
      HttpResponseMessage response,
      string responseBody
   )
   {
      if(response.StatusCode == HttpStatusCode.TooManyRequests)
      {
         return true;
      }

      try
      {
         using var document = JsonDocument.Parse(responseBody);
         if(!document.RootElement.TryGetProperty(
               "error",
               out var error
            ) || !error.TryGetProperty("code", out var code))
         {
            return false;
         }

         return code.ValueKind == JsonValueKind.String &&
            code.GetString() is "ratelimited" or "maxlag";
      }
      catch(JsonException)
      {
         return false;
      }
   }

   private static Uri BuildApiUri(
      IReadOnlyDictionary<string, string> parameters
   )
   {
      var query = string.Join(
         '&',
         parameters.Select(parameter =>
            Uri.EscapeDataString(parameter.Key) + "=" +
            Uri.EscapeDataString(parameter.Value))
      );
      return new Uri(WikimediaImageDefaults.ApiUri + "?" + query);
   }

   private static JsonElement? GetFirstPage(JsonElement root)
   {
      if(!root.TryGetProperty("query", out var query) ||
         !query.TryGetProperty("pages", out var pages) ||
         pages.ValueKind != JsonValueKind.Array ||
         pages.GetArrayLength() == 0)
      {
         return null;
      }

      return pages[0];
   }

   private static bool IsFreeLicense(
      string? licenseName,
      string? licenseUrl
   )
   {
      var normalizedName = licenseName?.Trim() ?? string.Empty;
      var normalizedUrl = licenseUrl?.Trim() ?? string.Empty;
      var combined = normalizedName + " " + normalizedUrl;

      if(combined.Contains(
            "non-commercial",
            StringComparison.OrdinalIgnoreCase
         ) ||
         combined.Contains(
            "no derivatives",
            StringComparison.OrdinalIgnoreCase
         ) ||
         combined.Contains("-nc", StringComparison.OrdinalIgnoreCase) ||
         combined.Contains("-nd", StringComparison.OrdinalIgnoreCase))
      {
         return false;
      }

      return normalizedName.StartsWith(
            "CC BY",
            StringComparison.OrdinalIgnoreCase
         ) ||
         normalizedName.StartsWith(
            "CC0",
            StringComparison.OrdinalIgnoreCase
         ) ||
         normalizedName.Contains(
            "public domain",
            StringComparison.OrdinalIgnoreCase
         ) ||
         normalizedName.StartsWith(
            "GFDL",
            StringComparison.OrdinalIgnoreCase
         ) ||
         normalizedUrl.Contains(
            "creativecommons.org/publicdomain",
            StringComparison.OrdinalIgnoreCase
         );
   }

   private static string? ReadString(
      JsonElement element,
      string propertyName
   )
   {
      return element.TryGetProperty(propertyName, out var property) &&
         property.ValueKind == JsonValueKind.String
         ? property.GetString()
         : null;
   }

   private static Uri? ReadUri(
      JsonElement element,
      string propertyName
   )
   {
      var value = ReadString(element, propertyName);
      return Uri.TryCreate(value, UriKind.Absolute, out var uri)
         ? uri
         : null;
   }

   private static int? ReadInt32(
      JsonElement element,
      string propertyName
   )
   {
      return element.TryGetProperty(propertyName, out var property) &&
         property.TryGetInt32(out var value)
         ? value
         : null;
   }

   private static string? ReadMetadataValue(
      JsonElement metadata,
      string propertyName
   )
   {
      if(metadata.ValueKind != JsonValueKind.Object ||
         !metadata.TryGetProperty(propertyName, out var property))
      {
         return null;
      }

      if(property.ValueKind == JsonValueKind.String)
      {
         return property.GetString();
      }

      return property.TryGetProperty("value", out var value) &&
         value.ValueKind == JsonValueKind.String
         ? value.GetString()
         : null;
   }

   private static string? StripHtml(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      var decoded = WebUtility.HtmlDecode(value);
      var withoutTags = Regex.Replace(
         decoded,
         "<[^>]+>",
         " ",
         RegexOptions.CultureInvariant
      );
      return string.Join(
         ' ',
         withoutTags.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries
         )
      );
   }

   private static string? ExtractFirstUrl(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      var match = Regex.Match(
         value,
         @"href=[""'](?<url>[^""']+)",
         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
      );
      if(!match.Success)
      {
         return null;
      }

      var url = WebUtility.HtmlDecode(match.Groups["url"].Value);
      if(url.StartsWith("//", StringComparison.Ordinal))
      {
         url = "https:" + url;
      }

      return Uri.TryCreate(url, UriKind.Absolute, out var uri)
         ? uri.ToString()
         : null;
   }

   private static string? NormalizeMimeType(string? mimeType)
   {
      return string.IsNullOrWhiteSpace(mimeType)
         ? null
         : mimeType.Split(';', 2)[0].Trim().ToLowerInvariant();
   }
}

internal sealed record WikimediaCommonsImageData(
   WikimediaCommonsImageReference Source,
   long PageId,
   string SourceTitle,
   string? CreatorName,
   string? CreatorUrl,
   string LicenseName,
   string? LicenseUrl,
   string? CopyrightNotice,
   DownloadedWikimediaImage Image,
   DownloadedWikimediaImage Thumbnail
);

internal sealed record WikimediaImageInfo(
   long PageId,
   Uri MediaUrl,
   string MimeType,
   int PixelWidth,
   int PixelHeight,
   string SourceTitle,
   string? CreatorName,
   string? CreatorUrl,
   string? LicenseName,
   string? LicenseUrl,
   string? CopyrightNotice
);

internal sealed record DownloadedWikimediaImage(
   byte[] Data,
   string MimeType,
   int PixelWidth,
   int PixelHeight,
   Uri MediaUrl
);
