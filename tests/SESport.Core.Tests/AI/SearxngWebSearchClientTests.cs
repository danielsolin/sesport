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
      Assert.Contains("engines=google", handler.RequestBody);
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
   public async Task SearchReturnsMetadataOnlyResults()
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

      Assert.Single(results);
      Assert.Equal("Tre Kronor roster", results[0].Title);
      Assert.Equal("https://example.test/roster", results[0].Url);
   }

   [Fact]
   public async Task SearchReturnsPdfResults()
   {
      var handler = new RecordingHandler(CreatePdfMixedResponseJson());
      var client = new SearxngWebSearchClient(
         new HttpClient(handler),
         new SearxngWebSearchClientOptions()
      );

      var results = await client.SearchAsync(
         "Tre Kronor",
         5,
         CancellationToken.None
      );

      Assert.Equal(2, results.Count);
      Assert.Equal("PDF roster", results[0].Title);
      Assert.Equal("https://example.test/roster.pdf", results[0].Url);
      Assert.Equal("Official roster", results[1].Title);
      Assert.Equal("https://example.test/roster", results[1].Url);
   }

   [Fact]
   public async Task SearchReturnsUnverifiedResults()
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

      Assert.Single(results);
      Assert.Equal("Tre Kronor roster", results[0].Title);
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

}
