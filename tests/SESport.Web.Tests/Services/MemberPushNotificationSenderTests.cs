using System.Reflection;
using System.Text.Json;

using SESport.Data.Models;

namespace SESport.Core.Tests.Services;

public sealed class MemberPushNotificationSenderTests
{
   [Fact]
   public void CreatePayloadUsesTheNewNotificationText()
   {
      var activityId = Guid.NewGuid();
      var notification = new MemberActivityPushNotification(
         Guid.NewGuid(),
         activityId,
         "Stavhopp",
         "Armand Duplantis",
         DateTimeOffset.Parse("2026-08-21T18:00:00Z"),
         "sport-day",
         10,
         "SVT1, SVT Play",
         []
      );

      var method = typeof(MemberPushNotificationSender).GetMethod(
         "CreatePayload",
         BindingFlags.NonPublic | BindingFlags.Static
      )!;
      var payload = (string)method.Invoke(null, [notification])!;

      using var document = JsonDocument.Parse(payload);
      Assert.Equal(
         "Armand Duplantis deltar i Stavhopp. Start 20:00. " +
         "Visas på SVT1, SVT Play.",
         document.RootElement.GetProperty("body").GetString()
      );
      Assert.Equal(
         "/?date=2026-08-21#activity-" + activityId.ToString("N"),
         document.RootElement.GetProperty("url").GetString()
      );
      Assert.Equal(
         DateTimeOffset.Parse("2026-08-21T18:00:00Z"),
         document.RootElement.GetProperty("expiresAt")
            .GetDateTimeOffset()
      );
      Assert.True(
         document.RootElement.GetProperty("sentAt")
            .GetDateTimeOffset() <= DateTimeOffset.UtcNow
      );
   }

   [Fact]
   public void CreatePayloadLimitsPersonNamesToTheConfiguredDefault()
   {
      var notification = new MemberActivityPushNotification(
         Guid.NewGuid(),
         Guid.NewGuid(),
         "Stavhopp",
         "First Person, Second Person, Third Person, Fourth Person",
         DateTimeOffset.Parse("2026-08-21T18:00:00Z"),
         "sport-day",
         10,
         null,
         []
      );

      var method = typeof(MemberPushNotificationSender).GetMethod(
         "CreatePayload",
         BindingFlags.NonPublic | BindingFlags.Static
      )!;
      var payload = (string)method.Invoke(null, [notification])!;

      using var document = JsonDocument.Parse(payload);
      Assert.Equal(
         "First Person, Second Person, Third Person med flera " +
         "deltar i Stavhopp. Start 20:00.",
         document.RootElement.GetProperty("body").GetString()
      );
   }
}
