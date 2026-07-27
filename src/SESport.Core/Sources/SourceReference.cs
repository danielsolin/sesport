namespace SESport.Core.Sources;

using SESport.Core.Domain;

public static class SourceCorrelationTypes
{
   public const string Entity = ApplicationObjectTypes.Entity;

   public const string Activity = ApplicationObjectTypes.Activity;

   public const string Broadcast = ApplicationObjectTypes.Broadcast;

   public const string AiJobRun = ApplicationObjectTypes.AiJobRun;
}

public static class SourceKinds
{
   public const string Bio = "Bio";

   public const string PersonFacts = "PersonFacts";

   public const string ActivityEvidence = "ActivityEvidence";

   public const string ParticipationEvidence = "ParticipationEvidence";
}

public sealed record SourceReference(
   Guid Id,
   string CorrelationType,
   string CorrelationId,
   string Kind,
   string Url,
   string? Title,
   string? Excerpt,
   DateTimeOffset ObservedAt,
   DateTimeOffset CreatedAt
);
