using Microsoft.AspNetCore.Mvc;
using Npgsql;

using SESport.Core.Configuration;
using SESport.Core.Domain;
using SESport.Data;
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
      var model = new EditModel(repository)
      {
         Entity = new EntityEditModel
         {
            Id = null,
            CanonicalName = entityName,
            AliasName = aliasName,
            EntityTypeId = TrackedEntityTypeIds.Organization,
            SportId = "football",
            CountryId = "se",
            CountryRelevanceKindId =
               "NationalityOrSportingIdentity",
            CountryRelevanceReason = "Test coverage",
            WatchPriorityId = "review",
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

   private static NpgsqlDataSource CreateDataSource()
   {
      var connectionString = PostgresConnectionStrings.ResolveDefault();

      return new NpgsqlDataSourceBuilder(connectionString).Build();
   }
}
