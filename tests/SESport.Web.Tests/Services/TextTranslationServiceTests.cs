using System.Text.Json;

using SESport.AI.Jobs;
using SESport.Core.Configuration;

namespace SESport.Core.Tests.Services;

public sealed class TextTranslationServiceTests
{
   [Fact]
   public async Task QueueAsyncCreatesGenericTranslationRequest()
   {
      var runner = new CapturingAiJobRunner();
      var service = new TextTranslationService(runner);
      var correlationId = Guid.NewGuid().ToString();

      await service.QueueAsync(
         "English",
         PrimaryCountry.LanguageName,
         "A short biography.",
         correlationId,
         CancellationToken.None
      );

      Assert.Equal(AiJobIds.TranslateText, runner.Request.JobId);
      Assert.Equal(correlationId, runner.Request.CorrelationId);

      using var payload = JsonDocument.Parse(
         runner.Request.InputPayloadJson
      );
      var root = payload.RootElement;

      Assert.Equal("English", root.GetProperty("from_language").GetString());
      Assert.Equal(
         PrimaryCountry.LanguageName,
         root.GetProperty("to_language").GetString()
      );
      Assert.Equal(
         "A short biography.",
         root.GetProperty("text").GetString()
      );
   }

   private sealed class CapturingAiJobRunner : IAiJobRunner
   {
      public AiJobRequest Request { get; private set; } = null!;

      public Task<Guid> QueueAsync(
         AiJobRequest request,
         CancellationToken cancellationToken
      )
      {
         Request = request;
         return Task.FromResult(Guid.NewGuid());
      }

      public Task<AiJobResult> RunAsync(
         AiJobRequest request,
         CancellationToken cancellationToken
      )
      {
         throw new NotSupportedException();
      }
   }
}
