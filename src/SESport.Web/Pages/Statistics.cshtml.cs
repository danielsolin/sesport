using System.Globalization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Domain;
using SESport.Data.Models;

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

   public IReadOnlyList<PublicStatisticsSportOption> SportOptions
   {
      get;
      private set;
   } = [];

   public DateOnly SelectedMonth { get; private set; }

   public string SelectedMonthValue => FormatMonthValue(SelectedMonth);

   public string SelectedMonthLabel => FormatMonthLabel(SelectedMonth);

   public int TotalParticipantCount { get; private set; }

   public PublicStatisticsSportOption? SelectedSportOption =>
      SportOptions.FirstOrDefault(option => string.Equals(
         option.SportId,
         Sport,
         StringComparison.OrdinalIgnoreCase
      ));

   public string SelectedSportLabel =>
      SelectedSportOption?.SportName ?? "Svenskar";

   public int SelectedSportParticipantCount =>
      SelectedSportOption?.ParticipantCount ??
      TotalParticipantCount;

   public PublicStatisticsSnapshot? Statistics { get; private set; }

   public string? LoadError { get; private set; }

   [BindProperty(SupportsGet = true, Name = RouteKeys.Month)]
   public string? Month { get; set; }

   [BindProperty(SupportsGet = true, Name = RouteKeys.Sport)]
   public string? Sport { get; set; }

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
         var sportSnapshot =
            await repository.GetMonthlySportOptionsAsync(
               SelectedMonth,
               cancellationToken
            );
         TotalParticipantCount = sportSnapshot.ParticipantCount;
         SportOptions = sportSnapshot.Options;
         Sport = NormalizeSportFilter(Sport, SportOptions);
         PublicFilterPreferenceStore.SaveStatistics(
            HttpContext.Response,
            SelectedMonthValue,
            Sport
         );
         Statistics = await repository.GetMonthlyAsync(
            SelectedMonth,
            options.TopParticipantLimit,
            cancellationToken,
            Sport
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
      }
   }

   internal static string? NormalizeSportFilter(
      string? requestedSport,
      IReadOnlyList<PublicStatisticsSportOption> sportOptions
   )
   {
      if(string.IsNullOrWhiteSpace(requestedSport))
      {
         return null;
      }

      return sportOptions
         .Select(option => option.SportId)
         .FirstOrDefault(id => string.Equals(
            id,
            requestedSport.Trim(),
            StringComparison.OrdinalIgnoreCase
         ));
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
