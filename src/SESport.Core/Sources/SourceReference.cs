namespace SESport.Core.Sources;

public static class SourceCorrelationTypes
{
   public const string Entity = "Entity";

   public const string Activity = "Activity";

   public const string Broadcast = "Broadcast";

   public const string ActivityProposal = "ActivityProposal";

   public const string AiJobRun = "AiJobRun";
}

public static class SourceKinds
{
   public const string Bio = "Bio";

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
