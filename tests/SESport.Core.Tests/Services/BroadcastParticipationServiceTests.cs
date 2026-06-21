using System.Reflection;
using System.Text.Json;

using SESport.Core.Broadcast;
using SESport.Web.Services;

namespace SESport.Core.Tests.Services;

public sealed class BroadcastParticipationServiceTests
{
   [Fact]
   public void CreateParticipationInputJsonUsesDateOnlyMarker()
   {
      var broadcast = new BroadcastActivitySource(
         Guid.NewGuid(),
         "Channel",
         "Event title",
         null,
         ["Tennis"],
         DateTimeOffset.Parse("2026-06-15T12:34:56Z"),
         DateTimeOffset.Parse("2026-06-15T14:00:00Z")
      );

      var method = typeof(BroadcastParticipationService).GetMethod(
         "CreateParticipationInputJson",
         BindingFlags.NonPublic | BindingFlags.Static
      )!;

      var json = (string)method.Invoke(null, [broadcast, "  - Candidate"])!;
      using var document = JsonDocument.Parse(json);
      var root = document.RootElement;

      Assert.True(root.TryGetProperty("date", out var date));
      Assert.False(root.TryGetProperty("date_time", out _));
      Assert.Equal("2026-06-15", date.GetString());
      Assert.Equal("Event title", root.GetProperty("event_name").GetString());
      Assert.Equal("  - Candidate", root.GetProperty("candidates").GetString());
   }
}
