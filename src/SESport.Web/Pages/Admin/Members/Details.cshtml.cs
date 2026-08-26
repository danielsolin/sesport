using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Members;

public sealed class DetailsModel(
   AdminMemberRepository memberRepository,
   MemberWatchRepository watchRepository
) : PageModel
{
   public AdminMemberListItem? Member { get; private set; }

   public IReadOnlyList<MemberPersonListItem> Watches { get; private set; } =
      [];

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      try
      {
         Member = await memberRepository.GetMemberAsync(
            id,
            cancellationToken
         );
         if(Member is null)
         {
            return NotFound();
         }

         Watches = await watchRepository.GetWatchedEntitiesAsync(
            id,
            DateTimeOffset.UtcNow,
            cancellationToken
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
      }

      return Page();
   }
}
