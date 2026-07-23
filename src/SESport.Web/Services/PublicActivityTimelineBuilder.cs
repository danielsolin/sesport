using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data;
using SESport.Web.Pages;

namespace SESport.Web.Services;

public sealed class PublicActivityTimelineBuilder
{
   public PublicActivityTimelineViewModel Build(
      IEnumerable<ActivityListItem> activities,
      DateTimeOffset now
   )
   {
      var visibleActivities = activities.ToList();

      var timedActivities = visibleActivities
         .Where(HasLocalStartTime)
         .ToList();
      var untimedActivities = visibleActivities
         .Where(activity => !HasLocalStartTime(activity))
         .ToList();

      var timedSections = timedActivities
         .Select(activity => new
         {
            Start = activity.StartsAt ?? DateTimeOffset.MaxValue,
            Activity = activity,
            Section = CreateSection(activity, now)
         })
         .OrderBy(item => item.Start)
         .ThenBy(item => item.Activity.TimeText, StringComparer.Ordinal)
         .ThenBy(item => item.Activity.Title, StringComparer.OrdinalIgnoreCase)
         .ToList();

      var timelineEntries = timedSections
         .Select(item => new PublicActivityTimelineEntry(item.Section))
         .ToList();

      return new PublicActivityTimelineViewModel(
         timelineEntries,
         untimedActivities,
         visibleActivities.Count > 0
      );
   }

   private static bool HasLocalStartTime(ActivityListItem activity)
   {
      return activity.TimeText.Contains(' ');
   }

   private static ActivityAgendaSection CreateSection(
      ActivityListItem activity,
      DateTimeOffset now
   )
   {
      var localStart = TimeZoneHelper.ToLocal(
         activity.StartsAt!.Value,
         SportDay.TimeZoneId
      );

      return new ActivityAgendaSection(
         TimeTextFormatter.FormatTimeOnlyText(activity.TimeText),
         [activity],
         activity.RelatedOrganizationEntities,
         GetDayPhase(localStart.Hour),
         GetHourHandAngle(localStart),
         $"{localStart.Minute * 6}deg",
         activity.LocalEndTime?.ToString("HH:mm"),
         activity.EndsAt is not null &&
            activity.StartsAt <= now &&
            activity.EndsAt > now,
         activity.EndsAt is not null && activity.EndsAt <= now
      );
   }

   private static string GetHourHandAngle(DateTimeOffset localStart)
   {
      var angle = (localStart.Hour % 12 * 30) +
         (localStart.Minute * 0.5);

      return FormattableString.Invariant($"{angle:0.#}deg");
   }

   private static ActivityDayPhase GetDayPhase(int hour)
   {
      return hour switch
      {
         >= 6 and < 10 => ActivityDayPhase.Morning,
         >= 10 and < 18 => ActivityDayPhase.Day,
         >= 18 and < 22 => ActivityDayPhase.Evening,
         _ => ActivityDayPhase.Night
      };
   }
}

public sealed record PublicActivityTimelineViewModel(
   IReadOnlyList<PublicActivityTimelineEntry> TimelineEntries,
   IReadOnlyList<ActivityListItem> UntimedActivities,
   bool HasVisibleActivities
);

public sealed record PublicActivityTimelineEntry(
   ActivityAgendaSection Section
);
