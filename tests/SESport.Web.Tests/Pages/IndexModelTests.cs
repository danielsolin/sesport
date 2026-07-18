using System.Reflection;

using Microsoft.Extensions.FileProviders;

using SESport.Data;
using SESport.Web.Pages;

namespace SESport.Core.Tests.Pages;

public sealed class IndexModelTests
{
   [Fact]
   public void HasSportIconPath_ReturnsFalseWhenFileMissing()
   {
      using var tempDirectory = TempDirectory.Create();
      var fileProvider = new PhysicalFileProvider(tempDirectory.FullName);

      var hasIcon = IndexModel.HasSportIconPath(
         fileProvider,
         "/icons/sports/boat-racing.svg"
      );

      Assert.False(hasIcon);
   }

   [Fact]
   public void HasSportIconPath_ReturnsTrueWhenFileExists()
   {
      using var tempDirectory = TempDirectory.Create();
      var iconDirectory = Path.Combine(
         tempDirectory.FullName,
         "icons",
         "sports"
      );
      Directory.CreateDirectory(iconDirectory);
      File.WriteAllText(
         Path.Combine(iconDirectory, "motorsport.svg"),
         string.Empty
      );

      var fileProvider = new PhysicalFileProvider(tempDirectory.FullName);
      var hasIcon = IndexModel.HasSportIconPath(
         fileProvider,
         "/icons/sports/motorsport.svg"
      );

      Assert.True(hasIcon);
   }

   [Fact]
   public void CountParticipants_CountsUniqueEntityIds()
   {
      var activities = new[]
      {
         CreateActivity("A", [Guid.Parse("11111111-1111-1111-1111-111111111111")]),
         CreateActivity("B", []),
         CreateActivity(
            "C",
            [
               Guid.Parse("11111111-1111-1111-1111-111111111111"),
               Guid.Parse("22222222-2222-2222-2222-222222222222")
            ]
         )
      };

      var total = IndexModel.CountParticipants(activities);

      Assert.Equal(2, total);
   }

   [Fact]
   public void SplitParticipantNames_TrimsAndSplitsNames()
   {
      var names = IndexModel.SplitParticipantNames(
         " Anna, Björn ,  Cecilia "
      );

      Assert.Equal(["Anna", "Björn", "Cecilia"], names);
   }

   [Fact]
   public void BuildDateOptions_UsesThreeDayWindow()
   {
      var today = new DateOnly(2026, 7, 3);
      var selectedDate = today;

      var method = typeof(IndexModel).GetMethod(
         "BuildDateOptions",
         BindingFlags.NonPublic | BindingFlags.Static
      );

      var options = (IReadOnlyList<DateOption>)method!.Invoke(
         null,
         [today, selectedDate]
      )!;

      Assert.Equal(3, options.Count);
      Assert.Equal(
         [today.AddDays(-1), today, today.AddDays(1)],
         options.Select(option => DateOnly.Parse(option.Value))
      );
   }

   private static ActivityListItem CreateActivity(
      string title,
      Guid[] participantIds
   )
   {
      return new ActivityListItem(
         Guid.NewGuid(),
         title,
         null,
         null,
         "Match",
         "football",
         "Football",
         null,
         "2026-06-26",
         null,
         null,
         "Published",
         string.Empty,
         participantIds,
         string.Empty
      );
   }

   private static DirectoryInfo CreateTempDirectory()
   {
      var path = Path.Combine(
         Path.GetTempPath(),
         $"sesport-index-{Guid.NewGuid():N}"
      );

      return Directory.CreateDirectory(path);
   }

   private sealed class TempDirectory : IDisposable
   {
      public TempDirectory(DirectoryInfo directory)
      {
         Directory = directory;
      }

      public DirectoryInfo Directory { get; }

      public string FullName => Directory.FullName;

      public static TempDirectory Create()
      {
         return new TempDirectory(CreateTempDirectory());
      }

      public void Dispose()
      {
         Directory.Delete(true);
      }
   }
}
