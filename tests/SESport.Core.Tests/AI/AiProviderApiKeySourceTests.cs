using System.Net;
using System.Net.Http.Json;
using SESport.AI.Models;
using SESport.AI.Providers;

namespace SESport.Core.Tests.AI;

public class AiProviderApiKeySourceTests
{
   [Fact]
   public async Task LmStudioUsesInlineApiKeySource()
   {
      var handler = new RecordingHandler();
      var client = new LmStudioClient(new HttpClient(handler));
      var provider = CreateProvider("key:secret-token");

      await client.GenerateAsync(
         provider,
         CreateJob(),
         CreatePrompt(),
         "Prompt text",
         "{}",
         CancellationToken.None
      );

      Assert.Equal("Bearer secret-token", handler.AuthorizationHeader);
   }

   [Fact]
   public async Task OpenRouterStillUsesEnvironmentApiKeySource()
   {
      var previous = Environment.GetEnvironmentVariable("LMSTUDIO_API_KEY");
      Environment.SetEnvironmentVariable("LMSTUDIO_API_KEY", "env-token");

      try
      {
         var handler = new RecordingHandler();
         var client = new OpenRouterClient(new HttpClient(handler));
         var provider = CreateProvider(
            "environment:LMSTUDIO_API_KEY",
            "openrouter"
         );

         await client.GenerateAsync(
            provider,
            CreateJob(),
            CreatePrompt(),
            "Prompt text",
            "{}",
            CancellationToken.None
         );

         Assert.Equal("Bearer env-token", handler.AuthorizationHeader);
      }
      finally
      {
         Environment.SetEnvironmentVariable("LMSTUDIO_API_KEY", previous);
      }
   }

   private static AiProviderDefinition CreateProvider(
      string apiKeySource,
      string kind = "lmstudio"
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
         true
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
         true
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
