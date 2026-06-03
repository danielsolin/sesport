using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.TvSport;

public class IndexModel(TvSportRepository repository) : PageModel
{
   [BindProperty(SupportsGet = true, Name = "date")]
   public DateOnly? Date { get; set; }

   [BindProperty(SupportsGet = true, Name = "hideReplays")]
   public bool HideReplays { get; set; }

   [BindProperty(SupportsGet = true, Name = "sports")]
   public List<string> SelectedSports { get; set; } = [];

   public string DateText => SelectedDate.ToString("yyyy-MM-dd");

   public DateOnly SelectedDate { get; private set; }

   public IReadOnlyList<TvSportBroadcastListItem> Broadcasts
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<TvSportCategoryOption> SportOptions
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      SelectedDate = Date ?? DateOnly.FromDateTime(DateTime.Now.AddDays(1));

      try
      {
         var normalizedSports = NormalizeSelectedSports(SelectedSports);
         SelectedSports = normalizedSports;
         var categories = await repository.GetCategoriesForDateAsync(
            SelectedDate,
            HideReplays,
            cancellationToken
         );
         SportOptions = categories
            .Select(category => new TvSportCategoryOption(
               category,
               normalizedSports.Contains(category)
            ))
            .ToList();
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
