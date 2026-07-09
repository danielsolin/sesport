using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Data;

namespace SESport.Web.Pages.Admin.ReferenceData;

public class EditModel(AdminRepository repository) : PageModel
{
   [BindProperty]
   public ReferenceEditModel Row { get; set; } = new();

   [BindProperty]
   public CountryReferenceEditModel Country { get; set; } = new();

   [BindProperty]
   public SportReferenceEditModel Sport { get; set; } = new();

   public ReferenceTableInfo? CurrentTable { get; private set; }

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      string table,
      string? id,
      CancellationToken cancellationToken
   )
   {
      CurrentTable = await repository.GetReferenceTableInfoAsync(
         table,
         cancellationToken
      );

      if(CurrentTable is null)
      {
         return NotFound();
      }

      if(string.IsNullOrWhiteSpace(id))
      {
         return Page();
      }

      if(CurrentTable.Kind == ReferenceTableKind.Countries)
      {
         Country = await repository.GetCountryForEditAsync(
            id,
            cancellationToken
         ) ?? new CountryReferenceEditModel();

         return Country.OriginalId is null ? NotFound() : Page();
      }

      if(CurrentTable.Kind == ReferenceTableKind.Sports)
      {
         Sport = await repository.GetSportForEditAsync(
            id,
            cancellationToken
         ) ?? new SportReferenceEditModel();

         return Sport.OriginalId is null ? NotFound() : Page();
      }

      Row = await repository.GetReferenceForEditAsync(
         table,
         id,
         cancellationToken
      ) ?? new ReferenceEditModel();

      return Row.OriginalId is null ? NotFound() : Page();
   }

   public async Task<IActionResult> OnPostAsync(
      string table,
      CancellationToken cancellationToken
   )
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
         ValidateCountry();

         if(!ModelState.IsValid)
         {
            return Page();
         }

         try
         {
            await repository.SaveCountryAsync(Country, cancellationToken);
         }
         catch(Exception exception)
         {
            LoadError = exception.Message;
            return Page();
         }

         return RedirectToPage("./Index", new { table });
      }

      if(CurrentTable.Kind == ReferenceTableKind.Sports)
      {
         ValidateSport();

         if(!ModelState.IsValid)
         {
            return Page();
         }

         try
         {
            await repository.SaveSportAsync(Sport, cancellationToken);
         }
         catch(Exception exception)
         {
            LoadError = exception.Message;
            return Page();
         }

         return RedirectToPage("./Index", new { table });
      }

      ValidateRow();

      if(!ModelState.IsValid)
      {
         return Page();
      }

      try
      {
         await repository.SaveReferenceAsync(
            table,
            Row,
            cancellationToken
         );
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
         return Page();
      }

      return RedirectToPage("./Index", new { table });
   }

   private void ValidateRow()
   {
      if(string.IsNullOrWhiteSpace(Row.Id))
      {
         ModelState.AddModelError("Row.Id", "ID is required.");
      }

      if(string.IsNullOrWhiteSpace(Row.Label))
      {
         ModelState.AddModelError("Row.Label", "Label is required.");
      }
   }

   private void ValidateCountry()
   {
      if(string.IsNullOrWhiteSpace(Country.Id))
      {
         ModelState.AddModelError("Country.Id", "ID is required.");
      }

      if(string.IsNullOrWhiteSpace(Country.Code))
      {
         ModelState.AddModelError("Country.Code", "Code is required.");
      }

      if(string.IsNullOrWhiteSpace(Country.Name))
      {
         ModelState.AddModelError("Country.Name", "Name is required.");
      }
   }

   private void ValidateSport()
   {
      if(string.IsNullOrWhiteSpace(Sport.Id))
      {
         ModelState.AddModelError("Sport.Id", "ID is required.");
      }

      if(string.IsNullOrWhiteSpace(Sport.Name))
      {
         ModelState.AddModelError("Sport.Name", "Name is required.");
      }
   }
}
