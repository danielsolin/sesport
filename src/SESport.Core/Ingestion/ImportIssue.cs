namespace SESport.Core.Ingestion;

public sealed record ImportIssue(
   ImportIssueKind Kind,
   ImportIssueSeverity Severity,
   ExternalEntityId? ExternalId,
   string Message
);
