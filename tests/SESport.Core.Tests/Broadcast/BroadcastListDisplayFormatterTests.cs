using SESport.Core.Broadcast;

namespace SESport.Core.Tests.Broadcast;

public class BroadcastListDisplayFormatterTests
{
   [Fact]
   public void FormatCategoriesText_JoinsCategoriesWithComma()
   {
      var result = BroadcastListDisplayFormatter.FormatCategoriesText(
         ["Football", "Handball", "Tennis"]
      );

      Assert.Equal("Football, Handball, Tennis", result);
   }

   [Fact]
   public void SplitChannelNames_ReturnsTrimmedNonEmptyNames()
   {
      var result = BroadcastListDisplayFormatter.SplitChannelNames(
         " SVT1,  SVT2 ,, TV4 "
      );

      Assert.Equal(["SVT1", "SVT2", "TV4"], result);
   }

   [Fact]
   public void FormatSourceLabel_ReturnsCompactLabel()
   {
      Assert.Equal(
         "U",
         BroadcastListDisplayFormatter.FormatSourceLabel("tvnu")
      );
      Assert.Equal(
         "M",
         BroadcastListDisplayFormatter.FormatSourceLabel("tvmatchen")
      );
   }

   [Fact]
   public void FormatTimeText_ReturnsLocalRange()
   {
      var startsAt = new DateTimeOffset(
         2026,
         1,
         15,
         10,
         0,
         0,
         TimeSpan.Zero
      );
      var endsAt = new DateTimeOffset(
         2026,
         1,
         15,
         11,
         30,
         0,
         TimeSpan.Zero
      );

      var result =
         BroadcastListDisplayFormatter.FormatTimeText(
            startsAt,
            endsAt
         );

      Assert.Equal("2026-01-15 11:00-12:30", result);
   }

   [Fact]
   public void FormatGroupValue_PrefersGroupTitleThenDraftThenTitle()
   {
      var titleOnly = BroadcastListDisplayFormatter.FormatGroupValue(
         "Fallback title",
         null,
         null
      );
      var draftTitle = BroadcastListDisplayFormatter.FormatGroupValue(
         "Fallback title",
         null,
         "Draft title"
      );
      var groupTitle = BroadcastListDisplayFormatter.FormatGroupValue(
         "Fallback title",
         "Group title",
         null
      );

      Assert.Equal("Fallback title", titleOnly);
      Assert.Equal("Draft title", draftTitle);
      Assert.Equal("Group title", groupTitle);
   }

   [Fact]
   public void FormatGroupText_ReturnsDashForOtherSourceKinds()
   {
      var result = BroadcastListDisplayFormatter.FormatGroupText(
         "Fallback title",
         "OtherKind",
         null,
         null,
         null
      );

      Assert.Equal("-", result);
   }

   [Fact]
   public void FormatGroupText_ReturnsNewPrefixWhenGroupIsMissing()
   {
      var result = BroadcastListDisplayFormatter.FormatGroupText(
         "Fallback title",
         BroadcastActivitySourceKindIds.ActivityGroupForActivity,
         null,
         null,
         null
      );

      Assert.Equal("NEW: Fallback title", result);
   }

   [Fact]
   public void FormatGroupText_ReturnsGroupValueWhenGroupExists()
   {
      var result = BroadcastListDisplayFormatter.FormatGroupText(
         "Fallback title",
         BroadcastActivitySourceKindIds.ActivityGroupForActivity,
         Guid.NewGuid(),
         null,
         null
      );

      Assert.Equal("Fallback title", result);
   }
}
