using SESport.Data.Repositories;

namespace SESport.Core.Tests.Data;

public sealed class MemberPushRepositoryTests
{
   [Fact]
   public async Task ClaimDueNotificationsCanQueryThePushSchema()
   {
      await using var dataSource = CreateDataSource();
      var repository = new MemberPushRepository(dataSource);
      var now = DateTimeOffset.UtcNow;

      Assert.False(
         await repository.HasActiveSubscriptionAsync(
            Guid.NewGuid(),
            CancellationToken.None
         )
      );

      var notifications = await repository.ClaimDueNotificationsAsync(
         now,
         now.AddMinutes(-5),
         10,
         10,
         CancellationToken.None
      );

      Assert.Empty(notifications);
   }
}
