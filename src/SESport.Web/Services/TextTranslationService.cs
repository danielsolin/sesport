using SESport.AI.Jobs;
using SESport.Core.AI;

using System.Text.Json;

namespace SESport.Web.Services;

public sealed class TextTranslationService(IAiJobRunner aiJobRunner)
{
   public Task<Guid> QueueAsync(
      string fromLanguage,
      string toLanguage,
      string text,
      string? correlationId,
      CancellationToken cancellationToken
   )
   {
      var inputPayloadJson = JsonSerializer.Serialize(
         new
         {
            from_language = fromLanguage,
            to_language = toLanguage,
            text
         }
      );

      return aiJobRunner.QueueAsync(
         new AiJobRequest(
            AiJobIds.TranslateText,
            inputPayloadJson,
            correlationId
         ),
         cancellationToken
      );
   }
}
