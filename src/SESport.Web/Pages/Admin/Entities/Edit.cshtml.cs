using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SESport.Data;

namespace SESport.Web.Pages.Admin.Entities;

public class EditModel(AdminRepository repository) : PageModel
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

   public IReadOnlyList<SelectListItem> EntityLinkOptions
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<EntityActivityListItem> Activities
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      Guid? id,
      CancellationToken cancellationToken
   )
   {
      if (id is null)
      {
         await LoadOptionsAsync(null, cancellationToken);
         return Page();
      }

      Entity = await repository.GetEntityForEditAsync(
         id.Value,
         cancellationToken
      ) ?? new EntityEditModel();

      await LoadOptionsAsync(Entity.Id, cancellationToken);

      return Entity.Id is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      ValidateEntity();

      if (!ModelState.IsValid)
      {
         await LoadOptionsAsync(Entity.Id, cancellationToken);
         return Page();
      }

      try
      {
         await repository.SaveEntityAsync(Entity, cancellationToken);
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
         await LoadOptionsAsync(Entity.Id, cancellationToken);
         return Page();
      }

      return RedirectToPage("./Index");
   }

   private async Task LoadOptionsAsync(
      Guid? entityId,
      CancellationToken cancellationToken
   )
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
         var entityLinkOptions = await repository.GetEntityLinkOptionsAsync(
            entityId,
            cancellationToken
         );
         EntityLinkOptions = entityLinkOptions
            .Select(entity => new SelectListItem(
               $"{entity.Name} ({entity.EntityType}, {entity.Sport})",
               entity.Id.ToString()
            ))
            .ToList();
         Activities = entityId is null
            ? []
            : await repository.GetEntityActivitiesAsync(
               entityId.Value,
               cancellationToken
            );
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }

   private void ValidateEntity()
   {
      if (string.IsNullOrWhiteSpace(Entity.CanonicalName))
      {
         ModelState.AddModelError(
            "Entity.CanonicalName",
            "Canonical name is required."
         );
      }

      if (string.IsNullOrWhiteSpace(Entity.EntityTypeId))
      {
         ModelState.AddModelError(
            "Entity.EntityTypeId",
            "Entity type is required."
         );
      }

      if (string.IsNullOrWhiteSpace(Entity.SportId))
      {
         ModelState.AddModelError("Entity.SportId", "Sport is required.");
      }

      if (string.IsNullOrWhiteSpace(Entity.WatchPriorityId))
      {
         ModelState.AddModelError(
            "Entity.WatchPriorityId",
            "Watch priority is required."
         );
      }

      if (string.IsNullOrWhiteSpace(Entity.ExpectedStabilityId))
      {
         ModelState.AddModelError(
            "Entity.ExpectedStabilityId",
            "Expected stability is required."
         );
      }

      if (string.IsNullOrWhiteSpace(Entity.CountryId))
      {
         ModelState.AddModelError(
            "Entity.CountryId",
            "Country is required."
         );
      }

      if (string.IsNullOrWhiteSpace(Entity.CountryRelevanceKindId))
      {
         ModelState.AddModelError(
            "Entity.CountryRelevanceKindId",
            "Relevance kind is required."
         );
      }

      if (string.IsNullOrWhiteSpace(Entity.CountryRelevanceReason))
      {
         ModelState.AddModelError(
            "Entity.CountryRelevanceReason",
            "Country relevance reason is required."
         );
      }

      if (!string.IsNullOrWhiteSpace(Entity.PersonGenderId) &&
         !string.Equals(
            Entity.EntityTypeId,
            "Person",
            StringComparison.OrdinalIgnoreCase
         ))
      {
         ModelState.AddModelError(
            "Entity.PersonGenderId",
            "Person gender is only valid for person entities."
         );
      }
   }
}
