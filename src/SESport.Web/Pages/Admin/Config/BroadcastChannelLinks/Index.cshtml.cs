using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SESport.Web.Pages.Admin.Config.BroadcastChannelLinks;

public class IndexModel(AdminRepository repository) : PageModel
{
   public IReadOnlyList<BroadcastChannelLinkRow> Rows
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Rows = await repository.GetBroadcastChannelLinksAsync(
            cancellationToken
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
      }
   }

   public async Task<IActionResult> OnPostDeleteAsync(
      string canonicalName,
      CancellationToken cancellationToken
   )
   {
      try
      {
         await repository.DeleteBroadcastChannelLinkAsync(
            canonicalName,
            cancellationToken
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
         Rows = await repository.GetBroadcastChannelLinksAsync(
            cancellationToken
         );
         return Page();
      }

      return RedirectToPage("./Index");
   }
}
