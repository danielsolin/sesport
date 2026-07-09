using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Data;

namespace SESport.Web.Pages.Admin.ReferenceData;

public class IndexModel(
   AdminRepository repository
) : PageModel
{
   public IReadOnlyList<ReferenceNavigationItem> NavigationItems
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<ReferenceTableInfo> Tables { get; private set; } = [];

   public ReferenceTableInfo? CurrentTable { get; private set; }

   public IReadOnlyList<ReferenceRow> Rows { get; private set; } = [];

   public IReadOnlyList<CountryReferenceRow> CountryRows
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<SportReferenceRow> SportRows
   {
      get;
      private set;
   } = [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      string? table,
      CancellationToken cancellationToken
   )
   {
      NavigationItems = repository.GetReferenceNavigationItems();
      Tables = repository.GetReferenceTables();

      if(string.IsNullOrWhiteSpace(table))
      {
         return Page();
      }

      try
      {
         CurrentTable = await repository.GetReferenceTableInfoAsync(
            table,
            cancellationToken
         );

         if(CurrentTable is null)
         {
            return NotFound();
         }

         if(CurrentTable.Kind == ReferenceTableKind.Countries)
         {
            CountryRows = await repository.GetCountryReferenceRowsAsync(
               cancellationToken
            );
            return Page();
         }

         if(CurrentTable.Kind == ReferenceTableKind.Sports)
         {
            SportRows = await repository.GetSportReferenceRowsAsync(
               cancellationToken
            );
            return Page();
         }

         Rows = await repository.GetReferenceRowsAsync(
            table,
            cancellationToken
         );
      }
      catch(Exception exception)
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
      var tableInfo = await repository.GetReferenceTableInfoAsync(
         table,
         cancellationToken
      );

      if(tableInfo?.Kind == ReferenceTableKind.Countries)
      {
         await repository.DeleteCountryAsync(id, cancellationToken);
         return RedirectToPage("./Index", new { table });
      }

      if(tableInfo?.Kind == ReferenceTableKind.Sports)
      {
         await repository.DeleteSportAsync(id, cancellationToken);
         return RedirectToPage("./Index", new { table });
      }

      await repository.DeleteReferenceAsync(table, id, cancellationToken);
      return RedirectToPage("./Index", new { table });
   }
}
