using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;

using Npgsql;

using SESport.Core.Configuration;
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
      var automationService = new CapturingAiAutomationService();
      var model = new EditModel(
         repository,
         sourceRepository,
         automationService,
         new TestHostApplicationLifetime(),
         new CapturingEntityImageReplacementService()
      )
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
      var automationService = new CapturingAiAutomationService();
      var model = new EditModel(
         repository,
         sourceRepository,
         automationService,
         new TestHostApplicationLifetime(),
         new CapturingEntityImageReplacementService()
      )
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

   [Fact]
   public async Task OnPostReplaceImageAsyncValidatesCommonsRevisionUrl()
   {
      var entityName = $"Image Entity {Guid.NewGuid():N}";
      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);
      var sourceRepository = new SourceReferenceRepository(dataSource);
      var automationService = new CapturingAiAutomationService();
      var imageService = new CapturingEntityImageReplacementService();
      var model = new EditModel(
         repository,
         sourceRepository,
         automationService,
         new TestHostApplicationLifetime(),
         imageService
      )
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
         Assert.NotNull(model.Entity.Id);
         var entityId = model.Entity.Id.Value;
         model.Entity.PrimaryImageSourceUrl =
            " https://commons.wikimedia.org/w/index.php?" +
            "title=File:Example.jpg&oldid=42 ";

         var result = await model.OnPostReplaceImageAsync(
            entityId,
            CancellationToken.None
         );

         Assert.IsType<RedirectToPageResult>(result);
         Assert.Equal(entityId, imageService.EntityId);
         Assert.NotNull(imageService.Source);
         Assert.Equal(42, imageService.Source!.RevisionId);
         Assert.Equal(
            "https://commons.wikimedia.org/w/index.php?" +
            "title=File:Example.jpg&oldid=42",
            imageService.Source.Url
         );
         Assert.Equal("Image replacement completed.", model.ImageMessage);
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
   public async Task OnPostReplaceImageAsyncDoesNotRenderInvalidUrlMessage()
   {
      var entityName = $"Invalid image entity {Guid.NewGuid():N}";
      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);
      var sourceRepository = new SourceReferenceRepository(dataSource);
      var automationService = new CapturingAiAutomationService();
      var imageService = new CapturingEntityImageReplacementService();
      var model = new EditModel(
         repository,
         sourceRepository,
         automationService,
         new TestHostApplicationLifetime(),
         imageService
      )
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
         Assert.NotNull(model.Entity.Id);
         var entityId = model.Entity.Id.Value;
         model.Entity.PrimaryImageSourceUrl =
            "https://example.test/not-a-wikimedia-url";

         var result = await model.OnPostReplaceImageAsync(
            entityId,
            CancellationToken.None
         );

         Assert.IsType<PageResult>(result);
         var entry = model.ModelState["Entity.PrimaryImageSourceUrl"];
         Assert.NotNull(entry);
         Assert.Single(entry!.Errors);
         Assert.Empty(entry.Errors[0].ErrorMessage);
         Assert.Null(imageService.Source);
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
   public async Task OnPostReplaceImageAsyncWithEmptyUrlRemovesImage()
   {
      var entityName = $"Removed image entity {Guid.NewGuid():N}";
      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);
      var sourceRepository = new SourceReferenceRepository(dataSource);
      var automationService = new CapturingAiAutomationService();
      var imageService = new CapturingEntityImageReplacementService();
      var model = new EditModel(
         repository,
         sourceRepository,
         automationService,
         new TestHostApplicationLifetime(),
         imageService
      )
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
         Assert.NotNull(model.Entity.Id);
         var entityId = model.Entity.Id.Value;
         await InsertEntityImageAsync(dataSource, entityId);
         model.Entity.PrimaryImageSourceUrl = "   ";

         var result = await model.OnPostReplaceImageAsync(
            entityId,
            CancellationToken.None
         );

         Assert.IsType<RedirectToPageResult>(result);
         Assert.Null(imageService.Source);
         Assert.Equal("Image removed.", model.ImageMessage);

         var loaded = await repository.GetEntityForEditAsync(
            entityId,
            CancellationToken.None
         );
         Assert.NotNull(loaded);
         Assert.False(loaded!.HasPrimaryThumbnail);
         Assert.Null(loaded.PrimaryImageSourceUrl);
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
   public async Task OnPostAsyncTriggersAutomationForNewPerson()
   {
      var entityName = $"Person {Guid.NewGuid():N}";

      await using var dataSource = CreateDataSource();
      var repository = new AdminRepository(dataSource);
      var sourceRepository = new SourceReferenceRepository(dataSource);
      var automationService = new CapturingAiAutomationService();
      var model = new EditModel(
         repository,
         sourceRepository,
         automationService,
         new TestHostApplicationLifetime(),
         new CapturingEntityImageReplacementService()
      )
      {
         Entity = new EntityEditModel
         {
            CanonicalName = entityName,
            EntityTypeId = TrackedEntityTypeIds.Person,
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
         var entityId = model.Entity.Id.Value;
         var triggeredId = Assert.Single(
            automationService.PersonCreatedEntityIds
         );

         Assert.Equal(entityId, triggeredId);
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

   private static async Task InsertEntityImageAsync(
      NpgsqlDataSource dataSource,
      Guid entityId
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         insert into entity_images (
            id,
            entity_id,
            image_data,
            mime_type,
            thumbnail_data,
            thumbnail_mime_type,
            source_kind,
            source_url,
            license_name,
            review_status,
            reviewed_at,
            is_primary
         )
         values (
            @id,
            @entity_id,
            @image_data,
            'image/jpeg',
            @thumbnail_data,
            'image/jpeg',
            'test',
            'https://example.test/entity-image',
            'Test license',
            @review_status,
            @reviewed_at,
            true
         )
         """
      );
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("entity_id", entityId);
      command.Parameters.AddWithValue(
         "image_data",
         new byte[] { 1, 2, 3 }
      );
      command.Parameters.AddWithValue(
         "thumbnail_data",
         new byte[] { 9, 8 }
      );
      command.Parameters.AddWithValue(
         "review_status",
         EntityImageReviewStatusIds.Approved
      );
      command.Parameters.AddWithValue(
         "reviewed_at",
         new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
      );

      await command.ExecuteNonQueryAsync();
   }

   private sealed class CapturingAiAutomationService : IAiAutomationService
   {
      public List<Guid> PersonCreatedEntityIds { get; } = [];

      public Task HandleActivityCreatedAsync(
         Guid activityId,
         CancellationToken cancellationToken
      )
      {
         return Task.CompletedTask;
      }

      public Task HandleActivityGroupCreatedAsync(
         Guid activityGroupId,
         CancellationToken cancellationToken
      )
      {
         return Task.CompletedTask;
      }

      public Task HandlePersonCreatedAsync(
         Guid personEntityId,
         CancellationToken cancellationToken
      )
      {
         PersonCreatedEntityIds.Add(personEntityId);
         return Task.CompletedTask;
      }
   }

   private sealed class CapturingEntityImageReplacementService
      : IEntityImageReplacementService
   {
      public Guid? EntityId { get; private set; }

      public WikimediaCommonsImageReference? Source { get; private set; }

      public Task ReplaceAsync(
         Guid entityId,
         WikimediaCommonsImageReference source,
         CancellationToken cancellationToken
      )
      {
         EntityId = entityId;
         Source = source;
         return Task.CompletedTask;
      }
   }

   private sealed class TestHostApplicationLifetime
      : IHostApplicationLifetime
   {
      public CancellationToken ApplicationStarted => CancellationToken.None;

      public CancellationToken ApplicationStopping => CancellationToken.None;

      public CancellationToken ApplicationStopped => CancellationToken.None;

      public void StopApplication()
      {
      }
   }

}
