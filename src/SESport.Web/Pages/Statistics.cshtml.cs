using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Models;
using System.Globalization;

namespace SESport.Web.Pages;

public sealed class StatisticsModel(
   PublicStatisticsRepository repository,
   PublicStatisticsOptions options
) : PageModel
{
   public const string MonthFormat = "yyyy-MM";

   public IReadOnlyList<StatisticsMonthOption> MonthOptions
   {
      get;
      private set;
   } = [];

   public DateOnly SelectedMonth { get; private set; }

   public string SelectedMonthLabel => FormatMonthLabel(SelectedMonth);

   public PublicStatisticsSnapshot? Statistics { get; private set; }

   public string? LoadError { get; private set; }

   [BindProperty(SupportsGet = true, Name = RouteKeys.Month)]
   public string? Month { get; set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      var currentSportDate = SportDay.GetSportDate(
         DateTimeOffset.UtcNow
      );
      var currentMonth = FirstOfMonth(currentSportDate);
      var firstAvailableMonth = FirstOfMonth(
         options.FirstAvailableMonth
      );
      SelectedMonth = NormalizeSelectedMonth(
         Month,
         firstAvailableMonth,
         currentMonth
      );
      MonthOptions = BuildMonthOptions(
         firstAvailableMonth,
         currentMonth,
         SelectedMonth
      );

      try
      {
         Statistics = await repository.GetMonthlyAsync(
            SelectedMonth,
            options.TopParticipantLimit,
            cancellationToken
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
      }
   }

   internal static IReadOnlyList<StatisticsMonthOption>
      BuildMonthOptions(
         DateOnly firstAvailableMonth,
         DateOnly currentMonth,
         DateOnly selectedMonth
      )
   {
      var firstMonth = FirstOfMonth(firstAvailableMonth);
      var lastMonth = FirstOfMonth(currentMonth);
      var options = new List<StatisticsMonthOption>();

      if(firstMonth > lastMonth)
      {
         lastMonth = firstMonth;
      }

      for(
         var month = firstMonth;
         month <= lastMonth;
         month = month.AddMonths(1)
      )
      {
         options.Add(
            new StatisticsMonthOption(
               FormatMonthValue(month),
               FormatMonthLabel(month),
               month == FirstOfMonth(selectedMonth)
            )
         );
      }

      return options;
   }

   internal static DateOnly NormalizeSelectedMonth(
      string? requestedMonth,
      DateOnly firstAvailableMonth,
      DateOnly currentMonth
   )
   {
      var firstMonth = FirstOfMonth(firstAvailableMonth);
      var lastMonth = FirstOfMonth(currentMonth);
      if(lastMonth < firstMonth)
      {
         lastMonth = firstMonth;
      }

      var parsedMonth = ParseMonth(requestedMonth);
      if(
         parsedMonth is null ||
         parsedMonth.Value < firstMonth ||
         parsedMonth.Value > lastMonth
      )
      {
         return lastMonth;
      }

      return parsedMonth.Value;
   }

   internal static DateOnly? ParseMonth(string? value)
   {
      if(!DateTime.TryParseExact(
         value,
         MonthFormat,
         CultureInfo.InvariantCulture,
         DateTimeStyles.None,
         out var parsed
      ))
      {
         return null;
      }

      return new DateOnly(parsed.Year, parsed.Month, 1);
   }

   internal static string FormatMonthValue(DateOnly month)
   {
      return month.ToString(
         MonthFormat,
         CultureInfo.InvariantCulture
      );
   }

   internal static string FormatMonthLabel(DateOnly month)
   {
      var culture = CultureInfo.GetCultureInfo(
         PrimaryCountry.CultureName
      );
      var value = month.ToString("MMMM yyyy", culture);
      return culture.TextInfo.ToTitleCase(value);
   }

   private static DateOnly FirstOfMonth(DateOnly date)
   {
      return new DateOnly(date.Year, date.Month, 1);
   }
}

public sealed record StatisticsMonthOption(
   string Value,
   string Label,
   bool IsSelected
);
