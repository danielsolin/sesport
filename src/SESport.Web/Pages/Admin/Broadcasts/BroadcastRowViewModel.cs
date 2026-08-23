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
         [$"{RouteKeys.BroadcastIds}[0]"] = broadcast.Id.ToString()
      };

      var returnUrl = GetActivityReturnUrl(request);
      if(returnUrl is not null)
      {
         activityRouteValues[RouteKeys.ReturnUrl] = returnUrl;
      }

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

   internal static string? GetActivityReturnUrl(HttpRequest request)
   {
      var path = request.Path.Value;
      if(string.IsNullOrWhiteSpace(path))
      {
         return null;
      }

      if(IsAjaxPath(path))
      {
         return GetSameOriginRefererReturnUrl(request);
      }

      return request.Path + request.QueryString;
   }

   private static string? GetSameOriginRefererReturnUrl(
      HttpRequest request
   )
   {
      var referer = request.Headers.Referer.ToString();
      if(!Uri.TryCreate(referer, UriKind.Absolute, out var refererUri)
         || !string.Equals(
            refererUri.Host,
            request.Host.Host,
            StringComparison.OrdinalIgnoreCase
         )
         || request.Host.Port is int requestPort
            && refererUri.Port != requestPort)
      {
         return null;
      }

      var path = refererUri.AbsolutePath;
      if(string.IsNullOrWhiteSpace(path) || IsAjaxPath(path))
      {
         return null;
      }

      return path + refererUri.Query;
   }

   private static bool IsAjaxPath(string path)
   {
      return path.Equals("/Admin/Ajax", StringComparison.OrdinalIgnoreCase)
         || path.StartsWith(
            "/Admin/Ajax/",
            StringComparison.OrdinalIgnoreCase
         );
   }
}
