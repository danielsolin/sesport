using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages;

public class IndexModel(ActivityRepository repository) : PageModel
{
   public const string TodayDay = "Today";
   public const string TomorrowDay = "Tomorrow";

   public IReadOnlyList<ActivityListItem> Activities { get; private set; } =
      [];

   [BindProperty(SupportsGet = true, Name = "day")]
   public string? Day { get; set; } = TodayDay;

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      Day = NormalizeDay(Day) ?? TodayDay;

      try
      {
         Activities = Day switch
         {
            TodayDay => await repository.GetTodaysAsync(
               cancellationToken
            ),
            TomorrowDay => await repository.GetTomorrowsAsync(
               cancellationToken
            ),
            _ => await repository.GetTodaysAsync(cancellationToken)
         };
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   private static string? NormalizeDay(string? day)
   {
      return day switch
      {
         TodayDay => TodayDay,
         TomorrowDay => TomorrowDay,
         "" => TodayDay,
         null => TodayDay,
         _ => null
      };
   }
}
