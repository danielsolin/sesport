
namespace SESport.Core.Tests.Data;

public sealed class DashboardRepositoryTests
{
   [Fact]
   public async Task GetAsyncReturnsCoverageAndHealth()
   {
      await using var dataSource = CreateDataSource();
      var repository = new DashboardRepository(dataSource);
      var now = DateTimeOffset.UtcNow;

      var dashboard = await repository.GetAsync(
         now,
         CancellationToken.None
      );

      Assert.Equal(
         DashboardDefaults.CoverageDayCount,
         dashboard.Dates.Count
      );
      Assert.Equal(
         SportDay.GetSportDate(now),
         dashboard.Dates[0].Date
      );
      Assert.All(
         dashboard.Dates,
         date =>
         {
            Assert.True(date.VisibleBroadcastCount >= 0);
            Assert.True(date.UnreviewedBroadcastCount >= 0);
            Assert.True(date.PublishedActivityCount >= 0);
            Assert.True(date.DraftActivityCount >= 0);
         }
      );
      Assert.True(dashboard.AiHealth.PendingCount >= 0);
      Assert.True(dashboard.AiHealth.RunningCount >= 0);
      Assert.True(dashboard.AiHealth.StaleRunningCount >= 0);
      Assert.True(dashboard.AiHealth.FailedLast25HoursCount >= 0);
   }
}
