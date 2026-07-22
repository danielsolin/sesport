using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data;
using SESport.Web.Pages;

namespace SESport.Web.Services;

public sealed class PublicActivityTimelineBuilder
{
   public PublicActivityTimelineViewModel Build(
      IEnumerable<ActivityListItem> activities,
      DateOnly selectedDate,
      DateTimeOffset now
   )
   {
      var sportToday = SportDay.GetSportDate(now);
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
            Section = CreateSection(activity)
         })
         .OrderBy(item => item.Start)
         .ThenBy(item => item.Activity.TimeText, StringComparer.Ordinal)
         .ThenBy(item => item.Activity.Title, StringComparer.OrdinalIgnoreCase)
         .ToList();

      var timelineEntries = new List<PublicActivityTimelineEntry>();

      if(selectedDate == sportToday && timedSections.Count > 0)
      {
         var localNow = TimeZoneHelper.ToLocal(now, SportDay.TimeZoneId);
         var marker =
            PublicActivityTimelineEntry.Current($"Nu {localNow:HH:mm}");
         var markerInserted = false;

         foreach(var timedSection in timedSections)
         {
            if(!markerInserted && timedSection.Start >= now)
            {
               timelineEntries.Add(marker);
               markerInserted = true;
            }

            timelineEntries.Add(
               PublicActivityTimelineEntry.Activity(timedSection.Section)
            );
         }

         if(!markerInserted)
         {
            timelineEntries.Add(marker);
         }
      }
      else
      {
         foreach(var timedSection in timedSections)
         {
            timelineEntries.Add(
               PublicActivityTimelineEntry.Activity(timedSection.Section)
            );
         }
      }

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
      ActivityListItem activity
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
         GetDayPhase(localStart.Hour)
      );
   }

   private static ActivityDayPhase GetDayPhase(int hour)
   {
      return hour >= 5 && hour < 22
         ? ActivityDayPhase.Day
         : ActivityDayPhase.Night;
   }
}

public sealed record PublicActivityTimelineViewModel(
   IReadOnlyList<PublicActivityTimelineEntry> TimelineEntries,
   IReadOnlyList<ActivityListItem> UntimedActivities,
   bool HasVisibleActivities
);

public sealed record PublicActivityTimelineEntry(
   bool IsCurrentMarker,
   string? CurrentMarkerLabel,
   ActivityAgendaSection? Section
)
{
   public static PublicActivityTimelineEntry Current(string label)
   {
      return new PublicActivityTimelineEntry(true, label, null);
   }

   public static PublicActivityTimelineEntry Activity(
      ActivityAgendaSection section
   )
   {
      return new PublicActivityTimelineEntry(false, null, section);
   }
}
