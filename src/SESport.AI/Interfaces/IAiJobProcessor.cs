namespace SESport.AI.Interfaces;

public interface IAiJobProcessor
{
   Task ProcessRunAsync(
      Guid runId,
      CancellationToken cancellationToken
   );
}
