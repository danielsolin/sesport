namespace SESport.Core.Domain;

public sealed record ActivityTime(
   ActivityTimeKind Kind,
   DateOnly ActivityDate,
   TimeOnly? LocalStartTime,
   DateTimeOffset? StartsAt,
   string TimeZoneId,
   string? Description
)
{
   public static ActivityTime OnDate(
      DateOnly activityDate,
      string? description = null,
      string timeZoneId = "Europe/Stockholm"
   )
   {
      return new ActivityTime(
         ActivityTimeKind.DateOnly,
         activityDate,
         null,
         null,
         timeZoneId,
         description
      );
   }

   public static ActivityTime Scheduled(
      DateTimeOffset startsAt,
      string timeZoneId = "Europe/Stockholm"
   )
   {
      return new ActivityTime(
         ActivityTimeKind.Scheduled,
         DateOnly.FromDateTime(startsAt.DateTime),
         TimeOnly.FromDateTime(startsAt.DateTime),
         startsAt,
         timeZoneId,
         null
      );
   }

   public static ActivityTime ScheduledLocal(
      DateOnly activityDate,
      TimeOnly localStartTime,
      string timeZoneId,
      string? description = null
   )
   {
      return new ActivityTime(
         ActivityTimeKind.Scheduled,
         activityDate,
         localStartTime,
         null,
         timeZoneId,
         description
      );
   }
}
