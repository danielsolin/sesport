using System.Globalization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Models;
using SESport.Data.Repositories;

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

   [BindProperty(SupportsGet = true, Name = RouteKeys.Sport)]
   public string? Sport { get; set; }

   public DateOnly SelectedDate { get; private set; }

   public bool IsSportToday { get; private set; }

   public DateOnly CurrentDate { get; private set; }

   public int? TotalParticipantsCount { get; private set; }

   public IReadOnlyList<SportParticipantCount> SportParticipantCounts
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      var now = DateTimeOffset.UtcNow;
      CurrentDate = DateOnly.FromDateTime(
         TimeZoneHelper.ToLocal(now, SportDay.TimeZoneId).DateTime
      );
      var sportToday = SportDay.Today(now).StartDate;
      SelectedDate = ParseDate(Date) ?? sportToday;
      IsSportToday = SelectedDate == sportToday;
      DateOptions = BuildDateOptions(sportToday, SelectedDate, []);

      try
      {
         var publishedDateCounts =
            await repository.GetPublishedDateParticipantCountsFromAsync(
               sportToday,
               cancellationToken
            );
         DateOptions = BuildDateOptions(
            sportToday,
            SelectedDate,
            publishedDateCounts
         );
         var activities = await repository.GetPublishedForDateAsync(
            SelectedDate,
            cancellationToken
         );
         TotalParticipantsCount = CountParticipants(activities);
         SportParticipantCounts =
            CountParticipantsBySport(activities);
         Sport = NormalizeSportFilter(
            Sport,
            SportParticipantCounts
         );
         var filteredActivities = FilterActivitiesBySport(
            activities,
            Sport
         );
         var timeline = timelineBuilder.Build(
            filteredActivities,
            SelectedDate,
            now
         );
         TimelineEntries = timeline.TimelineEntries;
         UntimedActivities = timeline.UntimedActivities;
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
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

   internal static IReadOnlyList<SportParticipantCount>
      CountParticipantsBySport(
         IEnumerable<ActivityListItem> activities
      )
   {
      return activities
         .GroupBy(
            activity => activity.SportId,
            StringComparer.OrdinalIgnoreCase
         )
         .Select(group => new SportParticipantCount(
            group.Key,
            group
               .Select(activity => activity.SportName)
               .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
               ?? group.Key,
            group
               .SelectMany(activity => activity.RelatedPersonEntityIds)
               .Where(entityId => entityId != Guid.Empty)
               .Distinct()
               .Count()
         ))
         .OrderBy(
            item => item.SportName,
            StringComparer.OrdinalIgnoreCase
         )
         .ToArray();
   }

   internal static IReadOnlyList<ActivityListItem>
      FilterActivitiesBySport(
         IEnumerable<ActivityListItem> activities,
         string? sportId
      )
   {
      return string.IsNullOrWhiteSpace(sportId)
         ? activities.ToArray()
         : activities
            .Where(activity => string.Equals(
               activity.SportId,
               sportId,
               StringComparison.OrdinalIgnoreCase
            ))
            .ToArray();
   }

   private static string? NormalizeSportFilter(
      string? sportId,
      IReadOnlyList<SportParticipantCount> sportCounts
   )
   {
      if(string.IsNullOrWhiteSpace(sportId))
      {
         return null;
      }

      return sportCounts
         .Select(sport => sport.SportId)
         .FirstOrDefault(id => string.Equals(
            id,
            sportId.Trim(),
            StringComparison.OrdinalIgnoreCase
         ));
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
      if(birthdate is null)
      {
         return string.Empty;
      }

      var culture = CultureInfo.GetCultureInfo(
         PrimaryCountry.CultureName
      );

      return birthdate.Value.ToString("d MMMM", culture);
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
      DateOnly selectedDate,
      IEnumerable<PublishedDateParticipantCount> publishedDateCounts
   )
   {
      return publishedDateCounts
         .Where(item => item.Date >= todayDate)
         .Append(new PublishedDateParticipantCount(todayDate, 0))
         .Append(new PublishedDateParticipantCount(selectedDate, 0))
         .GroupBy(item => item.Date)
         .Select(group => new PublishedDateParticipantCount(
            group.Key,
            group.Max(item => item.ParticipantCount)
         ))
         .OrderBy(item => item.Date)
         .Select(item =>
            new DateOption(
               DateDisplay.Format(item.Date),
               FormatDateOptionDayLabel(
                  item.Date,
                  todayDate
               ),
               FormatDateOptionDateLabel(item.Date),
               item.ParticipantCount,
               item.Date == selectedDate
            )
         )
         .ToList();
   }

   private static string FormatDateOptionDayLabel(
      DateOnly date,
      DateOnly todayDate
   )
   {
      var culture = CultureInfo.GetCultureInfo(
         PrimaryCountry.CultureName
      );
      var label = date == todayDate
         ? "Idag"
         : date == todayDate.AddDays(1)
            ? "Imorgon"
            : culture.TextInfo.ToTitleCase(
               date.ToString("dddd", culture)
            );
      return label;
   }

   private static string FormatDateOptionDateLabel(DateOnly date)
   {
      var culture = CultureInfo.GetCultureInfo(
         PrimaryCountry.CultureName
      );

      return date.ToString(
         "d MMMM",
         culture
      );
   }

}

public sealed record ActivityAgendaSection(
   string TimeLabel,
   IReadOnlyList<ActivityListItem> Activities,
   string RelatedOrganizationEntities,
   ActivityDayPhase DayPhase,
   string ClockHourAngle,
   string ClockMinuteAngle,
   string? EndTimeLabel,
   bool IsOngoing,
   bool HasEnded,
   string? ActivityGroupTitle,
   IReadOnlyList<ActivityAgendaSlot> Slots,
   ActivityAgendaSlot TimelineSlot
);

public sealed record ActivityAgendaSlot(
   ActivityListItem Activity,
   string TimeLabel,
   string? EndTimeLabel,
   bool IsOngoing,
   bool HasEnded
);

public enum ActivityDayPhase
{
   Morning,
   Day,
   Evening,
   Night
}

public sealed record DateOption(
   string Value,
   string DayLabel,
   string DateLabel,
   int ParticipantCount,
   bool IsSelected
)
{
   public string Label => $"{DayLabel} {DateLabel}";
}

public sealed record SportParticipantCount(
   string SportId,
   string SportName,
   int ParticipantCount
);
