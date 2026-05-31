using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages;

public class IndexModel(ActivityRepository repository) : PageModel
{
   public IReadOnlyList<ActivityListItem> Activities { get; private set; } =
      [];

   public string? LoadError { get; private set; }

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      try
      {
         Activities = await repository.GetPublishedAsync(cancellationToken);
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }
   }
}
