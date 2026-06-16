namespace SESport.AI.Abstractions;

public interface IAiJobProcessor
{
   Task ProcessRunAsync(
      Guid runId,
      CancellationToken cancellationToken
   );
}
