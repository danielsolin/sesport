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
         .GroupBy(activity => activity.TimeOnlyText)
         .Select(group =>
            new
            {
               Start = group
                  .Select(activity => activity.StartsAt)
                  .Where(startsAt => startsAt is not null)
                  .Select(startsAt => startsAt!.Value)
                  .DefaultIfEmpty(DateTimeOffset.MaxValue)
                  .Min(),
               Section = CreateSection(group)
            })
         .OrderBy(item => item.Start)
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
      IGrouping<string, ActivityListItem> group
   )
   {
      var relatedOrganization = string.Join(
         ", ",
         group.Select(activity =>
            activity.RelatedOrganizationEntities)
            .Where(summary => !string.IsNullOrWhiteSpace(summary))
            .Distinct(StringComparer.Ordinal)
      );

      return new ActivityAgendaSection(
         group.Key,
         group.ToList(),
         relatedOrganization
      );
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
