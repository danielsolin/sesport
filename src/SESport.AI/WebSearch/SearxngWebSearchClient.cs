using System.Text.Json;

using Microsoft.Extensions.Logging;

using SESport.AI.Interfaces;
using SESport.Core.Configuration;

namespace SESport.AI.WebSearch;

public sealed class SearxngWebSearchClient : IWebSearchClient
{
   public SearxngWebSearchClient(
      HttpClient httpClient,
      SearxngWebSearchClientOptions options,
      SearchRateLimiter? rateLimiter = null,
      ILogger<SearxngWebSearchClient>? logger = null
   )
   {
      HttpClient = httpClient;
      Options = options;
      RateLimiter = rateLimiter ?? new SearchRateLimiter();
      Logger = logger;
      BaseAddress = BuildBaseAddress(options.BaseUrl);
      SearchUri = new Uri(BaseAddress, "search");
   }

   private HttpClient HttpClient { get; }

   private SearxngWebSearchClientOptions Options { get; }

   private SearchRateLimiter RateLimiter { get; }

   private ILogger<SearxngWebSearchClient>? Logger { get; }

   private Uri BaseAddress { get; }

   private Uri SearchUri { get; }

   public async Task<WebSearchResponse> SearchAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken,
      int searchAttempt = 0
   )
   {
      if(string.IsNullOrWhiteSpace(query))
      {
         return new WebSearchResponse([]);
      }

      return await SearchWithRetryAsync(
         query,
         maxResults,
         cancellationToken,
         searchAttempt
      );
   }

   private async Task<WebSearchResponse> SearchWithRetryAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken,
      int searchAttempt
   )
   {
      var engines = SearxngSearchEngineRotation.NormalizeEngines(
         Options.Engines
      );
      var retryAttempt = searchAttempt;

      while(true)
      {
         var engine = SearxngSearchEngineRotation.GetEngineForAttempt(
            engines,
            retryAttempt
         );

         await RateLimiter.WaitAsync(engine, cancellationToken);

         try
         {
            return await SearchOnceAsync(
               query,
               maxResults,
               cancellationToken,
               engine
            );
         }
         catch(OperationCanceledException)
            when(cancellationToken.IsCancellationRequested)
         {
            throw;
         }
         catch(Exception exception) when(IsRateLimitedFailure(exception))
         {
            RateLimiter.RegisterRateLimitedFailure(engine);
            LogRetryWait(
               engine,
               query,
               exception.Message,
               "rate-limited"
            );
         }
         catch(Exception exception) when(IsTransientFailure(exception))
         {
            RateLimiter.RegisterTransientFailure(engine);
            LogRetryWait(
               engine,
               query,
               exception.Message,
               "transient"
            );
         }

         retryAttempt++;
      }
   }

   private async Task<WebSearchResponse> SearchOnceAsync(
      string query,
      int maxResults,
      CancellationToken cancellationToken,
      string engine
   )
   {
      Logger?.LogDebug(
         "Sending SearXNG search request to {SearchUri} using {Engine}",
         SearchUri,
         engine
      );

      using var request = new HttpRequestMessage(HttpMethod.Post, SearchUri)
      {
         Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
               ["q"] = query,
               ["format"] = "json",
               ["categories"] = "general",
               ["engines"] = engine
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

      var results = ParseResults(rawResponse, maxResults);

      if(results.Count == 0 && ContainsRateLimitSignal(rawResponse))
      {
         throw new SearxngRateLimitedException(
            "SearXNG response reported rate limiting or CAPTCHA."
         );
      }

      return new WebSearchResponse(
         results,
         $"SearXNG/{engine}",
         $"engines={engine}"
      );
   }

   private static bool IsRateLimitedFailure(Exception exception)
   {
      if(exception is SearxngRateLimitedException)
      {
         return true;
      }

      if(exception is not HttpRequestException httpRequestException)
      {
         return false;
      }

      return httpRequestException.StatusCode is
         System.Net.HttpStatusCode.TooManyRequests or
         System.Net.HttpStatusCode.Forbidden;
   }

   private static bool IsTransientFailure(Exception exception)
   {
      return exception is HttpRequestException httpRequestException
         ? IsTransientStatusCode(httpRequestException.StatusCode)
         : exception is TaskCanceledException or TimeoutException;
   }

   private static bool IsTransientStatusCode(System.Net.HttpStatusCode? code)
   {
      return code is null ||
         code is System.Net.HttpStatusCode.RequestTimeout ||
         code is System.Net.HttpStatusCode.BadGateway ||
         code is System.Net.HttpStatusCode.ServiceUnavailable ||
         code is System.Net.HttpStatusCode.GatewayTimeout;
   }

   private void LogRetryWait(
      string engine,
      string query,
      string reason,
      string failureType
   )
   {
      Logger?.LogWarning(
         "SearXNG search failed for {Query} using {Engine} with " +
         "{FailureType}: {Reason}. Waiting before retrying.",
         query,
         engine,
         failureType,
         reason
      );
   }

   private static bool ContainsRateLimitSignal(string rawResponse)
   {
      return rawResponse.Contains(
            "captcha",
            StringComparison.OrdinalIgnoreCase
         ) ||
         rawResponse.Contains(
            "too many requests",
            StringComparison.OrdinalIgnoreCase
         ) ||
         rawResponse.Contains(
            "rate limit",
            StringComparison.OrdinalIgnoreCase
         ) ||
         rawResponse.Contains(
            "rate-limit",
            StringComparison.OrdinalIgnoreCase
         ) ||
         rawResponse.Contains(
            "ratelimit",
            StringComparison.OrdinalIgnoreCase
         ) ||
         rawResponse.Contains(
            "suspended",
            StringComparison.OrdinalIgnoreCase
         );
   }

   private IReadOnlyList<WebSearchResult> ParseResults(
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
         var publishedAt = ReadPublishedAt(result);

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
               string.IsNullOrWhiteSpace(snippet) ? null : snippet,
               publishedAt
            )
         );

         if(items.Count >= Math.Clamp(
            maxResults,
            1,
            WebSearchDefaults.MaxSearchResults
         ))
         {
            break;
         }
      }

      return items;
   }

   private static DateTimeOffset? ReadPublishedAt(JsonElement element)
   {
      var candidates = new[]
      {
         ReadString(element, "publishedDate"),
         ReadString(element, "published_date"),
         ReadString(element, "publishedAt"),
         ReadString(element, "published_at"),
         ReadString(element, "date")
      };

      foreach(var candidate in candidates)
      {
         if(DateTimeOffset.TryParse(candidate, out var publishedAt))
         {
            return publishedAt;
         }
      }

      return null;
   }

   private static bool IsDeniedDomain(string url)
   {
      if(!Uri.TryCreate(url, UriKind.Absolute, out var uri))
      {
         return false;
      }

      var host = uri.Host;

      foreach(var deniedHostSuffix in WebSearchDefaults.DeniedHostSuffixes)
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

   private string NormalizeUrl(string url)
   {
      if(Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
      {
         return absoluteUri.ToString();
      }

      return new Uri(BaseAddress, url).ToString();
   }

   private static Uri BuildBaseAddress(string? baseUrl)
   {
      if(string.IsNullOrWhiteSpace(baseUrl) ||
         !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
      {
         return new Uri(SearxngWebSearchClientOptions.DefaultBaseUrl);
      }

      var uriString = uri.ToString();

      return uriString.EndsWith("/", StringComparison.Ordinal)
         ? uri
         : new Uri(uriString + "/");
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

   private sealed class SearxngRateLimitedException : Exception
   {
      public SearxngRateLimitedException(string message)
         : base(message)
      {
      }
   }
}
