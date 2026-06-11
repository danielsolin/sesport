using SESport.Core.Formatting;

namespace SESport.Web.Services;

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
         ["date"] = DateDisplay.Format(date),
         ["sortAsc"] = sortAsc.ToString()
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
         ["date"] = DateDisplay.Format(date),
         ["sortAsc"] = sortAsc.ToString()
      };

      if(hideReplays)
      {
        routeValues["hideReplays"] = "true";
      }

      if(showHidden)
      {
         routeValues["showHidden"] = "true";
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
         ["date"] = DateDisplay.Format(date),
         ["status"] = status,
         ["sortColumn"] = sortColumn,
         ["sortAsc"] = sortAsc
      };

      AddSelectedSports(routeValues, selectedSports);
      return routeValues;
   }

   private static void AddSelectedSports(
      IDictionary<string, string?> routeValues,
      IEnumerable<string> selectedSports
   )
   {
      var normalizedSports = NormalizeSelectedSports(selectedSports);

      for(var index = 0; index < normalizedSports.Count; index++)
      {
         routeValues[$"SelectedSports[{index}]"] = normalizedSports[index];
      }
   }

   private static void AddSelectedSports(
      IDictionary<string, object?> routeValues,
      IEnumerable<string> selectedSports
   )
   {
      var normalizedSports = NormalizeSelectedSports(selectedSports);

      for(var index = 0; index < normalizedSports.Count; index++)
      {
         routeValues[$"SelectedSports[{index}]"] = normalizedSports[index];
      }
   }

   private static List<string> NormalizeSelectedSports(
      IEnumerable<string> values
   )
   {
      return values
         .Where(value => !string.IsNullOrWhiteSpace(value))
         .Select(value => value.Trim())
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
         .ToList();
   }
}
