using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Domain;
using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Entities;

public class EditModel(
   AdminRepository repository,
   SourceReferenceRepository sourceRepository,
   IAiAutomationService automationService,
   IHostApplicationLifetime applicationLifetime,
   IEntityImageReplacementService imageReplacementService
) : PageModel
{
   [BindProperty]
   public EntityEditModel Entity { get; set; } = new();

   public IReadOnlyList<ReferenceRow> EntityTypes { get; private set; } = [];

   public IReadOnlyList<ReferenceRow> Sports { get; private set; } = [];

   public IReadOnlyList<ReferenceRow> CountryRelevanceKinds
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<LookupOption> Countries { get; private set; } = [];

   public IReadOnlyList<ReferenceRow> WatchPriorities
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<ReferenceRow> StabilityKinds { get; private set; } = [];

   public IReadOnlyList<LookupOption> PersonGenders { get; private set; } = [];

   public IReadOnlyList<LookupOption> PrimaryCountryParticipationStatuses
   {
      get;
   } =
   [
      new LookupOption(
         PrimaryCountryParticipationStatusIds.RepresentsOtherCountry,
         "Represents another country"
      )
   ];

   public IReadOnlyList<EntityLinkOption> LinkedEntityOptions
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<EntityActivityListItem> Activities
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<SourceReference> Sources
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public string? ImageError { get; private set; }

   [TempData]
   public string? SourceError { get; set; }

   [TempData]
   public string? SourceMessage { get; set; }

   [TempData]
   public string? ImageMessage { get; set; }

   public async Task<IActionResult> OnGetAsync(
      Guid? id,
      CancellationToken cancellationToken
   )
   {
      if(id is null)
      {
         await LoadOptionsAsync(cancellationToken);
         return Page();
      }

      Entity = await repository.GetEntityForEditAsync(
         id.Value,
         cancellationToken
      ) ?? new EntityEditModel();

      await LoadOptionsAsync(cancellationToken);

      return Entity.Id is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnGetThumbnailAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      var thumbnail = await repository.GetEntityPrimaryThumbnailAsync(
         id,
         cancellationToken
      );

      return thumbnail is null
         ? NotFound()
         : File(thumbnail.Data, thumbnail.MimeType);
   }

   public async Task<IActionResult> OnPostReplaceImageAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      var entity = await repository.GetEntityForEditAsync(
         id,
         cancellationToken
      );

      if(entity is null)
      {
         return NotFound();
      }

      var sourceUrl = Entity.PrimaryImageSourceUrl;

      try
      {
         if(string.IsNullOrWhiteSpace(sourceUrl))
         {
            await repository.DeletePrimaryEntityImageAsync(
               id,
               cancellationToken
            );
            ImageMessage = "Image removed.";
         }
         else
         {
            if(!WikimediaCommonsSourceUrl.TryParse(
                  sourceUrl,
                  out var source
               ))
            {
               Entity = entity;
               Entity.PrimaryImageSourceUrl = sourceUrl;
               ModelState.AddModelError(
                  "Entity.PrimaryImageSourceUrl",
                  string.Empty
               );
               await LoadOptionsAsync(cancellationToken);
               return Page();
            }

            await imageReplacementService.ReplaceAsync(
               id,
               source,
               cancellationToken
            );
            ImageMessage = "Image replacement completed.";
         }
      }
      catch(EntityImageReplacementException exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         Entity = entity;
         Entity.PrimaryImageSourceUrl = sourceUrl;
         ImageError = exception.Message;
         await LoadOptionsAsync(cancellationToken);
         return Page();
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         Entity = entity;
         Entity.PrimaryImageSourceUrl = sourceUrl;
         ImageError = this.LogUnexpectedError(exception);
         await LoadOptionsAsync(cancellationToken);
         return Page();
      }

      return RedirectToPage("./Edit", new { id });
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      var isNewPerson = Entity.Id is null && string.Equals(
         Entity.EntityTypeId,
         TrackedEntityTypeIds.Person,
         StringComparison.OrdinalIgnoreCase
      );

      ValidateEntity();

      if(!ModelState.IsValid)
      {
         await LoadOptionsAsync(cancellationToken);
         return Page();
      }

      try
      {
         await repository.SaveEntityAsync(Entity, cancellationToken);

         if(isNewPerson && Entity.Id is not null)
         {
            await automationService.HandlePersonCreatedAsync(
               Entity.Id.Value,
               applicationLifetime.ApplicationStopping
            );
         }
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
         await LoadOptionsAsync(cancellationToken);
         return Page();
      }

      return RedirectToPage("./Index");
   }

   public async Task<IActionResult> OnPostAddSourceAsync(
      Guid id,
      string? sourceUrl,
      CancellationToken cancellationToken
   )
   {
      var entity = await repository.GetEntityForEditAsync(
         id,
         cancellationToken
      );

      if(entity is null)
      {
         return NotFound();
      }

      if(!SourceUrlNormalizer.TryNormalize(sourceUrl, out var normalizedUrl))
      {
         SourceError = "Enter a valid HTTP or HTTPS URL.";
         return RedirectToEdit(id);
      }

      await sourceRepository.CreateAsync(
         SourceCorrelationTypes.Entity,
         id.ToString(),
         SourceKinds.Bio,
         normalizedUrl,
         null,
         null,
         DateTimeOffset.UtcNow,
         cancellationToken
      );
      SourceMessage = "Source added.";

      return RedirectToEdit(id);
   }

   private async Task LoadOptionsAsync(CancellationToken cancellationToken)
   {
      try
      {
         EntityTypes = await repository.GetReferenceRowsAsync(
            "entity-types",
            cancellationToken
         );
         Sports = await repository.GetReferenceRowsAsync(
            "sports",
            cancellationToken
         );
         CountryRelevanceKinds = await repository.GetReferenceRowsAsync(
            "country-relevance-kinds",
            cancellationToken
         );
         Countries = await repository.GetCountryOptionsAsync(
            cancellationToken
         );
         WatchPriorities = await repository.GetReferenceRowsAsync(
            "entity-watch-priorities",
            cancellationToken
         );
         StabilityKinds = await repository.GetReferenceRowsAsync(
            "entity-stability-kinds",
            cancellationToken
         );
         PersonGenders = await repository.GetPersonGenderOptionsAsync(
            cancellationToken
         );
         var entityLinkOptions =
            await repository.GetEntityLinkOptionsByIdsAsync(
               Entity.LinkedEntityIds,
               Entity.Id,
               cancellationToken
            );
         var entityLinkOptionsById = entityLinkOptions
            .ToDictionary(option => option.Id);
         LinkedEntityOptions = Entity.LinkedEntityIds
            .Distinct()
            .Select(id => entityLinkOptionsById.TryGetValue(
               id,
               out var option
            )
               ? option
               : null)
            .Where(option => option is not null)
            .Select(option => option!)
            .ToList();
         Activities = Entity.Id is null
            ? []
            : await repository.GetEntityActivitiesAsync(
               Entity.Id.Value,
               cancellationToken
            );
         Sources = Entity.Id is null
            ? []
            : await sourceRepository.GetByCorrelationAsync(
               SourceCorrelationTypes.Entity,
               Entity.Id.Value.ToString(),
               null,
               cancellationToken
            );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
      }
   }

   private void ValidateEntity()
   {
      if(string.IsNullOrWhiteSpace(Entity.CanonicalName))
      {
         ModelState.AddModelError(
            "Entity.CanonicalName",
            "Canonical name is required."
         );
      }

      if(string.IsNullOrWhiteSpace(Entity.EntityTypeId))
      {
         ModelState.AddModelError(
            "Entity.EntityTypeId",
            "Entity type is required."
         );
      }

      if(string.IsNullOrWhiteSpace(Entity.SportId))
      {
         ModelState.AddModelError("Entity.SportId", "Sport is required.");
      }

      if(string.IsNullOrWhiteSpace(Entity.WatchPriorityId))
      {
         ModelState.AddModelError(
            "Entity.WatchPriorityId",
            "Watch priority is required."
         );
      }

      if(string.IsNullOrWhiteSpace(Entity.ExpectedStabilityId))
      {
         ModelState.AddModelError(
            "Entity.ExpectedStabilityId",
            "Expected stability is required."
         );
      }

      if(string.IsNullOrWhiteSpace(Entity.CountryId))
      {
         ModelState.AddModelError(
            "Entity.CountryId",
            "Country is required."
         );
      }

      if(string.IsNullOrWhiteSpace(Entity.CountryRelevanceKindId))
      {
         ModelState.AddModelError(
            "Entity.CountryRelevanceKindId",
            "Relevance kind is required."
         );
      }

      if(string.IsNullOrWhiteSpace(Entity.CountryRelevanceReason))
      {
         ModelState.AddModelError(
            "Entity.CountryRelevanceReason",
            "Country relevance reason is required."
         );
      }

      if(!string.IsNullOrWhiteSpace(Entity.PersonGenderId) &&
         !string.Equals(
            Entity.EntityTypeId,
            TrackedEntityTypeIds.Person,
            StringComparison.OrdinalIgnoreCase
         ))
      {
         ModelState.AddModelError(
            "Entity.PersonGenderId",
            "Person gender is only valid for person entities."
         );
      }

      if(!string.IsNullOrWhiteSpace(
            Entity.PrimaryCountryParticipationStatusId
         ) &&
         !string.Equals(
            Entity.EntityTypeId,
            TrackedEntityTypeIds.Person,
            StringComparison.OrdinalIgnoreCase
         ))
      {
         ModelState.AddModelError(
            "Entity.PrimaryCountryParticipationStatusId",
            "Participation status is only valid for person entities."
         );
      }

      if(!string.IsNullOrWhiteSpace(
            Entity.PrimaryCountryParticipationStatusId
         ) &&
         !string.Equals(
            Entity.PrimaryCountryParticipationStatusId,
            PrimaryCountryParticipationStatusIds.RepresentsOtherCountry,
            StringComparison.Ordinal
         ))
      {
         ModelState.AddModelError(
            "Entity.PrimaryCountryParticipationStatusId",
            "Select a valid participation status."
         );
      }

      if(!string.IsNullOrWhiteSpace(
            Entity.PrimaryCountryParticipationReason
         ) &&
         string.IsNullOrWhiteSpace(
            Entity.PrimaryCountryParticipationStatusId
         ))
      {
         ModelState.AddModelError(
            "Entity.PrimaryCountryParticipationReason",
            "Participation explanation requires a participation status."
         );
      }
   }

   private IActionResult RedirectToEdit(Guid id)
   {
      return RedirectToPage(
         "./Edit",
         new { id }
      );
   }
}
