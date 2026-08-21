using SESport.Data.Models;
using SESport.Web.Services;
using System.Reflection;
using System.Text.Json;

namespace SESport.Core.Tests.Services;

public sealed class MemberPushNotificationSenderTests
{
   [Fact]
   public void CreatePayloadUsesTheNewNotificationText()
   {
      var notification = new MemberActivityPushNotification(
         Guid.NewGuid(),
         Guid.NewGuid(),
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
         "Om 10 minuter: Armand Duplantis tävlar i Stavhopp. " +
         "Visas på SVT1, SVT Play.",
         document.RootElement.GetProperty("body").GetString()
      );
   }
}
