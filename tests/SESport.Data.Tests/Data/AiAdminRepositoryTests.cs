using SESport.Data.Repositories;
using SESport.TestSupport;

namespace SESport.Core.Tests.Data;

public sealed class AiAdminRepositoryTests
{
   [Fact]
   public async Task PromptListsBuildValidSqlWithAndWithoutJobFilter()
   {
      await using var dataSource = PostgresTestDatabase.CreateDataSource();
      var repository = new AiAdminRepository(dataSource);

      var allPrompts = await repository.GetPromptsAsync(
         CancellationToken.None
      );
      var missingJobPrompts = await repository.GetJobPromptsAsync(
         $"missing-{Guid.NewGuid():N}",
         CancellationToken.None
      );

      Assert.NotNull(allPrompts);
      Assert.Empty(missingJobPrompts);
   }
}
