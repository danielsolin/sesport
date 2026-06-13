using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SESport.AI.Providers;

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

      var results = await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Equal(
         new Uri("https://xng.sesport.se/search"),
         handler.RequestUri
      );
      Assert.Contains("q=Tre+Kronor", handler.RequestBody);
      Assert.Contains("format=json", handler.RequestBody);
      Assert.Contains("categories=general", handler.RequestBody);
      Assert.Equal("application/json", handler.AcceptHeader);
      Assert.Single(results);
      Assert.Equal("Tre Kronor roster", results[0].Title);
      Assert.Equal("https://example.test/roster", results[0].Url);
      Assert.Equal("Sweden lineup info.", results[0].Snippet);
   }

   [Fact]
   public async Task SearchDropsDeniedSocialDomains()
   {
      var handler = new RecordingHandler(CreateMixedResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions()
      );

      var results = await client.SearchAsync(
         "Tre Kronor",
         5,
         CancellationToken.None
      );

      Assert.Single(results);
      Assert.Equal("Official roster", results[0].Title);
      Assert.Equal("https://example.test/roster", results[0].Url);
   }

   [Fact]
   public async Task EmptyQuerySkipsRequest()
   {
      var handler = new RecordingHandler(CreateResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions()
      );

      var results = await client.SearchAsync(
         " ",
         3,
         CancellationToken.None
      );

      Assert.Empty(results);
      Assert.Null(handler.RequestUri);
   }

   [Fact]
   public async Task SearchUsesBasicAuthWhenConfigured()
   {
      var handler = new RecordingHandler(CreateResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions
         {
            BasicAuthUsername = "user",
            BasicAuthPassword = "pass"
         }
      );

      await client.SearchAsync(
         "Tre Kronor",
         3,
         CancellationToken.None
      );

      Assert.Equal("Basic dXNlcjpwYXNz", handler.AuthorizationHeader);
   }

   [Fact]
   public async Task SearchFetchesMultiplePagesWhenNeeded()
   {
      var handler = new RecordingHandler(
         CreatePagedResponseJson("page1", 20),
         CreatePagedResponseJson("page2", 10)
      );
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions()
      );

      var results = await client.SearchAsync(
         "Tre Kronor",
         25,
         CancellationToken.None
      );

      Assert.Equal(25, results.Count);
      Assert.Equal(2, handler.RequestBodies.Count);
      Assert.Contains("pageno=1", handler.RequestBodies[0]);
      Assert.Contains("pageno=2", handler.RequestBodies[1]);
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

   private static string CreatePagedResponseJson(
      string prefix,
      int count
   )
   {
      var results = Enumerable.Range(1, count).Select(index => new
      {
         title = $"{prefix} result {index}",
         url = $"https://example.test/{prefix}/{index}",
         content = $"{prefix} snippet {index}"
      });

      return JsonSerializer.Serialize(new
      {
         query = "Tre Kronor",
         results,
         answers = Array.Empty<object>(),
         corrections = Array.Empty<string>(),
         infoboxes = Array.Empty<object>(),
         suggestions = Array.Empty<string>(),
         unresponsive_engines = Array.Empty<object>()
      });
   }

   private sealed class RecordingHandler : HttpMessageHandler
   {
      private readonly Queue<string> responseJson;

      public RecordingHandler(params string[] responseJson)
      {
         this.responseJson = new Queue<string>(responseJson);
      }

      public Uri? RequestUri { get; private set; }

      public string RequestBody { get; private set; } = string.Empty;

      public List<string> RequestBodies { get; } = [];

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
         RequestBodies.Add(RequestBody);

         return new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = JsonContent.Create(
               JsonSerializer.Deserialize<JsonElement>(
                  responseJson.Count == 0 ? "{}" : responseJson.Dequeue()
               )
            )
         };
      }
   }
}
