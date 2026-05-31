using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Audit;

public class ProposalsModel(AuditRepository repository) : PageModel
{
   public IReadOnlyList<ActivityProposalAuditItem> Proposals
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<ActivityProposalLinkAuditItem> Links
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<ActivityProposalEvidenceAuditItem> Evidence
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Proposals = await repository.GetProposalsAsync(cancellationToken);
         Links = await repository.GetProposalLinksAsync(cancellationToken);
         Evidence = await repository.GetProposalEvidenceAsync(
            cancellationToken
         );
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }
}
