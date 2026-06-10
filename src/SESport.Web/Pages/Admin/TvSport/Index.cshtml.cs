using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using SESport.AI.Abstractions;
using SESport.AI.Models;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.TvSport;

public class IndexModel(
   TvSportRepository repository,
   AdminDatePreferenceStore datePreferenceStore,
   IAiJobRunner aiJobRunner
) : PageModel
{
   public const string ChannelSortColumn = "Channel";
   public const string TimeSortColumn = "Time";
   public const string BroadcastSortColumn = "Broadcast";
   public const string CategoriesSortColumn = "Categories";

   [BindProperty(SupportsGet = true, Name = "date")]
   public DateOnly? Date { get; set; }

   [BindProperty(SupportsGet = true, Name = "hideReplays")]
   public bool HideReplays { get; set; }

   [BindProperty(SupportsGet = true, Name = "showHidden")]
   public bool ShowHidden { get; set; }

   [BindProperty(SupportsGet = true)]
   public List<string> SelectedSports { get; set; } = [];

   [BindProperty(SupportsGet = true)]
   public string SortColumn { get; set; } = TimeSortColumn;

   [BindProperty(SupportsGet = true)]
   public bool SortAsc { get; set; } = true;

   public string DateText => DateDisplay.Format(SelectedDate);

   public DateOnly SelectedDate { get; private set; }

   public IReadOnlyList<TvSportBroadcastListItem> Broadcasts
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<SelectListItem> SportOptions
   {
      get;
      private set;
   } = [];

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
         GetNextSortAsc(sortColumn),
         SelectedSports
      );
      routeValues["sortColumn"] = sortColumn;

      return routeValues;
   }

   public async Task<IActionResult> OnPostHideAsync(
      Guid id,
      bool isHidden,
      CancellationToken cancellationToken
   )
   {
      if(isHidden)
      {
         await repository.ShowAsync(id, cancellationToken);
      }
      else
      {
         await repository.HideAsync(id, cancellationToken);
      }

      SortColumn = NormalizeSortColumn(SortColumn);

      if(WantsJsonResponse())
      {
         return new JsonResult(new { hidden = !isHidden });
      }

      var selectedDate = Date ??
         SportDay.Tomorrow(DateTimeOffset.UtcNow).StartDate;
      var routeValues = AdminRouteValueBuilder.CreateSortRouteValues(
         selectedDate,
         HideReplays,
         ShowHidden,
         SortAsc,
         SelectedSports
      );
      routeValues["sortColumn"] = SortColumn;

      return RedirectToPage(routeValues);
   }

   public async Task<IActionResult> OnPostGenerateActivityAsync(
      List<Guid> tvSportBroadcastIds,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      var broadcastIds = NormalizeBroadcastIds(tvSportBroadcastIds);

      if(broadcastIds.Count == 0)
      {
         SortColumn = NormalizeSortColumn(SortColumn);
         await LoadAsync(cancellationToken);

         return Page();
      }

      var routeValues = new Dictionary<string, object?>();

      for(var index = 0; index < broadcastIds.Count; index++)
      {
         routeValues[$"tvSportBroadcastIds[{index}]"] =
            broadcastIds[index];
      }

      if(Url.IsLocalUrl(returnUrl))
      {
         routeValues["returnUrl"] = returnUrl;
      }

      return RedirectToPage("/Admin/Activities/Edit", routeValues);
   }

   public async Task<IActionResult> OnPostCheckSwedishParticipationAsync(
      List<Guid> tvSportBroadcastIds,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var broadcastIds = NormalizeBroadcastIds(tvSportBroadcastIds);

         if(broadcastIds.Count == 0)
         {
            return BadRequest(new
            {
               error = "Select at least one broadcast."
            });
         }

         var broadcasts = await repository.GetActivitySourcesAsync(
            broadcastIds,
            cancellationToken
         );
         var results = new List<object>();

         foreach(var broadcast in broadcasts)
         {
            var result = await aiJobRunner.RunAsync(
               new AiJobRequest(
                  "decide-swedish-participation",
                  CreateParticipationInputJson(broadcast),
                  broadcast.Id.ToString()
               ),
               cancellationToken
            );

            if(!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
               results.Add(
                  CreateParticipationCheckResult(
                     broadcast,
                     result.ErrorMessage,
                     null,
                     []
                  )
               );

               continue;
            }

            var parsed = ParseParticipationResult(result.OutputText);

            if(parsed is null)
            {
               results.Add(
                  CreateParticipationCheckResult(
                     broadcast,
                     "The model returned invalid JSON.",
                     null,
                     []
                  )
               );

               continue;
            }

            results.Add(
               CreateParticipationCheckResult(
                  broadcast,
                  null,
                  parsed.SwedishParticipation,
                  parsed.SwedishParticipants
               )
            );
         }

         return new JsonResult(new
         {
            results
         });
      }
      catch(Exception exception)
      {
         return new JsonResult(new
         {
            error = exception.Message
         })
         {
            StatusCode = StatusCodes.Status500InternalServerError
         };
      }
   }

   private bool WantsJsonResponse()
   {
      return Request.Headers.Accept.Any(value =>
         value?.Contains(
            "application/json",
            StringComparison.OrdinalIgnoreCase
         ) == true
      );
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
            .Select(category => new TvSportCategoryOption(
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

   private static IReadOnlyList<TvSportBroadcastListItem> SortBroadcasts(
      IEnumerable<TvSportBroadcastListItem> broadcasts,
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

   private static IReadOnlyList<TvSportBroadcastListItem> OrderByDirection(
      IEnumerable<TvSportBroadcastListItem> broadcasts,
      Func<TvSportBroadcastListItem, string> keySelector,
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

   private static string CreateParticipationInputJson(
      TvSportBroadcastActivitySource broadcast
   )
   {
      var localStart = TvSportRepository.ToLocal(broadcast.StartsAt);

      return JsonSerializer.Serialize(
         new
         {
            sport = broadcast.Categories,
            event_name = broadcast.Title,
            date_time = $"{localStart:yyyy-MM-dd HH:mm}"
         }
      );
   }

   private static object CreateParticipationCheckResult(
      TvSportBroadcastActivitySource broadcast,
      string? error,
      string? swedishParticipation,
      IReadOnlyList<string> swedishParticipants
   )
   {
      return new
      {
         id = broadcast.Id,
         channelName = broadcast.ChannelName,
         title = broadcast.Title,
         error,
         swedishParticipation,
         swedishParticipants
      };
   }

   private static SwedishParticipationResult? ParseParticipationResult(
      string outputText
   )
   {
      try
      {
         using var document = JsonDocument.Parse(outputText);
         var root = document.RootElement;

         if(root.ValueKind != JsonValueKind.Object)
         {
            return null;
         }

         if(
            !root.TryGetProperty(
               "SwedishParticipation",
               out var participation
            ) ||
            participation.ValueKind != JsonValueKind.String
         )
         {
            return null;
         }

         var participants = new List<string>();

         if(
            root.TryGetProperty(
               "SwedishParticipants",
               out var participantsElement
            ) &&
            participantsElement.ValueKind == JsonValueKind.Array
         )
         {
            foreach(var participant in participantsElement.EnumerateArray())
            {
               if(participant.ValueKind != JsonValueKind.String)
               {
                  continue;
               }

               var name = participant.GetString();

               if(!string.IsNullOrWhiteSpace(name))
               {
                  participants.Add(name);
               }
            }
         }

         return new SwedishParticipationResult(
            participation.GetString() ?? string.Empty,
            participants
         );
      }
      catch(JsonException)
      {
         return null;
      }
   }

   private sealed record SwedishParticipationResult(
      string SwedishParticipation,
      IReadOnlyList<string> SwedishParticipants
   );

}
