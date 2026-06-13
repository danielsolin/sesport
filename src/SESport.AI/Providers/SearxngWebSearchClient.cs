using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SESport.AI.Providers;

public sealed class SearxngWebSearchClient : IWebSearchClient
{
   private static readonly Uri DefaultBaseAddress = new(
      "https://xng.sesport.se/"
   );

   private static readonly string[] DeniedHostSuffixes =
   [
      "instagram.com",
      "www.instagram.com",
      "facebook.com",
      "www.facebook.com",
      "x.com",
      "www.x.com",
      "twitter.com",
      "www.twitter.com",
      "tiktok.com",
      "www.tiktok.com",
      "youtube.com",
      "www.youtube.com",
      "youtu.be",
      "www.youtu.be",
      "threads.net",
      "www.threads.net"
   ];

   private static readonly Uri SearchUri = new(
      DefaultBaseAddress,
      "search"
   );

   public SearxngWebSearchClient(
      HttpClient httpClient,
      SearxngWebSearchClientOptions options
   )
   {
      HttpClient = httpClient;

      var basicAuth = CreateBasicAuthHeader(options);

      if(!string.IsNullOrWhiteSpace(basicAuth))
      {
         HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", basicAuth);
      }
   }

   private HttpClient HttpClient { get; }

   public async Task<IReadOnlyList<WebSearchResult>> SearchAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(query))
      {
         return [];
      }

      using var request = new HttpRequestMessage(HttpMethod.Post, SearchUri)
      {
         Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
               ["q"] = query,
               ["format"] = "json",
               ["language"] = "sv",
               ["categories"] = "general"
            }
         )
      };

      request.Headers.Accept.ParseAdd("application/json");

      using var response = await HttpClient.SendAsync(
         request,
         cancellationToken
      );
      var rawResponse = await response.Content.ReadAsStringAsync(
         cancellationToken
      );

      if(!response.IsSuccessStatusCode)
      {
         throw new HttpRequestException(
            CreateFailureMessage(response.StatusCode, rawResponse),
            null,
            response.StatusCode
         );
      }

      return ParseResults(rawResponse, maxResults);
   }

   private static IReadOnlyList<WebSearchResult> ParseResults(
      string rawResponse,
      int maxResults
   )
   {
      using var document = JsonDocument.Parse(rawResponse);
      var root = document.RootElement;

      if(!root.TryGetProperty("results", out var results))
      {
         return [];
      }

      var items = new List<WebSearchResult>();

      foreach(var result in results.EnumerateArray())
      {
         var title = ReadString(result, "title");
         var url = ReadString(result, "url");
         var snippet = ReadString(result, "content");

         if(string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(url))
         {
            continue;
         }

         if(IsDeniedDomain(url))
         {
            continue;
         }

         items.Add(
            new WebSearchResult(
               title,
               NormalizeUrl(url),
               string.IsNullOrWhiteSpace(snippet) ? null : snippet
            )
         );

         if(items.Count >= Math.Clamp(maxResults, 1, 20))
         {
            break;
         }
      }

      return items;
   }

   private static bool IsDeniedDomain(string url)
   {
      if(!Uri.TryCreate(url, UriKind.Absolute, out var uri))
      {
         return false;
      }

      var host = uri.Host;

      foreach(var deniedHostSuffix in DeniedHostSuffixes)
      {
         if(host.EndsWith(
            deniedHostSuffix,
            StringComparison.OrdinalIgnoreCase
         ))
         {
            return true;
         }
      }

      return false;
   }

   private static string ReadString(
      JsonElement element,
      string propertyName
   )
   {
      if(!element.TryGetProperty(propertyName, out var property))
      {
         return string.Empty;
      }

      return property.ValueKind == JsonValueKind.String
         ? property.GetString() ?? string.Empty
         : string.Empty;
   }

   private static string NormalizeUrl(string url)
   {
      if(Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
      {
         return absoluteUri.ToString();
      }

      return new Uri(DefaultBaseAddress, url).ToString();
   }

   private static string? CreateBasicAuthHeader(
      SearxngWebSearchClientOptions options
   )
   {
      if(string.IsNullOrWhiteSpace(options.BasicAuthUsername) ||
         string.IsNullOrWhiteSpace(options.BasicAuthPassword))
      {
         return null;
      }

      var credentials =
         $"{options.BasicAuthUsername}:" +
         $"{options.BasicAuthPassword}";
      var bytes = Encoding.UTF8.GetBytes(credentials);

      return Convert.ToBase64String(bytes);
   }

   private static string CreateFailureMessage(
      System.Net.HttpStatusCode statusCode,
      string rawResponse
   )
   {
      var preview = rawResponse.ReplaceLineEndings(" ").Trim();

      if(preview.Length > 500)
      {
         preview = preview[..500] + "...";
      }

      return $"searxng search failed with {(int)statusCode} {statusCode}: " +
         preview;
   }
}
