using SESport.Core.Formatting;

namespace SESport.Core.Domain;

public static class ActivityDisplayDateResolver
{
   public static DateOnly Resolve(
      DateTimeOffset startsAt,
      string? publicDateMode
   )
   {
      if(string.Equals(
         publicDateMode,
         ActivityGroupPublicDateModeIds.LocalCalendarDate,
         StringComparison.Ordinal
      ))
      {
         var localStart = TimeZoneHelper.ToLocal(
            startsAt,
            SportDay.TimeZoneId
         );
         return DateOnly.FromDateTime(localStart.DateTime);
      }

      return SportDay.GetSportDate(startsAt);
   }
}
