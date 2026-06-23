using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.Core.Broadcast;
using SESport.Core.Formatting;
using SESport.Data;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Broadcasts;

public class IndexModel(
   BroadcastRepository repository,
   AdminRepository adminRepository,
   AdminDatePreferenceStore datePreferenceStore,
   BroadcastParticipationService participationService
) : PageModel
{
   public const string ChannelSortColumn = "Channel";
   public const string TimeSortColumn = "Time";
   public const string BroadcastSortColumn = "Broadcast";
   public const string CategoriesSortColumn = "Categories";

   [BindProperty(SupportsGet = true, Name = RouteKeys.Date)]
   public DateOnly? Date { get; set; }

   [BindProperty(SupportsGet = true, Name = RouteKeys.HideReplays)]
   public bool HideReplays { get; set; }

   [BindProperty(SupportsGet = true, Name = RouteKeys.ShowHidden)]
   public bool ShowHidden { get; set; }

   [BindProperty(SupportsGet = true)]
   public List<string> SelectedSports { get; set; } = [];

   [BindProperty(SupportsGet = true, Name = RouteKeys.SortColumn)]
   public string SortColumn { get; set; } = TimeSortColumn;

   [BindProperty(SupportsGet = true)]
   public bool SortAsc { get; set; } = true;

   public string DateText => DateDisplay.Format(SelectedDate);

   public DateOnly SelectedDate { get; private set; }

   public IReadOnlyList<BroadcastListItem> Broadcasts
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<SelectListItem> SportOptions
   {
      get;
      private set;
   } = [];

   public IReadOnlyDictionary<string, Guid> ParticipantEntityIdsByName
   {
      get;
      private set;
   } = new Dictionary<string, Guid>();

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      SortColumn = NormalizeSortColumn(SortColumn);
      await LoadAsync(cancellationToken);
   }

   public bool GetNextSortAsc(string sortColumn) =>
      string.Equals(SortColumn, sortColumn, StringComparison.Ordinal)
         ? !SortAsc
         : true;

   public string GetSortIndicator(string sortColumn)
   {
      if(!string.Equals(SortColumn, sortColumn, StringComparison.Ordinal))
      {
         return string.Empty;
      }

      return SortAsc ? "▲" : "▼";
   }

   public Dictionary<string, string?> GetSortRouteValues(string sortColumn)
   {
      var routeValues = AdminRouteValueBuilder.CreateSortRouteValues(
         SelectedDate,
         HideReplays,
         ShowHidden,
         GetNextSortAsc(sortColumn),
         SelectedSports
      );
      routeValues[RouteKeys.SortColumn] = sortColumn;

      return routeValues;
   }

   public IReadOnlyList<BroadcastParticipantDisplayItem>
      GetParticipantDisplayItems(
         IReadOnlyList<string> participantNames
      )
   {
      Guid? templateEntityId = null;

      foreach(var participantName in participantNames)
      {
         var normalizedName =
            BroadcastEntityFilter.NormalizeName(participantName);

         if(ParticipantEntityIdsByName.TryGetValue(
            normalizedName,
            out var entityId
         ))
         {
            templateEntityId = entityId;
            break;
         }
      }

      return participantNames
         .Select(name =>
         {
            var normalizedName = BroadcastEntityFilter.NormalizeName(name);

            if(ParticipantEntityIdsByName.TryGetValue(
               normalizedName,
               out var entityId
            ))
            {
               return new BroadcastParticipantDisplayItem(
                  name,
                  Url.Page("/Admin/Entities/Edit", new { id = entityId }),
                  null
               );
            }

            return new BroadcastParticipantDisplayItem(
               name,
               null,
               templateEntityId
            );
         })
         .ToList();
   }

   public Dictionary<string, string?> GetActivityRouteValues(
      Guid broadcastId,
      Guid? participationRunId = null
   )
   {
      var routeValues = new Dictionary<string, string?>
      {
         [$"{RouteKeys.BroadcastIds}[0]"] = broadcastId.ToString(),
         [RouteKeys.ReturnUrl] = Request.Path + Request.QueryString
      };

      if(participationRunId is not null && participationRunId != Guid.Empty)
      {
         routeValues[RouteKeys.ParticipationRunId] =
            participationRunId.Value.ToString();
      }

      return routeValues;
   }

   public async Task<IActionResult> OnPostGenerateActivityAsync(
      List<Guid> broadcastIds,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      var normalizedBroadcastIds = NormalizeBroadcastIds(broadcastIds);

      if(normalizedBroadcastIds.Count == 0)
      {
         SortColumn = NormalizeSortColumn(SortColumn);
         await LoadAsync(cancellationToken);

         return Page();
      }

      var routeValues = new Dictionary<string, object?>
      {
         [$"{RouteKeys.BroadcastIds}[0]"] = normalizedBroadcastIds[0]
      };

      if(Url.IsLocalUrl(returnUrl))
      {
         routeValues[RouteKeys.ReturnUrl] = returnUrl;
      }

      return RedirectToPage("/Admin/Activities/Edit", routeValues);
   }

   private async Task LoadAsync(CancellationToken cancellationToken)
   {
      SelectedDate = datePreferenceStore.ResolveDate(HttpContext, Date);

      try
      {
         var normalizedSports = NormalizeSelectedSports(SelectedSports);
         SelectedSports = normalizedSports.Count == 0
            ? [string.Empty]
            : normalizedSports;
         var categories = await repository.GetCategoriesForDateAsync(
            SelectedDate,
            HideReplays,
            ShowHidden,
            cancellationToken
         );
         SportOptions =
         [
            new SelectListItem(
               "Alla",
               string.Empty,
               normalizedSports.Count == 0
            ),
            .. categories
            .Select(category => new BroadcastCategoryOption(
               category,
               normalizedSports.Contains(category)
            ))
            .Select(option => new SelectListItem(
               option.Name,
               option.Name,
               option.IsSelected
            ))
         ];
         Broadcasts = await repository.GetByDateAsync(
            SelectedDate,
            HideReplays,
            ShowHidden,
            normalizedSports,
            cancellationToken
         );
         Broadcasts = SortBroadcasts(Broadcasts, SortColumn, SortAsc);
         Broadcasts = await participationService.ApplyParticipationChecksAsync(
            Broadcasts,
            cancellationToken
         );
         ParticipantEntityIdsByName = await LoadParticipantEntityIdsAsync(
            cancellationToken
         );
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   private static string NormalizeSortColumn(string? sortColumn) =>
      sortColumn switch
      {
         ChannelSortColumn => ChannelSortColumn,
         BroadcastSortColumn => BroadcastSortColumn,
         CategoriesSortColumn => CategoriesSortColumn,
         _ => TimeSortColumn
      };

   private static IReadOnlyList<BroadcastListItem> SortBroadcasts(
      IEnumerable<BroadcastListItem> broadcasts,
      string sortColumn,
      bool sortAsc
   )
   {
      return sortColumn switch
      {
         ChannelSortColumn => OrderByDirection(
            broadcasts,
            broadcast => broadcast.ChannelName,
            sortAsc
         ),
         BroadcastSortColumn => OrderByDirection(
            broadcasts,
            broadcast => broadcast.Title,
            sortAsc
         ),
         CategoriesSortColumn => OrderByDirection(
            broadcasts,
            broadcast => broadcast.Categories,
            sortAsc
         ),
         _ => OrderByDirection(
            broadcasts,
            broadcast => broadcast.TimeText,
            sortAsc
         )
      };
   }

   private static IReadOnlyList<BroadcastListItem> OrderByDirection(
      IEnumerable<BroadcastListItem> broadcasts,
      Func<BroadcastListItem, string> keySelector,
      bool sortAsc
   )
   {
      var sortedBroadcasts = sortAsc
         ? broadcasts.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase)
         : broadcasts.OrderByDescending(
            keySelector,
            StringComparer.OrdinalIgnoreCase
         );

      return sortedBroadcasts
         .ThenBy(broadcast => broadcast.TimeText, StringComparer.Ordinal)
         .ThenBy(broadcast => broadcast.ChannelName)
         .ThenBy(broadcast => broadcast.Title)
         .ToList();
   }

   private static List<string> NormalizeSelectedSports(
      IEnumerable<string> values
   )
   {
      return values
         .Where(value => !string.IsNullOrWhiteSpace(value))
         .Select(value => value.Trim())
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   private static List<Guid> NormalizeBroadcastIds(IEnumerable<Guid> ids)
   {
      return ids
         .Where(id => id != Guid.Empty)
         .Distinct()
         .ToList();
   }

   private async Task<IReadOnlyDictionary<string, Guid>>
      LoadParticipantEntityIdsAsync(
         CancellationToken cancellationToken
      )
   {
      var entityOptions = await adminRepository.GetPersonEntityNameOptionsAsync(
         cancellationToken
      );

      return entityOptions
         .Where(entity => !string.IsNullOrWhiteSpace(entity.Name))
         .GroupBy(entity => BroadcastEntityFilter.NormalizeName(entity.Name))
         .Where(group => !string.IsNullOrWhiteSpace(group.Key))
         .ToDictionary(group => group.Key, group => group.First().Id);
   }

}
