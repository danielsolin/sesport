using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Audit;

public class GroupsModel(AuditRepository repository) : PageModel
{
   public IReadOnlyList<ProposalGroupAuditItem> Groups { get; private set; } =
      [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Groups = await repository.GetProposalGroupsAsync(cancellationToken);
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }
}
