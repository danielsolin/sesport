using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Models;
using SESport.Web.Formatting;
using SESport.Web.Pages;
using System.Globalization;
using System.Text;

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
      var timelineEntries = CreateTimelineEntries(
         CreateTimedSections(timedActivities, now),
         selectedDate,
         now
      );

      return new PublicActivityTimelineViewModel(
         timelineEntries,
         visibleActivities
            .Where(activity => !HasLocalStartTime(activity))
            .ToList(),
         visibleActivities.Count > 0
      );
   }

   public PublicActivityTimelineViewModel BuildFuture(
      IEnumerable<ActivityListItem> activities,
      DateTimeOffset now
   )
   {
      var visibleActivities = activities.ToList();
      var timedActivities = visibleActivities
         .Where(HasLocalStartTime)
         .ToList();
      var timelineEntries = CreateFutureTimelineEntries(
         CreateTimedSections(timedActivities, now),
         SportDay.GetSportDate(now)
      );

      return new PublicActivityTimelineViewModel(
         timelineEntries,
         visibleActivities
            .Where(activity => !HasLocalStartTime(activity))
            .ToList(),
         visibleActivities.Count > 0
      );
   }

   private static IReadOnlyList<ActivityAgendaSection> CreateTimedSections(
      IReadOnlyList<ActivityListItem> timedActivities,
      DateTimeOffset now
   )
   {
      var groupedActivityGroups = timedActivities
         .Where(activity =>
            activity.ActivityGroupId is not null &&
            !activity.NoGrouping
         )
         .GroupBy(activity => new
         {
            DisplayDate = ActivityDisplayDateResolver.Resolve(
               activity.StartsAt!.Value,
               activity.PublicDateMode
            ),
            GroupId = activity.ActivityGroupId!.Value,
            TeamSportTitle = activity.IsTeamSport
               ? NormalizeActivityTitleForGrouping(activity.Title)
               : null
         })
         .Where(group => group.Count() > 1)
         .ToList();
      var groupedActivityIds = groupedActivityGroups
         .SelectMany(group => group.Select(activity => activity.Id))
         .ToHashSet();

      var groupedSections = groupedActivityGroups
         .Select(group =>
         {
            var activities = group.ToList();
            var isInvisibleChannelVariantGroup =
               group.Key.TeamSportTitle is null &&
               IsInvisibleChannelVariantGroup(activities);

            return CreateSection(
               activities,
               now,
               showGroupedLayout: !isInvisibleChannelVariantGroup &&
                  group.Key.TeamSportTitle is null
            );
         });
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

      return timedSections;
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

   private static IReadOnlyList<PublicActivityTimelineEntry>
      CreateFutureTimelineEntries(
         IReadOnlyList<ActivityAgendaSection> timedSections,
         DateOnly todayDate
      )
   {
      var entries = new List<PublicActivityTimelineEntry>();
      DateOnly? previousDisplayDate = null;

      foreach(var section in timedSections)
      {
         var displayDate = ActivityDisplayDateResolver.Resolve(
            section.TimelineSlot.Activity.StartsAt!.Value,
            section.TimelineSlot.Activity.PublicDateMode
         );
         if(displayDate != previousDisplayDate)
         {
            entries.Add(
               new PublicActivityTimelineEntry(
                  null,
                  null,
                  FormatDateSeparator(displayDate, todayDate),
                  displayDate == todayDate
               )
            );
            previousDisplayDate = displayDate;
         }

         entries.Add(new PublicActivityTimelineEntry(null, section));
      }

      return entries;
   }

   private static string FormatDateSeparator(
      DateOnly date,
      DateOnly todayDate
   )
   {
      if(date == todayDate)
      {
         return "Idag";
      }

      var culture = CultureInfo.GetCultureInfo(
         PrimaryCountry.CultureName
      );
      var dayLabel = culture.TextInfo.ToTitleCase(
         date.ToString("dddd", culture)
      );
      return $"{dayLabel} {date.ToString("d MMMM", culture)}";
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
      DateTimeOffset now,
      bool showGroupedLayout = false
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
      var latestActivity = orderedActivities
         .Where(item => item.EndsAt is not null)
         .MaxBy(item => item.EndsAt);
      var participants = MergeParticipants(orderedActivities);
      var hasDifferentParticipantSets =
         HasDifferentActiveParticipantSets(orderedActivities);
      var slots = orderedActivities
         .Select(item => CreateSlot(
            item,
            now,
            hasDifferentParticipantSets &&
               !TitleContainsActiveParticipantNameParts(
                  item.Title,
                  item.Participants
               )
         ))
         .ToList();
      var timelineSlot = slots
         .FirstOrDefault(slot => slot.IsOngoing)
         ?? slots.FirstOrDefault(slot => slot.Activity.StartsAt >= now)
         ?? slots[^1];
      var timelineStart = TimeZoneHelper.ToLocal(
         activity.StartsAt!.Value,
         SportDay.TimeZoneId
      );
      var timelineTimeLabel =
         PublicTimeDisplay.FormatApproximateTimeText(
            activity.TimeText,
            activity.LocalStartTime
         );
      var timelineEndTimeLabel =
         PublicTimeDisplay.FormatApproximateTime(
            latestActivity?.LocalEndTime
         );
      var activityGroupTitle = showGroupedLayout &&
         orderedActivities.Count > 1
            ? activity.ActivityGroupTitle
            : null;

      return new ActivityAgendaSection(
         timelineTimeLabel,
         orderedActivities,
         participants,
         activity.RelatedOrganizationEntities,
         GetDayPhase(timelineStart.Hour),
         timelineEndTimeLabel,
         slots.Any(slot => slot.IsOngoing),
         slots.All(slot => slot.HasEnded),
         activityGroupTitle,
         hasDifferentParticipantSets,
         slots,
         timelineSlot
      );
   }

   private static bool HasDifferentActiveParticipantSets(
      IReadOnlyList<ActivityListItem> activities
   )
   {
      if(activities.Count < 2)
      {
         return false;
      }

      var firstParticipantIds = GetActiveParticipantIds(activities[0]);
      return activities
         .Skip(1)
         .Any(activity =>
            !firstParticipantIds.SetEquals(
               GetActiveParticipantIds(activity)
            )
         );
   }

   private static bool IsInvisibleChannelVariantGroup(
      IReadOnlyList<ActivityListItem> activities
   )
   {
      if(activities.Count < 2)
      {
         return false;
      }

      var first = activities[0];
      var channelNames = new HashSet<string>(
         StringComparer.OrdinalIgnoreCase
      );
      foreach(var activity in activities)
      {
         if(!HasSameNonBroadcastIdentity(first, activity) ||
            string.IsNullOrWhiteSpace(activity.TvChannelName))
         {
            return false;
         }

         channelNames.Add(activity.TvChannelName.Trim());
      }

      return channelNames.Count > 1;
   }

   private static bool HasSameNonBroadcastIdentity(
      ActivityListItem first,
      ActivityListItem candidate
   )
   {
      return first.ActivityGroupId == candidate.ActivityGroupId &&
         first.NoGrouping == candidate.NoGrouping &&
         string.Equals(
            NormalizeActivityTitleForGrouping(first.Title),
            NormalizeActivityTitleForGrouping(candidate.Title),
            StringComparison.Ordinal
         ) &&
         string.Equals(
            first.Description,
            candidate.Description,
            StringComparison.Ordinal
         ) &&
         string.Equals(
            first.Teaser,
            candidate.Teaser,
            StringComparison.Ordinal
         ) &&
         string.Equals(
            first.ActivityType,
            candidate.ActivityType,
            StringComparison.Ordinal
         ) &&
         string.Equals(
            first.SportId,
            candidate.SportId,
            StringComparison.Ordinal
         ) &&
         string.Equals(
            first.PublicDateMode,
            candidate.PublicDateMode,
            StringComparison.Ordinal
         ) &&
         string.Equals(
            first.RelatedPersonEntities,
            candidate.RelatedPersonEntities,
            StringComparison.Ordinal
         ) &&
         HaveSameEntityIds(
            first.RelatedPersonEntityIds,
            candidate.RelatedPersonEntityIds
         ) &&
         HaveSameEntityIds(
            first.ActiveRelatedPersonEntityIds,
            candidate.ActiveRelatedPersonEntityIds
         ) &&
         string.Equals(
            first.RelatedOrganizationEntities,
            candidate.RelatedOrganizationEntities,
            StringComparison.Ordinal
         ) &&
         string.Equals(
            first.RelatedOrganizationCanonicalEntities,
            candidate.RelatedOrganizationCanonicalEntities,
            StringComparison.Ordinal
         ) &&
         string.Equals(
            first.OrganizationCountryId,
            candidate.OrganizationCountryId,
            StringComparison.Ordinal
         ) &&
         first.HasNationalTeamRelatedOrganization ==
            candidate.HasNationalTeamRelatedOrganization;
   }

   private static bool HaveSameEntityIds(
      Guid[] firstIds,
      Guid[] candidateIds
   )
   {
      return firstIds.ToHashSet().SetEquals(candidateIds);
   }

   private static HashSet<Guid> GetActiveParticipantIds(
      ActivityListItem activity
   )
   {
      return activity.Participants
         .Where(participant => participant.IsActive)
         .Select(participant => participant.Id)
         .ToHashSet();
   }

   private static bool TitleContainsActiveParticipantNameParts(
      string title,
      IReadOnlyList<PublicActivityParticipant> participants
   )
   {
      var titleParts = GetNormalizedNameParts(title);
      var activeParticipants = participants
         .Where(participant => participant.IsActive)
         .ToList();

      return activeParticipants.Count > 0 && activeParticipants.All(
         participant => GetNormalizedNameParts(participant.Name)
            .Any(part => titleParts.Contains(part))
      );
   }

   private static string NormalizeActivityTitleForGrouping(string title)
   {
      return string.Join(
         ' ',
         title.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries |
               StringSplitOptions.TrimEntries
         )
      ).ToUpperInvariant();
   }

   private static HashSet<string> GetNormalizedNameParts(string value)
   {
      var normalized = value.Normalize(NormalizationForm.FormD);
      var builder = new StringBuilder(normalized.Length);

      foreach(var character in normalized)
      {
         if(CharUnicodeInfo.GetUnicodeCategory(character) ==
            UnicodeCategory.NonSpacingMark)
         {
            continue;
         }

         builder.Append(
            char.IsLetterOrDigit(character)
               ? char.ToUpperInvariant(character)
               : ' '
         );
      }

      return builder
         .ToString()
         .Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
               StringSplitOptions.TrimEntries
         )
         .ToHashSet(StringComparer.Ordinal);
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
      DateTimeOffset now,
      bool showParticipantNames
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
         SplitTvChannelNames(activity.TvChannelName),
         showParticipantNames
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
         .OrderByDescending(item => item.Participant.IsActive)
         .ThenBy(item => item.Participant.WatchPriority ?? int.MaxValue)
         .ThenBy(item => item.StartTime is null)
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
   ActivityAgendaSection? Section,
   string? DateSeparatorLabel = null,
   bool IsTodayDateSeparator = false
)
{
   public bool IsCurrentMarker => CurrentMarkerLabel is not null;

   public bool IsDateSeparator => DateSeparatorLabel is not null;
}
