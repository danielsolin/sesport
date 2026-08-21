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
   bool HasPrimaryImage = false
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
