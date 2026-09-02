using System.Net;
using System.Net.Http.Json;

using SESport.AI.Clients;

namespace SESport.Core.Tests.AI;

public class AiProviderApiKeySourceTests
{
   [Fact]
   public async Task OpenRouterStillUsesEnvironmentApiKeySource()
   {
      var previous = Environment.GetEnvironmentVariable("TEST_API_KEY");
      Environment.SetEnvironmentVariable("TEST_API_KEY", "env-token");

      try
      {
         var handler = new RecordingHandler();
#pragma warning disable CS0618 // Intentional legacy provider coverage.
         var client = new OpenRouterClient(new HttpClient(handler));
#pragma warning restore CS0618
         var provider = CreateProvider(
            "environment:TEST_API_KEY",
            "openrouter"
         );

         await client.GenerateAsync(
            provider,
            CreateJob(),
            CreatePrompt(),
            CreateRenderedPrompt(),
            "{}",
            CancellationToken.None
         );

         Assert.Equal("Bearer env-token", handler.AuthorizationHeader);
      }
      finally
      {
         Environment.SetEnvironmentVariable("TEST_API_KEY", previous);
      }
   }

   private static AiProviderDefinition CreateProvider(
      string apiKeySource,
      string kind = "openrouter"
   )
   {
      return new AiProviderDefinition(
         "provider",
         "Provider",
         kind,
         "http://127.0.0.1:1234/v1/",
         "gpt",
         apiKeySource,
         "{}",
         true
      );
   }

   private static AiJobDefinition CreateJob()
   {
      return new AiJobDefinition(
         "job",
         "Job",
         null,
         "provider",
         "text",
         null,
         null,
         null,
         true,
         true,
         null
      );
   }

   private static AiPromptDefinition CreatePrompt()
   {
      return new AiPromptDefinition(
         Guid.NewGuid(),
         "job",
         1,
         "System",
         "User",
         null,
         "{}",
         null,
         null,
         null,
         true
      );
   }

   private static AiRenderedPrompt CreateRenderedPrompt()
   {
      return new AiRenderedPrompt(
         "System",
         "User"
      );
   }

   private sealed class RecordingHandler : HttpMessageHandler
   {
      public string? AuthorizationHeader { get; private set; }

      protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         AuthorizationHeader = request.Headers.Authorization?.ToString();

         return Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
               Content = JsonContent.Create(new { output_text = "ok" })
            }
         );
      }
   }
}
