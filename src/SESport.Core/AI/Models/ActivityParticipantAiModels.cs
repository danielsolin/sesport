using SESport.Core.Sources;

namespace SESport.Core.AI;

public sealed record ActivityParticipantAiFieldDraft(
   string FieldKey,
   string? ValueText,
   string ValueJson
);

public sealed record ActivityParticipantAiParticipantDraft(
   string Name,
   IReadOnlyList<ActivityParticipantAiFieldDraft> Fields,
   IReadOnlyList<SourceEvidenceDraft> Sources
);

public sealed record ActivityParticipantAiOutputDraft(
   IReadOnlyList<ActivityParticipantAiParticipantDraft> Participants,
   IReadOnlyList<SourceEvidenceDraft> CheckedSources
);

public sealed record ActivityParticipantAiResultValueDraft(
   Guid EntityId,
   string FieldKey,
   string? ValueText,
   string ValueJson,
   IReadOnlyList<SourceEvidenceDraft> Sources
);

public sealed record ActivityParticipantAiResultDraft(
   Guid ActivityId,
   string JobId,
   Guid RunId,
   IReadOnlyList<SourceEvidenceDraft> CheckedSources,
   IReadOnlyList<ActivityParticipantAiResultValueDraft> Values
);
