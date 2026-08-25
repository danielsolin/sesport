using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Broadcasts;

public class IndexModel(
   AdminBroadcastRepository repository,
   AdminRepository adminRepository,
   BroadcastDatePreferenceStore datePreferenceStore,
   FilterPreferenceStore filterPreferenceStore,
   BroadcastParticipationService participationService,
   TodoRepository todoRepository
) : PageModel
{
   public const string SportFilterCookieName =
      "sesport.admin.broadcasts.sports";
   public const string ShowHiddenFilterCookieName =
      "sesport.admin.broadcasts.show-hidden";
   public const string HideReplaysFilterCookieName =
      "sesport.admin.broadcasts.hide-replays";
   public const string ChannelSortColumn = "Channel";
   public const string TimeSortColumn = "Time";
   public const string BroadcastSortColumn = "Broadcast";
   public const string CategoriesSortColumn = "Categories";
   public const string BroadcastVisibilityShowLabel = "Show";
   public const string BroadcastVisibilityHideLabel = "Hide";
   public const string BroadcastVisibilityCheckLabel = "Check";

   [BindProperty(SupportsGet = true, Name = RouteKeys.Date)]
   public DateOnly? Date { get; set; }

   [BindProperty(SupportsGet = true, Name = RouteKeys.TitleFilter)]
   public string? TitleFilter { get; set; } = string.Empty;

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

   private IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, Guid>>
      participantEntityIdsByOrganizationEntityId = new Dictionary<
         Guid,
         IReadOnlyDictionary<string, Guid>
      >();

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      ResolveFilterPreferences();
      SortColumn = NormalizeSortColumn(SortColumn);
      await LoadAsync(cancellationToken);
      WriteFilterPreferences();
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

      if(!string.IsNullOrWhiteSpace(TitleFilter))
      {
         routeValues[RouteKeys.TitleFilter] = TitleFilter;
      }

      return routeValues;
   }

   public IReadOnlyList<BroadcastParticipantDisplayItem>
      GetParticipantDisplayItems(
         Guid? organizationEntityId,
         IReadOnlyList<string> participantNames
      )
   {
      if(organizationEntityId is null ||
         !participantEntityIdsByOrganizationEntityId.TryGetValue(
            organizationEntityId.Value,
            out var participantEntityIdsByName
         ))
      {
         participantEntityIdsByName = new Dictionary<string, Guid>();
      }

      return BroadcastParticipationService.GetParticipantDisplayItems(
         participantNames,
         participantEntityIdsByName
      );
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

   public async Task<IActionResult> OnPostAddTodoAsync(
      string? text,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      if(!string.IsNullOrWhiteSpace(text))
      {
         await todoRepository.CreateAsync(
            TodoTargetTypeIds.Broadcasts,
            text,
            null,
            cancellationToken
         );
      }

      if(Url.IsLocalUrl(returnUrl))
      {
         return LocalRedirect(returnUrl!);
      }

      return RedirectToPage("./Index");
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
      TitleFilter = TitleFilter?.Trim() ?? string.Empty;

      try
      {
         var normalizedSports = SportFilter.Normalize(SelectedSports);
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
            TitleFilter,
            cancellationToken
         );
         Broadcasts = SortBroadcasts(Broadcasts, SortColumn, SortAsc);
         Broadcasts = await participationService.ApplyParticipationChecksAsync(
            Broadcasts,
            cancellationToken
         );
         participantEntityIdsByOrganizationEntityId =
            await LoadParticipantEntityIdsAsync(
               Broadcasts
                  .Select(broadcast => broadcast.OrganizationEntityId)
                  .Where(
                     organizationEntityId => organizationEntityId is not null
                  )
                  .Select(organizationEntityId => organizationEntityId!.Value)
                  .Distinct()
                  .ToArray(),
               cancellationToken
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
      }
   }

   private void ResolveFilterPreferences()
   {
      SelectedSports = filterPreferenceStore.ResolveList(
         HttpContext,
         RouteKeys.SelectedSports,
         SelectedSports,
         SportFilterCookieName
      ).ToList();
      ShowHidden = filterPreferenceStore.ResolveBoolean(
         HttpContext,
         RouteKeys.ShowHidden,
         ShowHidden,
         ShowHiddenFilterCookieName
      );
      HideReplays = filterPreferenceStore.ResolveBoolean(
         HttpContext,
         RouteKeys.HideReplays,
         HideReplays,
         HideReplaysFilterCookieName
      );
   }

   private void WriteFilterPreferences()
   {
      filterPreferenceStore.WriteList(
         HttpContext,
         SportFilterCookieName,
         SelectedSports
      );
      filterPreferenceStore.WriteBoolean(
         HttpContext,
         ShowHiddenFilterCookieName,
         ShowHidden
      );
      filterPreferenceStore.WriteBoolean(
         HttpContext,
         HideReplaysFilterCookieName,
         HideReplays
      );
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
            broadcast =>
               BroadcastListDisplayFormatter.FormatCategoriesText(
                  broadcast.Categories
               ),
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

   private static List<Guid> NormalizeBroadcastIds(IEnumerable<Guid> ids)
   {
      return ids
         .Where(id => id != Guid.Empty)
         .Distinct()
         .ToList();
   }

   private async Task<
      IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, Guid>>
   > LoadParticipantEntityIdsAsync(
         Guid[] organizationEntityIds,
         CancellationToken cancellationToken
      )
   {
      var participantEntityIdsByOrganizationEntityId =
         new Dictionary<Guid, IReadOnlyDictionary<string, Guid>>();

      foreach(var organizationEntityId in organizationEntityIds)
      {
         var entityOptions =
            await adminRepository
               .GetBroadcastParticipantEntityNameOptionsAsync(
               organizationEntityId,
               cancellationToken
            );
         participantEntityIdsByOrganizationEntityId[
            organizationEntityId
         ] = BroadcastEntityFilter.CreateNameLookup(
            entityOptions,
            entity => entity.Name,
            entity => entity.Id
         );
      }

      return participantEntityIdsByOrganizationEntityId;
   }

}
