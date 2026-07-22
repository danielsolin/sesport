using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using SESport.AI.WebSearch;

namespace SESport.Core.Tests.AI;

public class SearxngWebSearchClientTests
{
   [Fact]
   public async Task SearchParsesJsonResults()
   {
      var handler = new RecordingHandler(CreateResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions()
      );

      var response = await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Equal(
         new Uri("http://127.0.0.1:8088/search"),
         handler.RequestUri
      );
      Assert.Contains("q=Tre+Kronor", handler.RequestBody);
      Assert.Contains("format=json", handler.RequestBody);
      Assert.DoesNotContain("categories=", handler.RequestBody);
      Assert.Contains("engines=google", handler.RequestBody);
      Assert.Equal("application/json", handler.AcceptHeader);
      Assert.Single(response.Results);
      Assert.Equal("Tre Kronor roster", response.Results[0].Title);
      Assert.Equal("https://example.test/roster", response.Results[0].Url);
      Assert.Equal("Sweden lineup info.", response.Results[0].Snippet);
   }

   [Fact]
   public async Task SearchUsesConfiguredBaseUrl()
   {
      var handler = new RecordingHandler(CreateResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions
         {
            BaseUrl = "http://127.0.0.1:18088"
         }
      );

      await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Equal(
         new Uri("http://127.0.0.1:18088/search"),
         handler.RequestUri
      );
   }

   [Fact]
   public async Task SearchRotatesDefaultEngines()
   {
      await AssertRequestUsesEngine(0, "google");
      await AssertRequestUsesEngine(1, "brave");
      await AssertRequestUsesEngine(2, "duckduckgo");
      await AssertRequestUsesEngine(3, "bing");
      await AssertRequestUsesEngine(4, "mojeek");
      await AssertRequestUsesEngine(5, "privacywall");
      await AssertRequestUsesEngine(6, "seznam");
      await AssertRequestUsesEngine(7, "naver");
      await AssertRequestUsesEngine(8, "boardreader");
      await AssertRequestUsesEngine(9, "yep");
      await AssertRequestUsesEngine(10, "yahoo");
      await AssertRequestUsesEngine(11, "google_cse");
      await AssertRequestUsesEngine(12, "gmx");
      await AssertRequestUsesEngine(13, "resulthunter");
   }

   [Fact]
   public async Task SearchDropsDeniedSocialDomains()
   {
      var handler = new RecordingHandler(CreateMixedResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions()
      );

      var response = await client.SearchAsync(
         "Tre Kronor",
         5,
         CancellationToken.None
      );

      Assert.Single(response.Results);
      Assert.Equal("Official roster", response.Results[0].Title);
      Assert.Equal("https://example.test/roster", response.Results[0].Url);
   }

   [Fact]
   public async Task SearchIncludesSocialDomainsWhenEnabled()
   {
      var handler = new RecordingHandler(CreateMixedResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions()
      );

      var response = await client.SearchAsync(
         "Tre Kronor",
         5,
         CancellationToken.None,
         includeSocialMedia: true
      );

      Assert.Equal(2, response.Results.Count);
      Assert.Equal("Instagram post", response.Results[0].Title);
   }

   [Fact]
   public async Task SearchReturnsMetadataOnlyResults()
   {
      var handler = new RecordingHandler(CreateResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions()
      );

      var response = await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Single(response.Results);
      Assert.Equal("Tre Kronor roster", response.Results[0].Title);
      Assert.Equal("https://example.test/roster", response.Results[0].Url);
   }

   [Fact]
   public async Task SearchReturnsPdfResults()
   {
      var handler = new RecordingHandler(CreatePdfMixedResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions()
      );

      var response = await client.SearchAsync(
         "Tre Kronor",
         5,
         CancellationToken.None
      );

      Assert.Equal(2, response.Results.Count);
      Assert.Equal("PDF roster", response.Results[0].Title);
      Assert.Equal("https://example.test/roster.pdf", response.Results[0].Url);
      Assert.Equal("Official roster", response.Results[1].Title);
      Assert.Equal("https://example.test/roster", response.Results[1].Url);
   }

   [Fact]
   public async Task SearchReturnsUnverifiedResults()
   {
      var handler = new RecordingHandler(CreateResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions()
      );

      var response = await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Single(response.Results);
      Assert.Equal("Tre Kronor roster", response.Results[0].Title);
      Assert.Equal("https://example.test/roster", response.Results[0].Url);
   }

   [Fact]
   public async Task EmptyQuerySkipsRequest()
   {
      var handler = new RecordingHandler(CreateResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions()
      );

      var response = await client.SearchAsync(
         " ",
         3,
         CancellationToken.None
      );

      Assert.Empty(response.Results);
      Assert.Null(handler.RequestUri);
   }

   [Fact]
   public async Task SearchRetriesWhenInitialRequestTimesOut()
   {
      var handler = new FlakyTimeoutHandler(CreateResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler)
         {
            Timeout = TimeSpan.FromMilliseconds(50)
         },
         new SearxngWebSearchClientOptions(),
         CreateFastRateLimiter()
      );

