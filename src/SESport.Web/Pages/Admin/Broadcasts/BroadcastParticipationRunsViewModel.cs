using SESport.Data;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Broadcasts;

public sealed record BroadcastParticipationRunsViewModel(
   BroadcastListItem Broadcast,
   string CheckParticipationUrl,
   string CreateParticipantEntityUrl,
   string ActivityUrl,
   IndexModel PageModel
);
