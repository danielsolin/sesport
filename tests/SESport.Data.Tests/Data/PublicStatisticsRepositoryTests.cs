using SESport.Data.Repositories;

namespace SESport.Core.Tests.Data;

public sealed class PublicStatisticsRepositoryTests
{
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
