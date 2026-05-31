using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.ReferenceData;

public class IndexModel(AdminRepository repository) : PageModel
{
   public IReadOnlyList<ReferenceTableInfo> Tables { get; private set; } = [];

   public ReferenceTableInfo? CurrentTable { get; private set; }

   public IReadOnlyList<ReferenceRow> Rows { get; private set; } = [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      string? table,
      CancellationToken cancellationToken
   )
   {
      Tables = repository.GetReferenceTables();

      if (string.IsNullOrWhiteSpace(table))
      {
         return Page();
      }

      try
      {
         CurrentTable = await repository.GetReferenceTableInfoAsync(
            table,
            cancellationToken
         );

         if (CurrentTable is null)
         {
            return NotFound();
         }

         Rows = await repository.GetReferenceRowsAsync(
            table,
            cancellationToken
         );
      }
      catch (Exception exception)
      {
         LoadError = exception.Message;
      }

      return Page();
   }

   public async Task<IActionResult> OnPostDeleteAsync(
      string table,
      string id,
      CancellationToken cancellationToken
   )
   {
      await repository.DeleteReferenceAsync(table, id, cancellationToken);
      return RedirectToPage("./Index", new { table });
   }
}
