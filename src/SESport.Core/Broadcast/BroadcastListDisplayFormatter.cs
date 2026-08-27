using System.Globalization;

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

      return string.Concat(
         localStart.ToString(
            DateDisplay.DateTimeMinutesFormat,
            CultureInfo.InvariantCulture
         ),
         "-",
         localEnd.ToString(
            DateDisplay.TimeOnlyMinutesFormat,
            CultureInfo.InvariantCulture
         )
      );
   }

   public static string FormatCategoriesText(
      IReadOnlyList<string> categories
   )
   {
      return string.Join(", ", categories);
   }

   public static IReadOnlyList<string> SplitChannelNames(
      string? channelNames
   )
   {
      return (channelNames ?? string.Empty).Split(
         ',',
         StringSplitOptions.RemoveEmptyEntries
            | StringSplitOptions.TrimEntries
      );
   }

   public static string FormatSourceLabel(string sourceKey)
   {
      return sourceKey switch
      {
         "tvnu" => "U",
         "tvmatchen" => "M",
         _ => sourceKey
      };
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
