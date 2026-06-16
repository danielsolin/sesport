namespace SESport.AI.Services;

public sealed class AiJobExecutionGate
{
   private readonly SemaphoreSlim semaphore = new(1, 1);

   public async ValueTask WaitAsync(CancellationToken cancellationToken)
   {
      await semaphore.WaitAsync(cancellationToken);
   }

   public void Release()
   {
      semaphore.Release();
   }
}
