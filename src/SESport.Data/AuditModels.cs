namespace SESport.Data;

public sealed record ActivityProposalAuditItem(
   string Id,
   string Title,
   string Producer,
   string Source,
   string Status,
   string? RejectReason,
   string? RejectComment,
   string ActivityType,
   string Sport,
   string TimeText,
   decimal? Confidence,
   Guid? ActivityId,
   int EntityLinkCount,
   int EvidenceCount,
   DateTime CreatedOn
);

public sealed record ActivityProposalLinkAuditItem(
   string ProposalId,
   string ProposalTitle,
   string EntityName,
   string Role,
   string Explanation,
   string? ContextName,
   decimal? Confidence
);

public sealed record ActivityProposalEvidenceAuditItem(
   string ProposalId,
   string ProposalTitle,
   string Source,
   string? Uri,
   string? Title,
   DateTimeOffset ObservedAt,
   string Summary,
   string? RawExcerpt
);

public sealed record ActivityProposalDetail(
   string Id,
   string Title,
   string? Description,
   string? Context,
   string ProducerTypeId,
   string Producer,
   string Source,
   string Status,
   string? RejectReason,
   string? RejectComment,
   string ActivityType,
   string ActivityTypeId,
   string Sport,
   string SportId,
   string TimeText,
   DateOnly ActivityDate,
   TimeOnly? LocalStartTime,
   string TimeZoneId,
   decimal? Confidence,
   Guid? ActivityId,
   string? Prompt
);

public sealed record RejectReasonOption(string Id, string Label);
