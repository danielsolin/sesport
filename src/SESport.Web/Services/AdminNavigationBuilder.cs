using SESport.Data;

namespace SESport.Web.Services;

public static class AdminNavigationBuilder
{
   public static IReadOnlyList<AdminNavItem> BuildReferenceNavigationItems(
      IReadOnlyList<ReferenceTableInfo> referenceTables
   )
   {
      return referenceTables
         .OrderBy(table => table.Title, StringComparer.OrdinalIgnoreCase)
         .Select(table => new AdminNavItem(
            table.Title,
            $"/Admin/Config/{table.Id}"
         ))
         .ToList();
   }

   public static IReadOnlyList<AdminNavGroup> BuildConfigNavigationGroups(
      IReadOnlyList<ReferenceTableInfo> referenceTables
   )
   {
      var referenceItems = BuildReferenceNavigationItems(
         referenceTables
      ).ToList();
      var activityTypesIndex = referenceItems.FindIndex(item =>
         string.Equals(
            item.Title,
            "Activity types",
            StringComparison.OrdinalIgnoreCase
         )
      );

      var broadcastIgnoreRulesItem = new AdminNavItem(
         "Broadcast Ignore Rules",
         "/Admin/Config/BroadcastIgnoreRules"
      );

      if(activityTypesIndex >= 0)
      {
         referenceItems.Insert(
            activityTypesIndex + 1,
            broadcastIgnoreRulesItem
         );
      }
      else
      {
         referenceItems.Add(broadcastIgnoreRulesItem);
      }

      return
      [
         new AdminNavGroup(
            "Operations",
            [
               new AdminNavItem("Web statistics", "/Admin/Config/Stats")
            ]
         ),
         new AdminNavGroup(
            "AI",
            [
               new AdminNavItem("AI providers", "/Admin/Config/Ai/Providers"),
               new AdminNavItem("AI jobs", "/Admin/Config/Ai/Jobs"),
               new AdminNavItem("AI prompts", "/Admin/Config/Ai/Prompts")
            ]
         ),
         new AdminNavGroup(
            "Reference tables",
            referenceItems
         )
      ];
   }
}
