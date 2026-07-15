using SESport.Core.Domain;
using SESport.Core.Formatting;

namespace SESport.Core.Broadcast;

public static class BroadcastListDisplayFormatter
{
   public static string FormatTimeText(
      DateTimeOffset startsAt,
      DateTimeOffset endsAt
   )
   {
      var localStart = TimeZoneHelper.ToLocal(
         startsAt,
         SportDay.TimeZoneId
      );
      var localEnd = TimeZoneHelper.ToLocal(
         endsAt,
         SportDay.TimeZoneId
      );

      return $"{localStart:yyyy-MM-dd HH:mm}-{localEnd:HH:mm}";
   }

   public static string FormatCategoriesText(
      IReadOnlyList<string> categories
   )
   {
      return string.Join(", ", categories);
   }

   public static string FormatGroupValue(
      string title,
      string? activityGroupTitle,
      string? activityGroupDraftTitle
   )
   {
      return activityGroupTitle ?? activityGroupDraftTitle ?? title;
   }

   public static string FormatGroupText(
      string title,
      string? activityGroupSourceKindId,
      Guid? activityGroupId,
      string? activityGroupTitle,
      string? activityGroupDraftTitle
   )
   {
      if(!string.Equals(
         activityGroupSourceKindId,
         BroadcastActivitySourceKindIds.ActivityGroupForActivity,
         StringComparison.Ordinal
      ))
      {
         return "-";
      }

      var groupValue = FormatGroupValue(
         title,
         activityGroupTitle,
         activityGroupDraftTitle
      );

      return activityGroupId is null
         ? $"NEW: {groupValue}"
         : groupValue;
   }
}
