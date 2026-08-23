using SESport.Core.Configuration;

namespace SESport.Core.Tests.Configuration;

public sealed class MemberNotificationLeadTimesTests
{
   [Fact]
   public void SupportedLeadTimesStartWithNotificationsDisabled()
   {
      Assert.Equal(
         [
            MemberNotificationLeadTimes.NoNotificationsMinutes,
            MemberNotificationLeadTimes.OneHourMinutes,
            MemberNotificationLeadTimes.ThirtyMinutes,
            MemberNotificationLeadTimes.TenMinutes
         ],
         MemberNotificationLeadTimes.SupportedMinutes
      );
   }

   [Theory]
   [InlineData(null, 0, 0)]
   [InlineData(null, 10, 10)]
   [InlineData(60, 10, 60)]
   [InlineData(30, 10, 30)]
   [InlineData(10, 60, 10)]
   [InlineData(5, 30, 30)]
   [InlineData(5, 0, 0)]
   public void NormalizeUsesSupportedValueOrDefault(
      int? value,
      int defaultValue,
      int expected
   )
   {
      Assert.Equal(
         expected,
         MemberNotificationLeadTimes.Normalize(value, defaultValue)
      );
   }
}
