using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.ReferenceData;

public class IndexModel(
   AdminRepository repository,
   AuditRepository auditRepository
) : PageModel
{
   public IReadOnlyList<ReferenceNavigationItem> NavigationItems
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<ReferenceTableInfo> Tables { get; private set; } = [];

   public ReferenceTableInfo? CurrentTable { get; private set; }

   public string? CurrentSpecialView { get; private set; }

   public IReadOnlyList<ReferenceRow> Rows { get; private set; } = [];

   public IReadOnlyList<ActivityLinkAuditItem> ActivityLinks
   {
      get;
      private set;
   } = [];

   public IReadOnlyList<ActivityEvidenceAuditItem> ActivityEvidence
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

         if (CurrentTable.Kind == ReferenceTableKind.ActivityAudit)
         {
            CurrentSpecialView = CurrentTable.Id;
            ActivityLinks = await auditRepository.GetActivityLinksAsync(
               cancellationToken
            );
            ActivityEvidence = await auditRepository.GetActivityEvidenceAsync(
               cancellationToken
            );
            return Page();
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
