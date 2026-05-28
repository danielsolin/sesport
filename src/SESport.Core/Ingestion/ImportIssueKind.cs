namespace SESport.Core.Ingestion;

public enum ImportIssueKind
{
   MissingSourceMapping,
   NoEventsFound,
   ParsingFailed,
   SourceUnavailable,
   UnexpectedSourceShape,
   UnknownCountryCode
}
