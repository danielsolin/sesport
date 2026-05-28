namespace SESport.Core.Ingestion;

public interface IEventSourceImporter
{
   Source Source { get; }

   Task<ImportRun> ImportEventsAsync(
      ImportRequest request,
      CancellationToken cancellationToken
   );
}