      var response = await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Equal(2, handler.RequestCount);
      Assert.Single(response.Results);
      Assert.Equal("Tre Kronor roster", response.Results[0].Title);
   }

   [Fact]
   public async Task SearchWaitsAndRetriesWhenRateLimited()
   {
      var handler = new SequenceHandler(
         new SequenceHandler.ResponseSpec(
            HttpStatusCode.TooManyRequests,
            "too many requests"
         ),
         new SequenceHandler.ResponseSpec(
            HttpStatusCode.OK,
            CreateResponseJson()
         )
      );
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions(),
         CreateFastRateLimiter()
      );

      var response = await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Equal(2, handler.RequestCount);
      Assert.Contains("engines=google", handler.RequestBodies[0]);
      Assert.Contains("engines=brave", handler.RequestBodies[1]);
      Assert.Single(response.Results);
      Assert.Equal("Tre Kronor roster", response.Results[0].Title);
   }

   [Fact]
   public async Task SearchRetriesCaptchaResponseWithoutReturningEmpty()
   {
      var handler = new SequenceHandler(
         new SequenceHandler.ResponseSpec(
            HttpStatusCode.OK,
            CreateCaptchaResponseJson()
         ),
         new SequenceHandler.ResponseSpec(
            HttpStatusCode.OK,
            CreateResponseJson()
         )
      );
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions(),
         CreateFastRateLimiter()
      );

      var response = await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Equal(2, handler.RequestCount);
      Assert.Single(response.Results);
      Assert.Equal("Tre Kronor roster", response.Results[0].Title);
   }

   [Fact]
   public async Task SearchIgnoresCaptchaReportedForDifferentEngine()
   {
      var handler = new SequenceHandler(
         new SequenceHandler.ResponseSpec(
            HttpStatusCode.OK,
            CreateCaptchaResponseJson("duckduckgo")
         )
      );
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions(),
         CreateFastRateLimiter()
      );

      var response = await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Equal(1, handler.RequestCount);
      Assert.Empty(response.Results);
   }

   [Fact]
   public async Task SearchStopsAfterOneConfiguredEngineCycle()
   {
      var handler = new SequenceHandler(
         new SequenceHandler.ResponseSpec(
            HttpStatusCode.OK,
            CreateCaptchaResponseJson("google")
         ),
         new SequenceHandler.ResponseSpec(
            HttpStatusCode.OK,
            CreateCaptchaResponseJson("brave")
         )
      );
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions
         {
            Engines = ["google", "brave"]
         },
         CreateFastRateLimiter()
      );

      var exception = await Record.ExceptionAsync(
         () => client.SearchAsync(
            "Tre Kronor",
            3,
            CancellationToken.None
         )
      );

      Assert.NotNull(exception);
      Assert.Equal(2, handler.RequestCount);
   }

   [Fact]
   public async Task SearchReturnsEmptyWhenSearxngReturnsNoResults()
   {
      var handler = new RecordingHandler(CreateEmptyResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions(),
         CreateFastRateLimiter()
      );

      var response = await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Empty(response.Results);
      Assert.NotNull(handler.RequestUri);
   }

   [Fact]
   public async Task SearchRecentCombinesDayAndWeekResults()
   {
      var handler = new SequenceHandler(
         new SequenceHandler.ResponseSpec(
            HttpStatusCode.OK,
            CreateRecentResponseJson(
               ("Today", "https://example.test/today")
            )
         ),
         new SequenceHandler.ResponseSpec(
            HttpStatusCode.OK,
            CreateRecentResponseJson(
               ("Today", "https://example.test/today"),
               ("This week", "https://example.test/week")
            )
         )
      );
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions(),
         CreateFastRateLimiter()
      );

      var response = await client.SearchRecentAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Equal(2, handler.RequestCount);
      Assert.Contains("time_range=day", handler.RequestBodies[0]);
      Assert.Contains("time_range=week", handler.RequestBodies[1]);
      Assert.Contains("engines=yahoo", handler.RequestBodies[0]);
      Assert.Contains("engines=yahoo", handler.RequestBodies[1]);
      Assert.Equal(
         ["Today", "This week"],
         response.Results.Select(result => result.Title)
      );
   }

   private static string CreateResponseJson()
   {
      return JsonSerializer.Serialize(new
      {
         query = "Tre Kronor",
         results = new[]
         {
            new
            {
               title = "Tre Kronor roster",
               url = "https://example.test/roster",
               content = "Sweden lineup info."
            }
         },
         answers = Array.Empty<object>(),
         corrections = Array.Empty<string>(),
         infoboxes = Array.Empty<object>(),
         suggestions = Array.Empty<string>(),
         unresponsive_engines = Array.Empty<object>()
      });
   }

   private static string CreateMixedResponseJson()
   {
      return JsonSerializer.Serialize(new
      {
         query = "Tre Kronor",
         results = new[]
         {
            new
            {
               title = "Instagram post",
               url = "https://instagram.com/p/example",
               content = "Social post."
            },
            new
            {
               title = "Official roster",
               url = "https://example.test/roster",
               content = "Sweden lineup info."
            }
         },
         answers = Array.Empty<object>(),
         corrections = Array.Empty<string>(),
         infoboxes = Array.Empty<object>(),
         suggestions = Array.Empty<string>(),
         unresponsive_engines = Array.Empty<object>()
      });
   }

   private static string CreatePdfMixedResponseJson()
   {
      return JsonSerializer.Serialize(new
      {
         query = "Tre Kronor",
         results = new[]
         {
            new
            {
               title = "PDF roster",
               url = "https://example.test/roster.pdf",
               content = "PDF file."
            },
            new
            {
               title = "Official roster",
               url = "https://example.test/roster",
               content = "Sweden lineup info."
            }
         },
         answers = Array.Empty<object>(),
         corrections = Array.Empty<string>(),
         infoboxes = Array.Empty<object>(),
         suggestions = Array.Empty<string>(),
         unresponsive_engines = Array.Empty<object>()
      });
   }

   private static string CreateEmptyResponseJson()
   {
      return JsonSerializer.Serialize(new
      {
         query = "Tre Kronor",
         results = Array.Empty<object>(),
         answers = Array.Empty<object>(),
         corrections = Array.Empty<string>(),
         infoboxes = Array.Empty<object>(),
         suggestions = Array.Empty<string>(),
         unresponsive_engines = Array.Empty<object>()
      });
   }

   private static string CreateRecentResponseJson(
      params (string Title, string Url)[] results
   )
   {
      return JsonSerializer.Serialize(new
      {
         query = "Tre Kronor",
         results = results.Select(result => new
         {
            title = result.Title,
            url = result.Url,
            content = result.Title
         }),
         answers = Array.Empty<object>(),
         corrections = Array.Empty<string>(),
         infoboxes = Array.Empty<object>(),
         suggestions = Array.Empty<string>(),
         unresponsive_engines = Array.Empty<object>()
      });
   }

   private static string CreateCaptchaResponseJson(
      string engine = "google"
   )
   {
      return JsonSerializer.Serialize(new
      {
         query = "Tre Kronor",
         results = Array.Empty<object>(),
         answers = Array.Empty<object>(),
         corrections = Array.Empty<string>(),
         infoboxes = Array.Empty<object>(),
         suggestions = Array.Empty<string>(),
         unresponsive_engines = new[]
         {
            new[] { engine, "CAPTCHA" }
         }
      });
   }

   private static SearchRateLimiter CreateFastRateLimiter()
   {
      return new SearchRateLimiter(
         new WebSearchRateLimitOptions
         {
            MinimumRequestInterval = TimeSpan.Zero,
            RateLimitedCooldown = TimeSpan.FromMilliseconds(1),
            TransientFailureCooldown = TimeSpan.FromMilliseconds(1)
         }
      );
   }

   private static async Task AssertRequestUsesEngine(
      int searchAttempt,
      string engine
   )
   {
      var handler = new RecordingHandler(CreateResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions()
      );

      await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None,
         searchAttempt
      );

      Assert.Contains($"engines={engine}", handler.RequestBody);
   }

   private sealed class RecordingHandler : HttpMessageHandler
   {
      private readonly string responseJson;

      public RecordingHandler(string responseJson)
      {
         this.responseJson = responseJson;
      }

      public Uri? RequestUri { get; private set; }

      public string RequestBody { get; private set; } = string.Empty;

      public string? AcceptHeader { get; private set; }

      public string? AuthorizationHeader { get; private set; }

      protected override async Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         RequestUri = request.RequestUri;
         AcceptHeader = request.Headers.Accept.FirstOrDefault()?.ToString();
         AuthorizationHeader = request.Headers.Authorization?.ToString();
         RequestBody = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

         return new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = JsonContent.Create(
               JsonSerializer.Deserialize<JsonElement>(responseJson)
            )
         };
      }
   }

   private sealed class FlakyTimeoutHandler : HttpMessageHandler
   {
      private readonly string responseJson;

      public FlakyTimeoutHandler(string responseJson)
      {
         this.responseJson = responseJson;
      }

      public int RequestCount { get; private set; }

      protected override async Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         RequestCount++;

         if(RequestCount == 1)
         {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
         }

         return new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = JsonContent.Create(
               JsonSerializer.Deserialize<JsonElement>(responseJson)
            )
         };
      }
   }

   private sealed class SequenceHandler : HttpMessageHandler
   {
      private readonly Queue<ResponseSpec> responses;

      public SequenceHandler(params ResponseSpec[] responses)
      {
         this.responses = new Queue<ResponseSpec>(responses);
      }

      public int RequestCount { get; private set; }

      public List<string> RequestBodies { get; } = [];

      protected override async Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         RequestCount++;
         var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
         RequestBodies.Add(body);

         var response = responses.Count == 0
            ? new ResponseSpec(HttpStatusCode.OK, CreateResponseJson())
            : responses.Dequeue();

         return new HttpResponseMessage(response.StatusCode)
         {
            Content = new StringContent(response.Body)
         };
      }

      public sealed record ResponseSpec(
         HttpStatusCode StatusCode,
         string Body
      );
   }

}
