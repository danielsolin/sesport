namespace SESport.Data;

public sealed record ActivityProposalAuditItem(
   string Id,
   string Title,
   string Producer,
   string Status,
   string ActivityType,
   string Sport,
   string TimeText,
   DateTime CreatedOn
);

public sealed record ActivityProposalLinkAuditItem(
   string EntityName,
   string Role,
   string Explanation,
   string? ContextName,
   decimal? Confidence
);

public sealed record ActivityProposalEvidenceAuditItem(
   string Source,
   string? Uri,
   string? Title,
   DateTimeOffset ObservedAt,
   string Summary
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
   string Sport,
   string TimeText,
   decimal? Confidence,
   Guid? ActivityId,
   string? Prompt
);

public sealed record RejectReasonOption(string Id, string Label);
