using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Data;

namespace SESport.Web.Pages.Admin.Ajax.Search;

public sealed class EntityModel(AdminRepository repository) : PageModel
{
   public async Task<IActionResult> OnGetAsync(
      string? term,
      bool organizationOnly,
      CancellationToken cancellationToken
   )
   {
      term = term?.Trim() ?? string.Empty;

      if(term == string.Empty)
      {
         return new JsonResult(new { results = Array.Empty<object>() });
      }

      var options = organizationOnly
         ? await repository.SearchBroadcastOrganizationLinkOptionsAsync(
            term,
            cancellationToken
         )
         : await repository.SearchBroadcastOrganizationLinkOptionsAsync(
            term,
            cancellationToken
         );

      var results = options
         .Select(option => new
         {
            id = option.Id,
            text = $"{option.Name} ({option.EntityType}/{option.Sport})"
         })
         .ToList();

      return new JsonResult(new { results });
   }
}
