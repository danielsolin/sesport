namespace SESport.Core.Configuration;

public static class MemberNotificationLeadTimes
{
   public const int OneHourMinutes = 60;

   public const int ThirtyMinutes = 30;

   public const int TenMinutes = 10;

   public static IReadOnlyList<int> SupportedMinutes { get; } =
   [
      OneHourMinutes,
      ThirtyMinutes,
      TenMinutes
   ];

   public static bool IsSupported(int minutes)
   {
      return SupportedMinutes.Contains(minutes);
   }

   public static int Normalize(int? minutes, int defaultMinutes)
   {
      if(minutes is not null && IsSupported(minutes.Value))
      {
         return minutes.Value;
      }

      return IsSupported(defaultMinutes)
         ? defaultMinutes
         : TenMinutes;
   }
}
