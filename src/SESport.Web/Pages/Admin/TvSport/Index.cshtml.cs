using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.TvSport;

public class IndexModel(TvSportRepository repository) : PageModel
{
   [BindProperty(SupportsGet = true, Name = "date")]
   public DateOnly? Date { get; set; }

   [BindProperty(SupportsGet = true, Name = "hideReplays")]
   public bool HideReplays { get; set; }

   [BindProperty(SupportsGet = true)]
   public List<string> SelectedSports { get; set; } = [];

   public string DateText => SelectedDate.ToString("yyyy-MM-dd");

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
      await LoadAsync(cancellationToken);
   }

   public async Task<IActionResult> OnPostHideAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      await repository.HideAsync(id, cancellationToken);

      var selectedDate = Date ?? DateOnly.FromDateTime(DateTime.Now.AddDays(1));
      var routeValues = new Dictionary<string, object?>
      {
         ["date"] = selectedDate.ToString("yyyy-MM-dd")
      };

      if(HideReplays)
      {
         routeValues["hideReplays"] = "true";
      }

      var normalizedSports = NormalizeSelectedSports(SelectedSports);

      for(var index = 0; index < normalizedSports.Count; index++)
      {
         routeValues[$"SelectedSports[{index}]"] = normalizedSports[index];
      }

      return RedirectToPage(routeValues);
   }

   private async Task LoadAsync(CancellationToken cancellationToken)
   {
      SelectedDate = Date ?? DateOnly.FromDateTime(DateTime.Now.AddDays(1));

      try
      {
         var normalizedSports = NormalizeSelectedSports(SelectedSports);
         SelectedSports = normalizedSports.Count == 0
            ? [string.Empty]
            : normalizedSports;
         var categories = await repository.GetCategoriesForDateAsync(
            SelectedDate,
            HideReplays,
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
            normalizedSports,
            cancellationToken
         );
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
      }
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
}
