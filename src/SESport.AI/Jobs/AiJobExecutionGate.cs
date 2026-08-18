using System.Collections.Concurrent;

namespace SESport.AI.Jobs;

public sealed class AiJobExecutionGate
{
   private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphores =
      new(StringComparer.Ordinal);

   public async ValueTask WaitAsync(
      string providerId,
      CancellationToken cancellationToken
   )
   {
      var semaphore = GetSemaphore(providerId);
      await semaphore.WaitAsync(cancellationToken);
   }

   public void Release(string providerId)
   {
      var semaphore = GetSemaphore(providerId);
      semaphore.Release();
   }

   private SemaphoreSlim GetSemaphore(string providerId)
   {
      if(string.IsNullOrWhiteSpace(providerId))
      {
         throw new ArgumentException(
            "An AI provider id is required.",
            nameof(providerId)
         );
      }

      return semaphores.GetOrAdd(
         providerId,
         static _ => new SemaphoreSlim(1, 1)
      );
   }
}
