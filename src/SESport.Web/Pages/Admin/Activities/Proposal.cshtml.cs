using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Web.Data;

namespace SESport.Web.Pages.Admin.Activities;

public class ProposalModel(AuditRepository repository) : PageModel
{
   public ActivityProposalDetail Proposal { get; private set; } = default!;

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

   public IReadOnlyList<RejectReasonOption> RejectReasons
   {
      get;
      private set;
   } = [];

   [BindProperty]
   public string RejectReasonId { get; set; } = "";

   [BindProperty]
   public string? RejectComment { get; set; }

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      return await LoadAsync(id, cancellationToken);
   }

   public async Task<IActionResult> OnPostAcceptAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var activityId = await repository.AcceptProposalAsync(
            id,
            cancellationToken
         );

         return RedirectToPage(
            "/Admin/Activities/Edit",
            new { id = activityId }
         );
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
         return await LoadAsync(id, cancellationToken);
      }
   }

   public async Task<IActionResult> OnPostRejectAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(RejectReasonId))
      {
         ModelState.AddModelError(
            nameof(RejectReasonId),
            "Reject reason is required."
         );
      }

      if(!ModelState.IsValid)
      {
         return await LoadAsync(id, cancellationToken);
      }

      try
      {
         await repository.RejectProposalAsync(
            id,
            RejectReasonId,
            RejectComment,
            cancellationToken
         );

         return RedirectToPage("./Proposals");
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
         return await LoadAsync(id, cancellationToken);
      }
   }

   private async Task<IActionResult> LoadAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      try
      {
         var proposal = await repository.GetProposalAsync(
            id,
            cancellationToken
         );

         if(proposal is null)
         {
            return NotFound();
         }

         Proposal = proposal;
         RejectReasonId = string.IsNullOrWhiteSpace(RejectReasonId)
            ? ""
            : RejectReasonId;
         Links = await repository.GetProposalLinksAsync(id, cancellationToken);
         Evidence = await repository.GetProposalEvidenceAsync(
            id,
            cancellationToken
         );
         RejectReasons = await repository.GetRejectReasonsAsync(
            cancellationToken
         );

         return Page();
      }
      catch(Exception exception)
      {
         LoadError = exception.Message;
         return Page();
      }
   }
}
