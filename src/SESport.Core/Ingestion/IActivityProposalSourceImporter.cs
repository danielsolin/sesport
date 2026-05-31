namespace SESport.Core.Ingestion;

public interface IActivityProposalSourceImporter
{
   Source Source { get; }

   Task<ImportRun> ImportActivityProposalsAsync(
      ImportRequest request,
      CancellationToken cancellationToken
   );
}
