using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data;
using System.Globalization;

namespace SESport.Web.Pages;

public class IndexModel(ActivityRepository repository) : PageModel
{
   public IReadOnlyList<ActivityListItem> Activities { get; private set; } =
      [];

   public IReadOnlyList<ActivityAgendaSection> AgendaSections
   {
      get; private set;
   } = [];

   public IReadOnlyList<ActivityListItem> UntimedActivities
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<DateOption> DateOptions { get; private set; } = [];

   [BindProperty(SupportsGet = true, Name = "date")]
   public string? Date { get; set; }

   public DateOnly SelectedDate { get; private set; }

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      var now = DateTimeOffset.UtcNow;
      var sportToday = SportDay.Today(now).StartDate;
      SelectedDate = ParseDate(Date) ?? sportToday;
      DateOptions = BuildDateOptions(sportToday, SelectedDate);

      try
      {
         Activities = await repository.GetPublishedForDateAsync(
            SelectedDate,
            cancellationToken
         );
         BuildAgendaSections();
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   private static DateOnly? ParseDate(string? date)
   {
      return DateOnly.TryParseExact(
         date,
         DateDisplay.DateOnlyFormat,
         CultureInfo.InvariantCulture,
         DateTimeStyles.None,
         out var parsedDate
      )
         ? parsedDate
         : null;
   }

   private void BuildAgendaSections()
   {
      var timedActivities = new List<ActivityListItem>();
      var untimedActivities = new List<ActivityListItem>();

      foreach(var activity in Activities)
      {
         if(HasLocalStartTime(activity))
         {
            timedActivities.Add(activity);
         }
         else
         {
            untimedActivities.Add(activity);
         }
      }

      var agendaSections = new List<ActivityAgendaSection>();

      foreach(
         var group in timedActivities.GroupBy(activity => activity.TimeOnlyText)
      )
      {
         var relatedOrganization = string.Join(
            ", ",
            group.Select(activity =>
               activity.RelatedOrganizationEntities)
               .Where(summary => !string.IsNullOrWhiteSpace(summary))
               .Distinct(StringComparer.Ordinal)
         );

         agendaSections.Add(
            new ActivityAgendaSection(
               group.Key,
               group.ToList(),
               relatedOrganization
            )
         );
      }

      AgendaSections = agendaSections;
      UntimedActivities = untimedActivities;
   }

   private static IReadOnlyList<DateOption> BuildDateOptions(
      DateOnly todayDate,
      DateOnly selectedDate
   )
   {
      var dates = new List<DateOnly>();

      for(var offset = 0; offset <= 7; offset++)
      {
         dates.Add(todayDate.AddDays(offset));
      }

      if(!dates.Contains(selectedDate))
      {
         dates.Add(selectedDate);
      }

      return dates
         .OrderBy(date => date)
         .Select(date =>
            new DateOption(
               DateDisplay.Format(date),
               DateDisplay.Format(date),
               date == selectedDate
            )
         )
         .ToList();
   }

   private static bool HasLocalStartTime(ActivityListItem activity)
   {
      return activity.TimeText.Contains(' ');
   }
}

public sealed record ActivityAgendaSection(
   string TimeLabel,
   IReadOnlyList<ActivityListItem> Activities,
   string RelatedOrganizationEntities
);

public sealed record DateOption(
   string Value,
   string Label,
   bool IsSelected
);
