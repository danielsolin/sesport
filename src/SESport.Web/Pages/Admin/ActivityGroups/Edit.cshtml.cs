using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data.Models;
using SESport.Data.Repositories;

namespace SESport.Web.Pages.Admin.ActivityGroups;

public class EditModel(ActivityRepository repository) : PageModel
{
   [BindProperty]
   public ActivityGroupEditModel ActivityGroup { get; set; } = new();

   [BindProperty]
   public string? ReturnUrl { get; set; }

   public IReadOnlyList<LookupOption> Sports { get; private set; } = [];

   public IReadOnlyList<ActivityGroupActivityListItem> Activities
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<ActivityGroupSourceListItem> Sources
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      Guid id,
      string? returnUrl,
      CancellationToken cancellationToken
   )
   {
      ReturnUrl = GetLocalReturnUrl(returnUrl);
      ActivityGroup = await repository.GetActivityGroupForEditAsync(
         id,
         cancellationToken
      ) ?? new ActivityGroupEditModel();

      if(ActivityGroup.Id == Guid.Empty)
      {
         return NotFound();
      }

      await LoadPageDataAsync(cancellationToken);
      return Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      ValidateActivityGroup();

      if(!ModelState.IsValid)
      {
         await LoadPageDataAsync(cancellationToken);
         return Page();
      }

      try
      {
         var updated = await repository.UpdateActivityGroupAsync(
            ActivityGroup,
            cancellationToken
         );

         if(!updated)
         {
            return NotFound();
         }
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
         await LoadPageDataAsync(cancellationToken);
         return Page();
      }

      if(ReturnUrl is not null)
      {
         return LocalRedirect(ReturnUrl);
      }

      return RedirectToPage("/Admin/Activities/Index");
   }

   public string GetBackUrl() =>
      ReturnUrl ?? Url.Page("/Admin/Activities/Index")
      ?? "/Admin/Activities";

   private async Task LoadPageDataAsync(
      CancellationToken cancellationToken
   )
   {
      try
      {
         Sports = await repository.GetSportOptionsAsync(cancellationToken);
         Activities = await repository.GetActivitiesForGroupEditAsync(
            ActivityGroup.Id,
            cancellationToken
         );
         Sources = await repository.GetSourcesForGroupEditAsync(
            ActivityGroup.Id,
            cancellationToken
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
         Sports = [];
         Activities = [];
         Sources = [];
      }
   }

   private void ValidateActivityGroup()
   {
      if(string.IsNullOrWhiteSpace(ActivityGroup.Title))
      {
         ModelState.AddModelError(
            "ActivityGroup.Title",
            "Title is required."
         );
      }

      if(string.IsNullOrWhiteSpace(ActivityGroup.SportId))
      {
         ModelState.AddModelError(
            "ActivityGroup.SportId",
            "Sport is required."
         );
      }

      if(ActivityGroup.StartDate is null)
      {
         ModelState.AddModelError(
            "ActivityGroup.StartDate",
            "Start date is required."
         );
      }

      if(ActivityGroup.EndDate is null)
      {
         ModelState.AddModelError(
            "ActivityGroup.EndDate",
            "End date is required."
         );
      }
      else if(
         ActivityGroup.StartDate is not null
         && ActivityGroup.EndDate < ActivityGroup.StartDate
      )
      {
         ModelState.AddModelError(
            "ActivityGroup.EndDate",
            "End date must not be before start date."
         );
      }
   }

   private string? GetLocalReturnUrl(string? returnUrl)
   {
      if(string.IsNullOrWhiteSpace(returnUrl)
         || !Url.IsLocalUrl(returnUrl))
      {
         return null;
      }

      return returnUrl;
   }
}
