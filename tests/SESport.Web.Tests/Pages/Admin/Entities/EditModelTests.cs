using Microsoft.AspNetCore.Mvc;

using SESport.Core.Sources;
using SESport.Data.Models;
using SESport.Data.Repositories;
using SESport.Web.Pages.Admin.Entities;

namespace SESport.Core.Tests.Pages.Admin.Entities;

public sealed class EditModelTests
{
   [Fact]
   public async Task OnPostAsyncPersistsAliasForOrganizationEntities()
   {
      var entityId = Guid.NewGuid();
      var entityName = $"Organization {entityId:N}";
      var aliasName = $"Alias {entityId:N}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);
      var sourceRepository = new SourceReferenceRepository(dataSource);
      var model = new EditModel(repository, sourceRepository)
      {
         Entity = new EntityEditModel
         {
            Id = null,
            CanonicalName = entityName,
            AliasName = aliasName,
            EntityTypeId = TrackedEntityTypeIds.Organization,
            SportId = "football",
            CountryId = PrimaryCountry.Id,
            CountryRelevanceKindId =
               "NationalityOrSportingIdentity",
            CountryRelevanceReason = "Test coverage",
            WatchPriorityId = "tier_3",
            ExpectedStabilityId = "short_term"
         }
      };

      try
      {
         var result = await model.OnPostAsync(CancellationToken.None);

         Assert.IsType<RedirectToPageResult>(result);
         Assert.NotNull(model.Entity.Id);

         var loaded = await repository.GetEntityForEditAsync(
            model.Entity.Id.Value,
            CancellationToken.None
         );

         Assert.NotNull(loaded);
         Assert.Equal(aliasName, loaded!.AliasName);
         Assert.Equal(entityName, loaded.CanonicalName);
      }
      finally
      {
         if(model.Entity.Id is not null)
         {
            await repository.DeleteEntityAsync(
               model.Entity.Id.Value,
               CancellationToken.None
            );
         }
      }
   }

   [Fact]
   public async Task OnPostAddSourceAsyncPersistsNormalizedEntitySource()
   {
      var entityName = $"Source Entity {Guid.NewGuid():N}";
      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);
      var sourceRepository = new SourceReferenceRepository(dataSource);
      var model = new EditModel(repository, sourceRepository)
      {
         Entity = new EntityEditModel
         {
            CanonicalName = entityName,
            EntityTypeId = TrackedEntityTypeIds.Organization,
            SportId = "football",
            CountryId = PrimaryCountry.Id,
            CountryRelevanceKindId =
               "NationalityOrSportingIdentity",
            CountryRelevanceReason = "Test coverage",
            WatchPriorityId = "tier_3",
            ExpectedStabilityId = "short_term"
         }
      };

      try
      {
         await model.OnPostAsync(CancellationToken.None);
         var entityId = model.Entity.Id;
         Assert.NotNull(entityId);

         var result = await model.OnPostAddSourceAsync(
            entityId.Value,
            " https://example.test/entity ",
            CancellationToken.None
         );

         Assert.IsType<RedirectToPageResult>(result);
         var sources = await sourceRepository.GetByCorrelationAsync(
            SourceCorrelationTypes.Entity,
            entityId.Value.ToString(),
            null,
            CancellationToken.None
         );
         var source = Assert.Single(sources);

         Assert.Equal(SourceKinds.Bio, source.Kind);
         Assert.Equal("https://example.test/entity", source.Url);
      }
      finally
      {
         if(model.Entity.Id is not null)
         {
            await sourceRepository.DeleteByCorrelationAsync(
               SourceCorrelationTypes.Entity,
               model.Entity.Id.Value.ToString(),
               CancellationToken.None
            );
            await repository.DeleteEntityAsync(
               model.Entity.Id.Value,
               CancellationToken.None
            );
         }
      }
   }

}
