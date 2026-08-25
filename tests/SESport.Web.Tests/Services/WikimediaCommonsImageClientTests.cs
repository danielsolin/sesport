using SESport.Core.Sources;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SESport.Web.Tests.Services;

public sealed class WikimediaCommonsImageClientTests
{
   [Fact]
   public async Task FetchAsyncUsesRevisionAndSourceProvidedThumbnails()
   {
      var handler = new FakeWikimediaHandler();
      using var httpClient = new HttpClient(handler);
      var client = new WikimediaCommonsImageClient(httpClient);
      var isValid = WikimediaCommonsSourceUrl.TryParse(
         "https://commons.wikimedia.org/w/index.php?" +
         "title=File:Example.jpg&oldid=42",
         out var source
      );

      Assert.True(isValid);
      var result = await client.FetchAsync(source, CancellationToken.None);

      Assert.Equal(
         "https://commons.wikimedia.org/w/index.php?" +
         "title=File:Example.jpg&oldid=42",
         result.Source.Url
      );
      Assert.Equal(42, result.Source.RevisionId);
      Assert.Equal(123, result.PageId);
      Assert.Equal("File:Example.jpg", result.SourceTitle);
      Assert.Equal("Example Creator", result.CreatorName);
      Assert.Equal(
         "https://commons.wikimedia.org/wiki/User:Example",
         result.CreatorUrl
      );
      Assert.Equal("CC BY-SA 4.0", result.LicenseName);
      Assert.Equal(new byte[] { 1, 2, 3 }, result.Image.Data);
      Assert.Equal(500, result.Image.PixelWidth);
      Assert.Equal(333, result.Image.PixelHeight);
      Assert.Equal(new byte[] { 4, 5 }, result.Thumbnail.Data);
      Assert.Equal(72, result.Thumbnail.PixelWidth);
      Assert.Equal(48, result.Thumbnail.PixelHeight);
      Assert.Contains(
         handler.Requests,
         request => request.Contains("iiurlwidth=500")
      );
      Assert.Contains(
         handler.Requests,
         request => request.Contains("iiurlwidth=72")
      );
   }

   [Fact]
   public async Task FetchAsyncResolvesCurrentFilePageRevision()
   {
      var handler = new FakeWikimediaHandler();
      using var httpClient = new HttpClient(handler);
      var client = new WikimediaCommonsImageClient(httpClient);
      var isValid = WikimediaCommonsSourceUrl.TryParse(
         "https://commons.wikimedia.org/wiki/File:Example.jpg",
         out var source
      );

      Assert.True(isValid);
      Assert.Equal(0, source.RevisionId);

      var result = await client.FetchAsync(source, CancellationToken.None);

      Assert.Equal(
         "https://commons.wikimedia.org/w/index.php?" +
         "title=File:Example.jpg&oldid=42",
         result.Source.Url
      );
      Assert.Equal(42, result.Source.RevisionId);
      Assert.Contains(
         handler.Requests,
         request => request.Contains("titles=File%3AExample.jpg")
      );
      Assert.Contains(
         handler.Requests,
         request => request.Contains("revids=42")
      );
   }

   private sealed class FakeWikimediaHandler : HttpMessageHandler
   {
      public List<string> Requests { get; } = [];

      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         var requestUrl = request.RequestUri!.ToString();
         Requests.Add(requestUrl);

         if(requestUrl.Contains(
               "commons.wikimedia.org/w/api.php",
               StringComparison.Ordinal
            ))
         {
            var width = requestUrl.Contains(
               "iiurlwidth=72",
               StringComparison.Ordinal
            )
               ? 72
               : 500;
            return Task.FromResult(
               CreateResponse(
                  BuildApiResponse(width),
                  "application/json"
               )
            );
         }

         var data = requestUrl.EndsWith(
            "/72.jpg",
            StringComparison.Ordinal
         )
            ? new byte[] { 4, 5 }
            : new byte[] { 1, 2, 3 };
         return Task.FromResult(CreateResponse(data, "image/jpeg"));
      }

      private static HttpResponseMessage CreateResponse(
         string content,
         string mediaType
      )
      {
         return CreateResponse(
            System.Text.Encoding.UTF8.GetBytes(content),
            mediaType
         );
      }

      private static HttpResponseMessage CreateResponse(
         byte[] content,
         string mediaType
      )
      {
         var response = new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = new ByteArrayContent(content)
         };
         response.Content.Headers.ContentType =
            new MediaTypeHeaderValue(mediaType);
         return response;
      }

      private static string BuildApiResponse(int width)
      {
         var height = width == 72 ? 48 : 333;
         var mediaUrl =
            $"https://upload.test/{width.ToString()}.jpg";
         var imageInfo = new Dictionary<string, object?>
         {
            ["thumburl"] = mediaUrl,
            ["thumbwidth"] = width,
            ["thumbheight"] = height,
            ["thumbmime"] = "image/jpeg",
            ["mime"] = "image/jpeg"
         };
         if(width == 500)
         {
            imageInfo["extmetadata"] = new Dictionary<string, object?>
            {
               ["Artist"] = new
               {
                  value =
                     "<a href=\"https://commons.wikimedia.org/wiki/" +
                     "User:Example\">Example Creator</a>"
               },
               ["LicenseShortName"] = new
               {
                  value = "CC BY-SA 4.0"
               },
               ["LicenseUrl"] = new
               {
                  value =
                     "https://creativecommons.org/licenses/by-sa/4.0/"
               }
            };
         }

         return JsonSerializer.Serialize(
            new
            {
               query = new
               {
                  pages = new[]
                  {
                     new
                     {
                        pageid = 123L,
                        title = "File:Example.jpg",
                        revisions = new[] { new { revid = 42L } },
                        imageinfo = new[] { imageInfo }
                     }
                  }
               }
            }
         );
      }
   }
}
