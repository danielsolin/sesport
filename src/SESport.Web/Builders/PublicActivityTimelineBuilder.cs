using System.Globalization;

using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Models;
using SESport.Web.Formatting;
using SESport.Web.Pages;

namespace SESport.Web.Builders;

public sealed class PublicActivityTimelineBuilder
{
   public PublicActivityTimelineViewModel Build(
      IEnumerable<ActivityListItem> activities,
      DateOnly selectedDate,
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

      var groupedActivityIds = timedActivities
         .Where(activity =>
            activity.ActivityGroupId is not null &&
            !activity.IsTeamSport &&
            !activity.NoGrouping
         )
         .GroupBy(activity => new
         {
            DisplayDate = ActivityDisplayDateResolver.Resolve(
               activity.StartsAt!.Value,
               activity.PublicDateMode
            ),
            GroupId = activity.ActivityGroupId!.Value
         })
         .Where(group => group.Count() > 1)
         .SelectMany(group => group.Select(activity => activity.Id))
         .ToHashSet();

      var groupedSections = timedActivities
         .Where(activity => groupedActivityIds.Contains(activity.Id))
         .GroupBy(activity => new
         {
            DisplayDate = ActivityDisplayDateResolver.Resolve(
               activity.StartsAt!.Value,
               activity.PublicDateMode
            ),
            GroupId = activity.ActivityGroupId!.Value
         })
         .Select(group => CreateSection(group.ToList(), now));
      var individualSections = timedActivities
         .Where(activity => !groupedActivityIds.Contains(activity.Id))
         .Select(activity => CreateSection([activity], now));

      var timedSections = groupedSections
         .Concat(individualSections)
         .OrderBy(section => GetTimelineOrder(section, now))
         .ThenBy(section => section.TimelineSlot.Activity.StartsAt)
         .ThenBy(section => section.TimeLabel, StringComparer.Ordinal)
         .ThenBy(
            section => section.ActivityGroupTitle ??
               section.Activities[0].Title,
            StringComparer.OrdinalIgnoreCase
         )
         .ToList();

      var timelineEntries = CreateTimelineEntries(
         timedSections,
         selectedDate,
         now
      );

      return new PublicActivityTimelineViewModel(
         timelineEntries,
         untimedActivities,
         visibleActivities.Count > 0
      );
   }

   private static IReadOnlyList<PublicActivityTimelineEntry>
      CreateTimelineEntries(
         IReadOnlyList<ActivityAgendaSection> timedSections,
         DateOnly selectedDate,
         DateTimeOffset now
      )
   {
      var entries = new List<PublicActivityTimelineEntry>();
      var showCurrentMarker =
         selectedDate == SportDay.GetSportDate(now) &&
         timedSections.Count > 0;
      var markerInserted = false;

      foreach(var section in timedSections)
      {
         if(
            showCurrentMarker &&
            !markerInserted &&
            !IsBeforeCurrentMarker(section, now)
         )
         {
            entries.Add(CreateCurrentMarker(now));
            markerInserted = true;
         }

         entries.Add(new PublicActivityTimelineEntry(null, section));
      }

      if(showCurrentMarker && !markerInserted)
      {
         entries.Add(CreateCurrentMarker(now));
      }

      return entries;
   }

   private static int GetTimelineOrder(
      ActivityAgendaSection section,
      DateTimeOffset now
   )
   {
      if(IsBeforeCurrentMarker(section, now))
      {
         return 0;
      }

      return section.IsOngoing ? 1 : 2;
   }

   private static bool IsBeforeCurrentMarker(
      ActivityAgendaSection section,
      DateTimeOffset now
   )
   {
      return section.HasEnded ||
         !section.IsOngoing &&
         section.TimelineSlot.Activity.StartsAt < now;
   }

   private static PublicActivityTimelineEntry CreateCurrentMarker(
      DateTimeOffset now
   )
   {
      return new PublicActivityTimelineEntry(
         PublicTimeDisplay.FormatCurrentMarker(now),
         null
      );
   }

   private static bool HasLocalStartTime(ActivityListItem activity)
   {
      return activity.TimeText.Contains(' ');
   }

