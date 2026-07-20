namespace SESport.Core.Ingestion;

public interface IActivityProposalSourceImporter
{
   IngestionSource Source { get; }

   Task<ImportRun> ImportActivityProposalsAsync(
      ImportRequest request,
      CancellationToken cancellationToken
   );
}
