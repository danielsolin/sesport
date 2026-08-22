using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Activities;

public sealed record ActivityParticipantSelectionViewModel(
   IReadOnlyList<ActivityParticipantListItem> Participants,
   Guid? ActivityId
);
