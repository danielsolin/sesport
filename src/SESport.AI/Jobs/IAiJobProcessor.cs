namespace SESport.AI.Jobs;

public interface IAiJobProcessor
{
   Task ProcessRunAsync(
      Guid runId,
      CancellationToken cancellationToken
   );
}
