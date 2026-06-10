using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SESport.AI.Abstractions;
using SESport.AI.Models;
using SESport.AI.Providers;

namespace SESport.Core.Tests.AI;

public class ResponsesAiProviderClientTests
{
   [Theory]
   [MemberData(nameof(ProviderClients))]
   public async Task GenerateAsyncUsesFinalMessageOutput(
      Func<HttpClient, IAiProviderClient> clientFactory
   )
   {
      var handler = new RecordingHandler(
         CreateReasoningResponseJson("KLM Open spelas i Amsterdam.")
      );
      var client = clientFactory(new HttpClient(handler));

      var result = await client.GenerateAsync(
         CreateProvider(),
         CreateJob(),
         CreatePrompt(),
         "Prompt text",
         "{}",
         CancellationToken.None
      );

      Assert.Equal(
         "KLM Open spelas i Amsterdam.",
         result.OutputText
      );
      Assert.Contains(
         "\"response_format\":{\"type\":\"json_object\"}",
         handler.RequestBody
      );
   }

   public static IEnumerable<object[]> ProviderClients()
   {
      yield return
      [
         new Func<HttpClient, IAiProviderClient>(client =>
            new LmStudioResponsesAiProviderClient(client))
      ];

      yield return
      [
         new Func<HttpClient, IAiProviderClient>(client =>
            new OpenRouterResponsesAiProviderClient(client))
      ];
   }

   private static AiProviderDefinition CreateProvider()
   {
      return new AiProviderDefinition(
         "provider",
         "Provider",
         "lmstudio",
         "http://127.0.0.1:1234/v1/",
         "gpt-oss-20b",
         "key:secret-token",
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
         "json_object",
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

   private sealed class RecordingHandler : HttpMessageHandler
   {
      private readonly string responseJson;

      public RecordingHandler(string responseJson)
      {
         this.responseJson = responseJson;
      }

      public string? RequestBody { get; private set; }

      protected override async Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken
      )
      {
         RequestBody = await request.Content!.ReadAsStringAsync(
            cancellationToken
         );

         return new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = JsonContent.Create(
               JsonSerializer.Deserialize<JsonElement>(responseJson)
            )
         };
      }
   }
}
