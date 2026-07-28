using SESport.Core.Sources;
using SESport.Data.Repositories;

namespace SESport.Core.Tests.Data;

public sealed class SourceReferenceRepositoryTests
{
   [Fact]
   public async Task CreateAndGetByCorrelationKeepsSeparateUsages()
   {
      var correlationId = Guid.NewGuid().ToString();
      await using var dataSource = CreateDataSource();
      var repository = new SourceReferenceRepository(dataSource);

      var first = await repository.CreateAsync(
         SourceCorrelationTypes.Entity,
         correlationId,
         SourceKinds.Bio,
         "https://example.test/source",
         "Example source",
         "Bio excerpt",
         null,
         CancellationToken.None
      );
      var second = await repository.CreateAsync(
         SourceCorrelationTypes.Entity,
         correlationId,
         SourceKinds.ParticipationEvidence,
         "https://example.test/source",
         null,
         "Participation excerpt",
         null,
         CancellationToken.None
      );

      try
      {
         var all = await repository.GetByCorrelationAsync(
            SourceCorrelationTypes.Entity,
            correlationId,
            null,
            CancellationToken.None
         );
         var bio = await repository.GetByCorrelationAsync(
            SourceCorrelationTypes.Entity,
            correlationId,
            SourceKinds.Bio,
            CancellationToken.None
         );

         Assert.Equal(2, all.Count);
         Assert.Contains(all, source => source.Id == first.Id);
         Assert.Contains(all, source => source.Id == second.Id);
         Assert.Equal([first.Id], bio.Select(source => source.Id));
      }
      finally
      {
         await repository.DeleteByCorrelationAsync(
            SourceCorrelationTypes.Entity,
            correlationId,
            CancellationToken.None
         );
      }
   }

   [Fact]
   public async Task DeleteByCorrelationDoesNotDeleteAnotherTarget()
   {
      var firstCorrelationId = Guid.NewGuid().ToString();
      var secondCorrelationId = Guid.NewGuid().ToString();
      await using var dataSource = CreateDataSource();
      var repository = new SourceReferenceRepository(dataSource);

      await repository.CreateAsync(
         SourceCorrelationTypes.Entity,
         firstCorrelationId,
         SourceKinds.Bio,
         "https://example.test/first",
         null,
         null,
         null,
         CancellationToken.None
      );
      var second = await repository.CreateAsync(
         SourceCorrelationTypes.Entity,
         secondCorrelationId,
         SourceKinds.Bio,
         "https://example.test/second",
         null,
         null,
         null,
         CancellationToken.None
      );

      try
      {
         await repository.DeleteByCorrelationAsync(
            SourceCorrelationTypes.Entity,
            firstCorrelationId,
            CancellationToken.None
         );

         var remaining = await repository.GetAsync(
            second.Id,
            CancellationToken.None
         );

         Assert.NotNull(remaining);
         Assert.Equal(secondCorrelationId, remaining.CorrelationId);
      }
      finally
      {
         await repository.DeleteByCorrelationAsync(
            SourceCorrelationTypes.Entity,
            firstCorrelationId,
            CancellationToken.None
         );
         await repository.DeleteByCorrelationAsync(
            SourceCorrelationTypes.Entity,
            secondCorrelationId,
            CancellationToken.None
         );
      }
   }

}
