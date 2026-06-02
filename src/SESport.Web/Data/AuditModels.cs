namespace SESport.Web.Data;

public sealed record AuditArea(string Title, string Description, string Href);

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
   string? GroupId,
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
)
{
   public string ConfidencePercentString =>
         Confidence.HasValue
            ? Math.Floor(Confidence.Value * 100).ToString()
            : string.Empty;
};

public sealed record ActivityProposalEvidenceAuditItem(
   string ProposalId,
   string ProposalTitle,
   string Source,
   string? Uri,
   string? Title,
   DateTimeOffset ObservedAt,
   string Summary,
   string? RawExcerpt
)
{
   public string UrlShort
   {
      get
      {
         var shortUrl = Uri != null ? Uri : string.Empty;

         shortUrl = shortUrl.Replace("https://", "");
         shortUrl = shortUrl.Replace("http://", "");
         shortUrl = shortUrl.Replace("www.", "");
         shortUrl = shortUrl.Substring(0, shortUrl.IndexOf('/'));
         shortUrl += "↗";

         return shortUrl;
      }
   }
};

public sealed record ActivityProposalDetail(
   string Id,
   string Title,
   string? Description,
   string? Context,
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
   string? GroupId,
   Guid? ActivityId
)
{
   public string ConfidencePercentString =>
         Confidence.HasValue
            ? Math.Floor(Confidence.Value * 100).ToString()
            : string.Empty;
};

public sealed record RejectReasonOption(string Id, string Label);

public sealed record ActivityLinkAuditItem(
   Guid ActivityId,
   string ActivityTitle,
   string TimeText,
   string EntityName,
   string EntityType
);

public sealed record ActivityEvidenceAuditItem(
   Guid ActivityId,
   string ActivityTitle,
   string TimeText,
   string Source,
   string? Uri,
   string? Title,
   DateTimeOffset ObservedAt,
   string? Comment,
   string? ProposalId
);

public sealed record ProposalGroupAuditItem(
   string Id,
   string Fingerprint,
   Guid? ActivityId,
   int ProposalCount,
   DateTimeOffset UpdatedAt
);
