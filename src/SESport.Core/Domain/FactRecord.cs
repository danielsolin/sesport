namespace SESport.Core.Domain;

public static class FactSubjectTypes
{
   public const string Activity = ApplicationObjectTypes.Activity;

   public const string Entity = ApplicationObjectTypes.Entity;
}

public sealed record FactRecord(
   Guid Id,
   string SubjectType,
   Guid SubjectId,
   string Text,
   DateTimeOffset CreatedAt,
   DateTimeOffset UpdatedAt,
   IReadOnlyList<string> SourceUrls
);

public sealed record FactSourceDraft(
   string Url,
   string? Title,
   string? Excerpt
);

public sealed record FactDraft(
   string Text,
   IReadOnlyList<FactSourceDraft> Sources
);
