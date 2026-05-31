using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Audit;

public class ActivitiesModel(AuditRepository repository) : PageModel
{
   public IReadOnlyList<ActivityLinkAuditItem> Links { get; private set; } = [];

   public IReadOnlyList<ActivityEvidenceAuditItem> Evidence
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Links = await repository.GetActivityLinksAsync(cancellationToken);
         Evidence = await repository.GetActivityEvidenceAsync(
            cancellationToken
         );
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }
}
