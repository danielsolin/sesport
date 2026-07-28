using SESport.Core.Domain;
using SESport.Core.Formatting;

namespace SESport.Web.Routing;

internal static class AdminRouteValueBuilder
{
   public static Dictionary<string, string?> CreateSortRouteValues(
      DateOnly date,
      bool sortAsc,
      IEnumerable<string> selectedSports
   )
   {
      var routeValues = new Dictionary<string, string?>
      {
         [RouteKeys.Date] = DateDisplay.Format(date),
         [RouteKeys.SortAsc] = sortAsc.ToString()
      };

      AddSelectedSports(routeValues, selectedSports);
      return routeValues;
   }

   public static Dictionary<string, string?> CreateSortRouteValues(
      DateOnly date,
      bool hideReplays,
      bool showHidden,
      bool sortAsc,
      IEnumerable<string> selectedSports
   )
   {
      var routeValues = new Dictionary<string, string?>
      {
         [RouteKeys.Date] = DateDisplay.Format(date),
         [RouteKeys.SortAsc] = sortAsc.ToString()
      };

      if(hideReplays)
      {
         routeValues[RouteKeys.HideReplays] = "true";
      }

      if(showHidden)
      {
         routeValues[RouteKeys.ShowHidden] = "true";
      }

      AddSelectedSports(routeValues, selectedSports);
      return routeValues;
   }

   public static Dictionary<string, object?> CreateActivityRedirectRouteValues(
      DateOnly date,
      string status,
      string sortColumn,
      bool sortAsc,
      IEnumerable<string> selectedSports
   )
   {
      var routeValues = new Dictionary<string, object?>
      {
         [RouteKeys.Date] = DateDisplay.Format(date),
         [RouteKeys.Status] = status,
         [RouteKeys.SortColumn] = sortColumn,
         [RouteKeys.SortAsc] = sortAsc
      };

      AddSelectedSports(routeValues, selectedSports);
      return routeValues;
   }

   private static void AddSelectedSports(
      IDictionary<string, string?> routeValues,
      IEnumerable<string> selectedSports
   )
   {
      var normalizedSports = SportFilter.Normalize(selectedSports);

      for(var index = 0; index < normalizedSports.Count; index++)
      {
         routeValues[$"{RouteKeys.SelectedSports}[{index}]"] =
            normalizedSports[index];
      }
   }

   private static void AddSelectedSports(
      IDictionary<string, object?> routeValues,
      IEnumerable<string> selectedSports
   )
   {
      var normalizedSports = SportFilter.Normalize(selectedSports);

      for(var index = 0; index < normalizedSports.Count; index++)
      {
         routeValues[$"{RouteKeys.SelectedSports}[{index}]"] =
            normalizedSports[index];
      }
   }

}
