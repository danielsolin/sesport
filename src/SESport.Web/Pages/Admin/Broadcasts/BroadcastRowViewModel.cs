using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SESport.Core.Broadcast;
using SESport.Core.Formatting;
using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Broadcasts;

public sealed record BroadcastRowViewModel(
   BroadcastListItem Broadcast,
   string? CheckParticipationUrl,
   string? ActivityGroupSearchUrl,
   string? HideBroadcastUrl,
   string? ActivityUrl,
   string SearchUrlBase,
   DateOnly? Date,
   string SortColumn,
   bool SortAsc,
   bool ShowHidden,
   bool HideReplays,
   IReadOnlyList<string> SelectedSports
)
{
   public static BroadcastRowViewModel Create(
      BroadcastListItem broadcast,
      IUrlHelper url,
      HttpRequest request,
      DateOnly? date,
      string? sortColumn,
      bool sortAsc,
      bool showHidden,
      bool hideReplays,
      IEnumerable<string>? selectedSports,
      string? searchUrlBase
   )
   {
      var activityRouteValues = new Dictionary<string, string?>
      {
         [$"{RouteKeys.BroadcastIds}[0]"] = broadcast.Id.ToString(),
         [RouteKeys.ReturnUrl] = request.Path + request.QueryString
      };

      return new BroadcastRowViewModel(
         broadcast,
         url.Page("/Admin/Ajax/Create/ParticipationCheck"),
         url.Page("/Admin/Ajax/Search/ActivityGroup"),
         url.Page("/Admin/Ajax/Toggle/BroadcastVisibility"),
         url.Page("/Admin/Activities/Edit", activityRouteValues),
         searchUrlBase ?? string.Empty,
         date,
         sortColumn ?? IndexModel.TimeSortColumn,
         sortAsc,
         showHidden,
         hideReplays,
         selectedSports?.ToArray() ?? []
      );
   }
}