   private static ActivityAgendaSection CreateSection(
      IReadOnlyList<ActivityListItem> activities,
      DateTimeOffset now
   )
   {
      var orderedActivities = activities
         .OrderBy(activity => activity.StartsAt)
         .ThenBy(activity => activity.Title, StringComparer.OrdinalIgnoreCase)
         .Select(activity => activity with
         {
            Participants = OrderParticipants(activity.Participants)
         })
         .ToList();
      var activity = orderedActivities[0];
      var participants = MergeParticipants(orderedActivities);
      var slots = orderedActivities
         .Select(item => CreateSlot(item, now))
         .ToList();
      var timelineSlot = slots
         .FirstOrDefault(slot => slot.IsOngoing)
         ?? slots.FirstOrDefault(slot => slot.Activity.StartsAt >= now)
         ?? slots[^1];
      var timelineStart = TimeZoneHelper.ToLocal(
         timelineSlot.Activity.StartsAt!.Value,
         SportDay.TimeZoneId
      );
      var timelineTime = TimeOnly.FromDateTime(timelineStart.DateTime);
      var timelineTimeLabel =
         PublicTimeDisplay.FormatApproximateTimeText(
            timelineSlot.Activity.TimeText,
            timelineSlot.Activity.LocalStartTime
         );
      var timelineEndTimeLabel =
         PublicTimeDisplay.FormatApproximateTime(
            timelineSlot.Activity.LocalEndTime
         );

      return new ActivityAgendaSection(
         timelineTimeLabel,
         orderedActivities,
         participants,
         activity.RelatedOrganizationEntities,
         GetDayPhase(timelineStart.Hour),
         GetHourHandAngle(timelineTime),
         $"{timelineTime.Minute * 6}deg",
         timelineEndTimeLabel,
         slots.Any(slot => slot.IsOngoing),
         slots.All(slot => slot.HasEnded),
         orderedActivities.Count > 1
            ? activity.ActivityGroupTitle
            : null,
         slots,
         timelineSlot
      );
   }

   private static IReadOnlyList<PublicActivityParticipant>
      MergeParticipants(
         IEnumerable<ActivityListItem> activities
      )
   {
      var mergedParticipants = activities
         .SelectMany(activity =>
            activity.Participants.Select(participant => new
            {
               Activity = activity,
               Participant = participant
            })
         )
         .GroupBy(item => item.Participant.Id)
         .Select(group => group
            .OrderByDescending(item =>
               !string.IsNullOrWhiteSpace(item.Participant.StartTime)
            )
            .ThenBy(item => item.Activity.StartsAt)
            .ThenBy(
               item => item.Activity.Title,
               StringComparer.OrdinalIgnoreCase
            )
            .First()
            .Participant
         );

      return OrderParticipants(mergedParticipants);
   }

   private static ActivityAgendaSlot CreateSlot(
      ActivityListItem activity,
      DateTimeOffset now
   )
   {
      return new ActivityAgendaSlot(
         activity,
         PublicTimeDisplay.FormatExactTimeText(
            activity.TimeText,
            activity.LocalStartTime
         ),
         PublicTimeDisplay.FormatExactTime(activity.LocalEndTime),
         activity.EndsAt is not null &&
            activity.StartsAt <= now &&
            activity.EndsAt > now,
         activity.EndsAt is not null && activity.EndsAt <= now,
         SplitTvChannelNames(activity.TvChannelName)
      );
   }

   private static IReadOnlyList<string> SplitTvChannelNames(
      string? tvChannelName
   )
   {
      return (tvChannelName ?? string.Empty)
         .Split(
            ',',
            StringSplitOptions.TrimEntries |
               StringSplitOptions.RemoveEmptyEntries
         )
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   private static IReadOnlyList<PublicActivityParticipant>
      OrderParticipants(
         IEnumerable<PublicActivityParticipant> participants
      )
   {
      return participants
         .Select((participant, index) => new
         {
            Participant = participant,
            Index = index,
            StartTime = ParseParticipantStartTime(
               participant.StartTime
            )
         })
         .OrderBy(item => item.StartTime is null)
         .ThenBy(item => item.StartTime)
         .ThenBy(item => item.Index)
         .Select(item => item.Participant)
         .ToList();
   }

   private static int? ParseParticipantStartTime(string? value)
   {
      return TimeOnly.TryParseExact(
         value?.Trim(),
         ["H:mm", "HH:mm", "H.mm", "HH.mm"],
         CultureInfo.InvariantCulture,
         DateTimeStyles.None,
         out var time
      )
         ? time.Hour * 60 + time.Minute
         : null;
   }

   private static string GetHourHandAngle(TimeOnly localStart)
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
   string? CurrentMarkerLabel,
   ActivityAgendaSection? Section
)
{
   public bool IsCurrentMarker => CurrentMarkerLabel is not null;
}
