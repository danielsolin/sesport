using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Models;

namespace SESport.Web.Pages;

public class IndexModel(
   ActivityRepository repository,
   MemberWatchRepository memberWatchRepository,
   PublicActivityTimelineBuilder timelineBuilder,
   PublicSiteOptions publicSiteOptions,
   BroadcastChannelLinkRepository channelLinkRepository
) : PageModel
{
   private static readonly CultureInfo PrimaryCountryCulture =
      CultureInfo.GetCultureInfo(PrimaryCountry.CultureName);

   public PublicSiteOptions PublicSiteOptions { get; } = publicSiteOptions;

   public BroadcastChannelLinkCatalog ChannelLinkCatalog
   {
      get;
      private set;
   } = new([]);

   public IReadOnlyList<PublicActivityTimelineEntry> TimelineEntries
   {
      get; private set;
   } = [];

   public bool HasVisibleActivities { get; private set; }

   public IReadOnlyList<DateOption> DateOptions { get; private set; } = [];

   [BindProperty(SupportsGet = true, Name = RouteKeys.Date)]
   public string? Date { get; set; }

    [BindProperty(SupportsGet = true, Name = RouteKeys.Sport)]
    public string? Sport { get; set; }

    [BindProperty(SupportsGet = true, Name = RouteKeys.Country)]
    public string? Country { get; set; }

    [BindProperty(SupportsGet = true, Name = RouteKeys.Watched)]
    public bool Watched { get; set; }

   public bool IsWatchedActivitiesView { get; private set; }

   public bool IsMember { get; private set; }

   public DateOnly SelectedDate { get; private set; }

   public bool IsSportToday { get; private set; }

   public DateOnly CurrentDate { get; private set; }

   public DateOnly TomorrowDate { get; private set; }

   public string NextDateLinkLabel { get; private set; } = string.Empty;

   public bool HasPublishedActivitiesTomorrow { get; private set; }

   public int? TotalParticipantsCount { get; private set; }

   public IReadOnlyList<SportParticipantCount> SportParticipantCounts
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      CancellationToken cancellationToken
   )
   {
      var now = DateTimeOffset.UtcNow;
      CurrentDate = DateOnly.FromDateTime(
         TimeZoneHelper.ToLocal(now, SportDay.TimeZoneId).DateTime
      );
      var sportToday = SportDay.Today(now).StartDate;
      SelectedDate = ParseDate(Date) ?? sportToday;
      TomorrowDate = SelectedDate.AddDays(1);
      NextDateLinkLabel = FormatNextDateLinkLabel(
         TomorrowDate,
         sportToday
      );
      IsSportToday = SelectedDate == sportToday;
      IsWatchedActivitiesView = Watched || string.Equals(
         HttpContext.Request.Path.Value,
         PublicRoutePaths.Watched,
         StringComparison.OrdinalIgnoreCase
      );
      if(string.Equals(
            HttpContext.Request.Path.Value,
            PublicRoutePaths.Watched,
            StringComparison.OrdinalIgnoreCase
         ) &&
         HttpContext.Request.Query.ContainsKey(RouteKeys.Date))
      {
          var sport = HttpContext.Request.Query[
             RouteKeys.Sport
          ].FirstOrDefault()?.Trim();
          var country = HttpContext.Request.Query[
             RouteKeys.Country
          ].FirstOrDefault()?.Trim();
          var redirectUrl = PublicRoutePaths.Watched;
          if(!string.IsNullOrWhiteSpace(sport))
          {
             redirectUrl += "?sport=" + Uri.EscapeDataString(sport);
             if(!string.IsNullOrWhiteSpace(country))
             {
                redirectUrl += "&" + RouteKeys.Country + "=" +
                   Uri.EscapeDataString(country);
             }
          }

          return Redirect(redirectUrl);
      }
      var memberId = TryGetMemberId();
      IsMember = memberId is not null;
      // Keep the date selector renderable if loading published counts fails.
      DateOptions = BuildDateOptions(sportToday, SelectedDate, []);

      try
      {
         ChannelLinkCatalog = new BroadcastChannelLinkCatalog(
            await channelLinkRepository.GetActiveDefinitionsAsync(
               cancellationToken
            )
         );
         var publishedDateCounts =
            await repository.GetPublishedDateParticipantCountsFromAsync(
               sportToday,
               cancellationToken
            );
         HasPublishedActivitiesTomorrow = ShouldShowTomorrowLink(
            SelectedDate,
            publishedDateCounts
         );
         DateOptions = BuildDateOptions(
            sportToday,
            SelectedDate,
            publishedDateCounts
         );

         if(IsWatchedActivitiesView)
         {
            HasPublishedActivitiesTomorrow = false;
            if(memberId is null)
            {
               PublicFilterPreferenceStore.Save(
                  HttpContext.Response,
                  SelectedDate,
                  null,
                  watched: true
               );
               return Page();
            }

            var watchedActivities =
               await repository.GetPublishedFutureForMemberWatchesAsync(
                  memberId.Value,
                  now,
                  cancellationToken
               );
            TotalParticipantsCount =
               await memberWatchRepository.GetWatchedEntityCountAsync(
                  memberId.Value,
                  cancellationToken
               );
            var allWatchedTimeline = timelineBuilder.BuildFuture(
               watchedActivities,
               now
            );
            SportParticipantCounts = CountActivityCardsBySport(
               allWatchedTimeline
            );
             Sport = NormalizeSportFilter(
                Sport,
                SportParticipantCounts
             );
             Country = NormalizeCountryFilter(
                Country,
                GetSelectedSportCount(Sport)
             );
             PublicFilterPreferenceStore.Save(
                HttpContext.Response,
                SelectedDate,
                Sport,
                watched: true,
                Country
             );
             var filteredWatchedActivities = FilterActivitiesBySport(
                watchedActivities,
                Sport,
                Country
             );
            var watchedTimeline = timelineBuilder.BuildFuture(
               filteredWatchedActivities,
               now
            );
            TimelineEntries = watchedTimeline.TimelineEntries;
            HasVisibleActivities = watchedTimeline.HasVisibleActivities;
            return Page();
         }

         var activities = await repository.GetPublishedForDateAsync(
            SelectedDate,
            cancellationToken,
            memberId
         );
         TotalParticipantsCount = CountParticipants(activities);
         SportParticipantCounts =
            CountParticipantsBySport(activities);
          Sport = NormalizeSportFilter(
             Sport,
             SportParticipantCounts
          );
          Country = NormalizeCountryFilter(
             Country,
             GetSelectedSportCount(Sport)
          );
          PublicFilterPreferenceStore.Save(
             HttpContext.Response,
             SelectedDate,
             Sport,
             watched: false,
             Country
          );
          var filteredActivities = FilterActivitiesBySport(
             activities,
             Sport,
             Country
          );
         var timeline = timelineBuilder.Build(
            filteredActivities,
            SelectedDate,
            now
         );
         TimelineEntries = timeline.TimelineEntries;
         HasVisibleActivities = timeline.HasVisibleActivities;
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
      }

      return Page();
   }

   private Guid? TryGetMemberId()
   {
      var memberIdValue = User.FindFirstValue(
         MemberClaimTypes.MemberId
      );
      return Guid.TryParse(memberIdValue, out var memberId)
         ? memberId
         : null;
   }

   internal static int CountParticipants(
      IEnumerable<ActivityListItem> activities
   )
   {
      return activities
         .SelectMany(activity => activity.ActiveRelatedPersonEntityIds)
         .Where(entityId => entityId != Guid.Empty)
         .Distinct()
         .Count();
   }

   internal static bool ShouldShowTomorrowLink(
      DateOnly selectedDate,
      IEnumerable<PublishedDateParticipantCount> publishedDateCounts
   )
   {
      var tomorrowDate = selectedDate.AddDays(1);
      return publishedDateCounts.Any(item => item.Date == tomorrowDate);
   }

   internal static string FormatNextDateLinkLabel(
      DateOnly targetDate,
      DateOnly todayDate
   )
   {
      var dayLabel = FormatDateOptionDayLabel(targetDate, todayDate);
      var dateLabel = FormatDateOptionDateLabel(targetDate);
      return $"{dayLabel} {dateLabel}";
   }

   internal static bool ShouldShowDisciplineColumn(
      IEnumerable<PublicActivityParticipant> participants
   )
   {
      var disciplineValues = participants
         .Select(
            participant => participant.DisciplineAliasName ?? string.Empty
         )
         .Distinct(StringComparer.Ordinal);

      return disciplineValues.Skip(1).Any();
   }

   internal static bool ShouldHideRepresentedEntityColumn(
      IReadOnlyList<PublicActivityParticipant> participants
   )
   {
      if(participants.Count == 0)
      {
         return false;
      }

      var representedEntityIds = participants
         .Select(participant => participant.RepresentedEntityId)
         .Distinct()
         .ToArray();

      return representedEntityIds.Length == 1 &&
         representedEntityIds[0] is not null &&
         participants.All(
            participant =>
               participant.HasNonNationalTeamRepresentation &&
               string.Equals(
                  participant.RepresentedEntityCountryId,
                  PrimaryCountry.Id,
                  StringComparison.OrdinalIgnoreCase
               )
         );
   }

   internal static bool ShouldAutoExpandPastActivities(
      bool isSportToday,
      bool hasPastActivities,
      bool hasActiveOrUpcomingActivities
   )
   {
      return isSportToday &&
         hasPastActivities &&
         !hasActiveOrUpcomingActivities;
   }

   internal static bool ShouldCollapseInactiveParticipants(
      int activeParticipantCount,
      bool hasInactiveParticipants
   )
   {
      return hasInactiveParticipants && activeParticipantCount > 0;
   }

   internal static bool ShouldCombineParticipantToggles(
      bool shouldCollapseParticipants,
      bool hasInactiveParticipants
   )
   {
      return shouldCollapseParticipants && hasInactiveParticipants;
   }

   internal static IReadOnlyList<SportParticipantCount>
      CountParticipantsBySport(
         IEnumerable<ActivityListItem> activities
      )
   {
      return CountBySport(
         activities,
         group => group
            .SelectMany(activity => activity.ActiveRelatedPersonEntityIds)
            .Where(entityId => entityId != Guid.Empty)
            .Distinct()
            .Count()
      );
   }

   internal static IReadOnlyList<SportParticipantCount>
      CountActivitiesBySport(
         IEnumerable<ActivityListItem> activities
      )
   {
      return CountBySport(
         activities,
         group => group.Count()
      );
   }

   internal static IReadOnlyList<SportParticipantCount>
      CountActivityCardsBySport(
         PublicActivityTimelineViewModel timeline
      )
   {
      var cardActivities = timeline.TimelineEntries
         .Where(entry => entry.Section is not null)
         .Select(entry => entry.Section!.Activities[0]);
      return CountActivitiesBySport(cardActivities);
   }

    private static IReadOnlyList<SportParticipantCount> CountBySport(
       IEnumerable<ActivityListItem> activities,
       Func<IEnumerable<ActivityListItem>, int> countSelector
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
             countSelector(group),
             CountCountriesBySport(group, countSelector)
          ))
          .OrderBy(
             item => item.SportName,
             StringComparer.OrdinalIgnoreCase
          )
          .ToArray();
    }

    private static IReadOnlyList<SportCountryParticipantCount>
       CountCountriesBySport(
          IGrouping<string, ActivityListItem> activities,
          Func<IEnumerable<ActivityListItem>, int> countSelector
       )
    {
       if(!activities
            .All(activity =>
               !string.IsNullOrWhiteSpace(
                  activity.OrganizationCountryId
               )
            )
       )
       {
          return [];
       }

       return activities
          .GroupBy(
             activity => activity.OrganizationCountryId!
                .Trim()
                .ToLowerInvariant(),
             StringComparer.OrdinalIgnoreCase
          )
          .Select(group => new SportCountryParticipantCount(
             group.Key,
             countSelector(group)
          ))
          .OrderByDescending(country => country.ParticipantCount)
          .ThenBy(
             country => country.CountryId,
             StringComparer.OrdinalIgnoreCase
          )
          .ToArray();
    }

    internal static IReadOnlyList<ActivityListItem>
       FilterActivitiesBySport(
          IEnumerable<ActivityListItem> activities,
          string? sportId,
          string? countryId = null
       )
    {
       if(string.IsNullOrWhiteSpace(sportId))
       {
          return activities.ToArray();
       }

       var filtered = activities
          .Where(activity => string.Equals(
             activity.SportId,
             sportId,
             StringComparison.OrdinalIgnoreCase
          ));
       if(!string.IsNullOrWhiteSpace(countryId))
       {
          var normalizedCountryId = countryId.Trim();
          filtered = filtered
             .Where(activity => string.Equals(
                activity.OrganizationCountryId?.Trim(),
                normalizedCountryId,
                StringComparison.OrdinalIgnoreCase
             ));
       }

       return filtered.ToArray();
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

    private SportParticipantCount? GetSelectedSportCount(
       string? sportId
    )
    {
       if(string.IsNullOrWhiteSpace(sportId))
       {
          return null;
       }

       return SportParticipantCounts
          .FirstOrDefault(sport => string.Equals(
             sport.SportId,
             sportId,
             StringComparison.OrdinalIgnoreCase
          ));
    }

    private static string? NormalizeCountryFilter(
       string? countryId,
       SportParticipantCount? sportCount
    )
    {
       if(string.IsNullOrWhiteSpace(countryId))
       {
          return null;
       }

       return sportCount?.Countries
          .Select(country => country.CountryId)
          .FirstOrDefault(id => string.Equals(
             id,
             countryId.Trim(),
             StringComparison.OrdinalIgnoreCase
          ));
    }

   internal static int? CalculateAge(DateOnly? birthdate, DateOnly today)
   {
      if(birthdate is null || birthdate.Value > today)
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
      var label = date == todayDate
         ? "Idag"
         : date == todayDate.AddDays(1)
            ? "Imorgon"
            : PrimaryCountryCulture.TextInfo.ToTitleCase(
               date.ToString("dddd", PrimaryCountryCulture)
            );
      return label;
   }

   private static string FormatDateOptionDateLabel(DateOnly date)
   {
      return date.ToString(
         "d MMMM",
         PrimaryCountryCulture
      );
   }

}

public sealed record ActivityAgendaSection(
   string TimeLabel,
   IReadOnlyList<ActivityListItem> Activities,
   IReadOnlyList<PublicActivityParticipant> Participants,
   string RelatedOrganizationEntities,
   ActivityDayPhase DayPhase,
   string? EndTimeLabel,
   bool IsOngoing,
   bool HasEnded,
   string? ActivityGroupTitle,
   string DisplayTitle,
   IReadOnlyList<ActivityAgendaSlot> Slots,
   ActivityAgendaSlot TimelineSlot
);

public sealed record ActivityAgendaSlot(
   ActivityListItem Activity,
   string StartTimeLabel,
   string? EndTimeLabel,
   bool IsOngoing,
   bool HasEnded,
   IReadOnlyList<string> TvChannels,
   bool ShowParticipantNames
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
   int ParticipantCount,
   IReadOnlyList<SportCountryParticipantCount> Countries
);

public sealed record SportCountryParticipantCount(
   string CountryId,
   int ParticipantCount
);
