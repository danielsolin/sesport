using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SESport.AI.Models;
using SESport.AI.Providers;

namespace SESport.Core.Tests.AI;

public class AiProviderClientTests
{
   [Fact]
   public async Task LmStudioGenerateAsyncUsesResponsesEnvelope()
   {
      var handler = new RecordingHandler(
         CreateReasoningResponseJson("KLM Open spelas i Amsterdam.")
      );
      var client = new LmStudioClient(new HttpClient(handler));

      var result = await client.GenerateAsync(
         CreateProvider("lmstudio"),
         CreateJob(),
         CreatePrompt(),
         "Prompt text",
         "{}",
         CancellationToken.None
      );

      Assert.Equal("KLM Open spelas i Amsterdam.", result.OutputText);
      Assert.Contains("\"response_format\":{\"type\":\"json_schema\"",
         handler.RequestBody);
      Assert.Contains("\"input\":\"Prompt text\"", handler.RequestBody);
   }

   [Fact]
   public async Task OpenRouterGenerateAsyncUsesChatCompletionsEnvelope()
   {
      var handler = new RecordingHandler(
         CreateChatResponseJson("{\"ok\":true}")
      );
      var client = new OpenRouterClient(new HttpClient(handler));

      var result = await client.GenerateAsync(
         CreateProvider("openrouter"),
         CreateJob("json_schema"),
         CreatePrompt(),
         "Prompt text",
         "{}",
         CancellationToken.None
      );

      Assert.Equal("{\"ok\":true}", result.OutputText);
      Assert.Equal(new Uri("http://127.0.0.1:1234/v1/chat/completions"),
         handler.RequestUri);
      Assert.Contains("\"messages\":[{\"role\":\"user\"",
         handler.RequestBody);
      Assert.Contains("\"plugins\":[{\"id\":\"web\"}]",
         handler.RequestBody);
      Assert.Contains("\"response_format\":{\"type\":\"json_schema\"",
         handler.RequestBody);
   }

   [Fact]
   public async Task OpenRouterGenerateAsyncUsesSchemaEvenForJsonObjectMode()
   {
      var handler = new RecordingHandler(
         CreateChatResponseJson("{\"ok\":true}")
      );
      var client = new OpenRouterClient(new HttpClient(handler));

      await client.GenerateAsync(
         CreateProvider("openrouter"),
         CreateJob("json_object"),
         CreatePrompt(),
         "Prompt text",
         "{}",
         CancellationToken.None
      );

      Assert.Contains("\"response_format\":{\"type\":\"json_schema\"",
         handler.RequestBody);
   }

   private static AiProviderDefinition CreateProvider(string kind)
   {
      return new AiProviderDefinition(
         "provider",
         "Provider",
         kind,
         "http://127.0.0.1:1234/v1/",
         "gpt-4o-2024-08-06",
         "key:secret-token",
         "{}",
         true
      );
   }

   private static AiJobDefinition CreateJob(string outputMode = "json_object")
   {
      return new AiJobDefinition(
         "job",
         "Job",
         null,
         "provider",
         outputMode,
         true
      );
   }

   private static AiPromptDefinition CreatePrompt()
   {
      return new AiPromptDefinition(
         Guid.Parse("11111111-1111-1111-1111-111111111111"),
         "job",
         1,
         "System",
         "User",
         """{"type":"object"}""",
         "{}",
         null,
         null,
         true
      );
   }

   private static string CreateReasoningResponseJson(string finalContent)
   {
      return JsonSerializer.Serialize(new
      {
         output = new object[]
         {
            new
            {
               type = "reasoning",
               content = new object[]
               {
                  new
                  {
                     type = "reasoning_text",
                     text = "Need JSON."
                  }
               }
            },
            new
            {
               type = "message",
               content = new object[]
               {
                  new
                  {
                     type = "output_text",
                     text = finalContent
                  }
               }
            }
         }
      });
   }

   private static string CreateChatResponseJson(string content)
   {
      return JsonSerializer.Serialize(new
      {
         choices = new[]
         {
            new
            {
               message = new
               {
                  role = "assistant",
                  content
               }
            }
         },
         model = "openai/gpt-4o-2024-08-06"
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

      public string RequestBody { get; private set; } = "";

      protected override async Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         RequestUri = request.RequestUri;
         RequestBody = request.Content is null
            ? ""
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
