
namespace SESport.Core.Tests.Data;

public sealed class PublicStatisticsRepositoryTests
{
   [Theory]
   [InlineData("2026-09-01", "2026-09-07", "2026-09-08")]
   [InlineData("2026-08-01", "2026-09-07", "2026-09-01")]
   [InlineData("2026-10-01", "2026-09-07", "2026-10-01")]
   public void GetMonthEndExclusiveStopsAtTheLastIncludedDate(
      string monthStartValue,
      string lastIncludedDateValue,
      string expectedValue
   )
   {
      var result = PublicStatisticsRepository.GetMonthEndExclusive(
         DateOnly.Parse(monthStartValue),
         DateOnly.Parse(lastIncludedDateValue)
      );

      Assert.Equal(DateOnly.Parse(expectedValue), result);
   }

   [Fact]
   public async Task GetMonthlyAsyncHandlesAnEmptyDistantMonth()
   {
      await using var dataSource = CreateDataSource();
      var repository = new PublicStatisticsRepository(dataSource);

      var result = await repository.GetMonthlyAsync(
         new DateOnly(2199, 1, 1),
         10,
         CancellationToken.None
      );

      Assert.Equal(0, result.ParticipantCount);
      Assert.Empty(result.Leaders);

      var sportOptions = await repository.GetMonthlySportOptionsAsync(
         new DateOnly(2199, 1, 1),
         CancellationToken.None
      );

      Assert.Equal(0, sportOptions.ParticipantCount);
      Assert.Empty(sportOptions.Options);
   }
}
