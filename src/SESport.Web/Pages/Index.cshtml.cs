using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages;

public class IndexModel(ActivityRepository repository) : PageModel
{
   public const string TodayDay = "Today";
   public const string TomorrowDay = "Tomorrow";

   public IReadOnlyList<ActivityListItem> Activities { get; private set; } =
      [];

   public IReadOnlyList<ActivityAgendaSection> AgendaSections
   {
      get; private set;
   } = [];

   public IReadOnlyList<ActivityListItem> UntimedActivities { get; private set; }
      = [];

   [BindProperty(SupportsGet = true, Name = "day")]
   public string? Day { get; set; } = TodayDay;

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      Day = NormalizeDay(Day) ?? TodayDay;

      try
      {
         Activities = Day switch
         {
            TodayDay => await repository.GetTodaysAsync(
               cancellationToken
            ),
            TomorrowDay => await repository.GetTomorrowsAsync(
               cancellationToken
            ),
            _ => await repository.GetTodaysAsync(cancellationToken)
         };
         BuildAgendaSections();
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   private static string? NormalizeDay(string? day)
   {
      return day switch
      {
         TodayDay => TodayDay,
         TomorrowDay => TomorrowDay,
         "" => TodayDay,
         null => TodayDay,
         _ => null
      };
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
            group.Select(activity => activity.RelatedOrganization)
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

   private static bool HasLocalStartTime(ActivityListItem activity)
   {
      return activity.TimeText.Contains(' ');
   }
}

public sealed record ActivityAgendaSection(
   string TimeLabel,
   IReadOnlyList<ActivityListItem> Activities,
   string RelatedOrganization
);
