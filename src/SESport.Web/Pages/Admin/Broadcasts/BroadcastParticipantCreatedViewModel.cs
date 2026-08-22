namespace SESport.Web.Pages.Admin.Broadcasts;

public sealed record BroadcastParticipantCreatedViewModel(
   string Name,
   string EditUrl,
   string SearchUrlBase,
   string? OrganizationSportName
)
{
   public string SearchQuery => string.Join(
      " ",
      new[] { Name, OrganizationSportName }
         .Where(value => !string.IsNullOrWhiteSpace(value))
   );
}
