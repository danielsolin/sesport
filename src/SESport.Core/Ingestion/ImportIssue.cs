namespace SESport.Core.Ingestion;

public sealed record ImportIssue(
   ImportIssueSeverity Severity,
   ExternalEntityId? ExternalId,
   string Message
);
