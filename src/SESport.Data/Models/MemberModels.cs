namespace SESport.Data.Models;

public enum MemberWatchSort
{
   Name,
   NextActivity
}

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
