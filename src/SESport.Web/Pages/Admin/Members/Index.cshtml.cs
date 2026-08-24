using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Members;

public class IndexModel(AdminMemberRepository repository) : PageModel
{
   public IReadOnlyList<AdminMemberListItem> Members { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Members = await repository.GetMembersAsync(cancellationToken);
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
      }
   }
}
