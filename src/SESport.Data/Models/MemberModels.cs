using SESport.Core.Domain;

namespace SESport.Data.Models;

public sealed record AdminMemberListItem(
   Guid Id,
   string Email,
   DateTimeOffset CreatedAt,
   DateTimeOffset? LastLoginAt,
   int WatchCount,
   int PushNotificationSentCount,
   int LoginTokenCreatedCount,
   int LoginTokenConsumedCount
);

public sealed record MemberPersonListItem(
   Guid Id,
   string Name,
   string SportName,
   string RelatedNames,
   MemberNextActivity? NextActivity = null,
   bool HasPrimaryImage = false,
   MemberPrimaryImageSource? PrimaryImageSource = null,
   bool IsWatched = false
)
{
   public string DisplayInformation =>
      string.IsNullOrWhiteSpace(RelatedNames)
         ? SportName
         : $"{SportName}, {RelatedNames}";
}

public sealed record MemberWatchListItem(
   Guid Id,
   string Name,
   string EntityTypeId,
   string? AliasName,
   string SportName,
   string RelatedNames,
   MemberNextActivity? NextActivity = null,
   bool HasPrimaryImage = false,
   MemberPrimaryImageSource? PrimaryImageSource = null,
   bool IsWatched = false
)
{
   public string DisplayInformation =>
      EntityTypeId == TrackedEntityTypeIds.Person &&
      !string.IsNullOrWhiteSpace(RelatedNames)
         ? $"{SportName}, {RelatedNames}"
         : SportName;
}

public sealed record MemberNextActivity(
   DateTimeOffset StartsAt,
   string Title,
   string? RelatedOrganizationName
);

public sealed record MemberPrimaryImage(
   byte[] Data,
   string MimeType
);

public sealed record MemberPrimaryImageSource(
   string SourceUrl,
   string? CreatorName,
   string LicenseName,
   string? LicenseUrl
);
