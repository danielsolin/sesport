using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Entities;

public class MergeModel(AdminRepository repository) : PageModel
{
   [BindProperty(SupportsGet = true)]
   public Guid SourceId { get; set; }

   [BindProperty(SupportsGet = true)]
   public Guid? TargetEntityId { get; set; }

   [BindProperty]
   public bool ConfirmMerge { get; set; }

   public EntityEditModel? Source { get; private set; }

   public EntityMergePreview? Preview { get; private set; }

   public IReadOnlyList<SelectListItem> TargetOptions
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      CancellationToken cancellationToken
   )
   {
      await LoadAsync(cancellationToken);

      return Source is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      if(TargetEntityId is null)
      {
         ModelState.AddModelError(
            nameof(TargetEntityId),
            "Target entity is required."
         );
      }

      if(!ConfirmMerge)
      {
         ModelState.AddModelError(
            nameof(ConfirmMerge),
            "Confirm that the source entity should be merged."
         );
      }

      if(!ModelState.IsValid)
      {
         await LoadAsync(cancellationToken);
         return Page();
      }

      try
      {
         await repository.MergeEntityAsync(
            SourceId,
            TargetEntityId!.Value,
            cancellationToken
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
         await LoadAsync(cancellationToken);
         return Page();
      }

      return RedirectToPage(
         "./Edit",
         new
         {
            id = TargetEntityId.Value
         }
      );
   }

   private async Task LoadAsync(CancellationToken cancellationToken)
   {
      try
      {
         Source = await repository.GetEntityForEditAsync(
            SourceId,
            cancellationToken
         );

         var options = await repository.GetEntityLinkOptionsAsync(
            SourceId,
            cancellationToken
         );
         TargetOptions = options
            .Select(entity => new SelectListItem(
               $"{entity.Name} ({entity.EntityType}, {entity.Sport})",
               entity.Id.ToString(),
               entity.Id == TargetEntityId
            ))
            .ToList();

         if(TargetEntityId is not null)
         {
            Preview = await repository.GetEntityMergePreviewAsync(
               SourceId,
               TargetEntityId.Value,
               cancellationToken
            );

            if(Preview is null)
            {
               LoadError = "Unable to load merge preview.";
            }
         }
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
      }
   }
}
