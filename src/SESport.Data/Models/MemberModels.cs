namespace SESport.Data.Models;

public sealed record MemberPersonListItem(
   Guid Id,
   string Name,
   string SportName,
   string RelatedNames
)
{
   public string DisplayInformation =>
      string.IsNullOrWhiteSpace(RelatedNames)
         ? SportName
         : $"{SportName}, {RelatedNames}";
}
