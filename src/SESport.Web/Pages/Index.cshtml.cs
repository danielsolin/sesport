using System.Globalization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data;
using SESport.Web.Services;

namespace SESport.Web.Pages;

public class IndexModel(
   ActivityRepository repository,
   PublicActivityTimelineBuilder timelineBuilder
) : PageModel
{
   public IReadOnlyList<PublicActivityTimelineEntry> TimelineEntries
   {
      get; private set;
   } = [];

   public IReadOnlyList<ActivityListItem> UntimedActivities
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<DateOption> DateOptions { get; private set; } = [];

   [BindProperty(SupportsGet = true, Name = RouteKeys.Date)]
   public string? Date { get; set; }

   public DateOnly SelectedDate { get; private set; }

   public bool IsSportToday { get; private set; }

   public DateOnly CurrentDate { get; private set; }

   public int? TotalParticipantsCount { get; private set; }

   public string? LoadError { get; private set; }

   public bool ShowLoadErrorDetails =>
      string.Equals(
         HttpContext.Request.Host.Host,
         "dev.sesport.se",
         StringComparison.OrdinalIgnoreCase
      );

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      var now = DateTimeOffset.UtcNow;
      CurrentDate = DateOnly.FromDateTime(
         TimeZoneHelper.ToLocal(now, SportDay.TimeZoneId).DateTime
      );
      var sportToday = SportDay.Today(now).StartDate;
      SelectedDate = ParseDate(Date) ?? sportToday;
      IsSportToday = SelectedDate == sportToday;
      DateOptions = BuildDateOptions(sportToday, SelectedDate);

      try
      {
         var activities = await repository.GetPublishedForDateAsync(
            SelectedDate,
            cancellationToken
         );
         var timeline = timelineBuilder.Build(
            activities,
            SelectedDate,
            now
         );
         TimelineEntries = timeline.TimelineEntries;
         UntimedActivities = timeline.UntimedActivities;
         TotalParticipantsCount = CountParticipants(activities);
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   internal static int CountParticipants(
      IEnumerable<ActivityListItem> activities
   )
   {
      return activities
         .SelectMany(activity => activity.RelatedPersonEntityIds)
         .Where(entityId => entityId != Guid.Empty)
         .Distinct()
         .Count();
   }

   internal static IReadOnlyList<string> SplitParticipantNames(
      string? participants
   )
   {
      return string.IsNullOrWhiteSpace(participants)
         ? []
         : participants.Split(
               ", ",
               StringSplitOptions.RemoveEmptyEntries |
               StringSplitOptions.TrimEntries
            );
   }

   internal static int? CalculateAge(DateOnly? birthdate, DateOnly today)
   {
      if(birthdate is null)
      {
         return null;
      }

      var age = today.Year - birthdate.Value.Year;

      if(birthdate.Value > today.AddYears(-age))
      {
         age--;
      }

      return age;
   }

   internal static string FormatBirthday(DateOnly? birthdate)
   {
      return birthdate?.ToString(
         "d MMMM, yyyy",
         CultureInfo.GetCultureInfo("sv-SE")
      ) ?? string.Empty;
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

   private static IReadOnlyList<DateOption> BuildDateOptions(
      DateOnly todayDate,
      DateOnly selectedDate
   )
   {
      var dates = new List<DateOnly>();

      for(var offset = -1; offset <= 1; offset++)
      {
         dates.Add(todayDate.AddDays(offset));
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

}

public sealed record ActivityAgendaSection(
   string TimeLabel,
   IReadOnlyList<ActivityListItem> Activities,
   string RelatedOrganizationEntities,
   ActivityDayPhase DayPhase
);

public enum ActivityDayPhase
{
   Day,
   Evening,
   Night
}

public sealed record DateOption(
   string Value,
   string Label,
   bool IsSelected
);
