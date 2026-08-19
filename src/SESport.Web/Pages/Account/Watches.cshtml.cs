using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

using SESport.Core.Members;
using SESport.Data.Models;
using SESport.Data.Repositories;

namespace SESport.Web.Pages.Account;

[Authorize(AuthenticationSchemes = MemberAuthenticationDefaults.Scheme)]
public sealed class WatchesModel(
   MemberWatchRepository watchRepository
) : PageModel
{
   private const int MaxSearchResults = 5;

   [BindProperty(SupportsGet = true, Name = "q")]
   public string? Query { get; set; }

   public IReadOnlyList<MemberPersonListItem> SearchResults {
      get;
      private set;
   } = [];

   public IReadOnlyList<MemberPersonListItem> WatchedEntities {
      get;
      private set;
   } = [];

   public bool HasSearch => !string.IsNullOrWhiteSpace(Query);

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      Query = NormalizeQuery(Query);
      var memberId = GetMemberId();
      WatchedEntities = await watchRepository.GetWatchedEntitiesAsync(
         memberId,
         cancellationToken
      );

      if(!HasSearch)
      {
         return;
      }

      SearchResults = await watchRepository.SearchPeopleAsync(
         Query!,
         memberId,
         MaxSearchResults,
         cancellationToken
      );
   }

   public async Task<IActionResult> OnPostAddAsync(
      Guid entityId,
      string? q,
      CancellationToken cancellationToken
   )
   {
      await watchRepository.TryAddEntityWatchAsync(
         GetMemberId(),
         entityId,
         cancellationToken
      );

      return RedirectToPage(
         new { q = NormalizeQuery(q) }
      );
   }

   public async Task<IActionResult> OnPostRemoveAsync(
      Guid entityId,
      string? q,
      CancellationToken cancellationToken
   )
   {
      await watchRepository.RemoveEntityWatchAsync(
         GetMemberId(),
         entityId,
         cancellationToken
      );

      return RedirectToPage(
         new { q = NormalizeQuery(q) }
      );
   }

   private Guid GetMemberId()
   {
      var memberIdValue = User.FindFirstValue(
         MemberClaimTypes.MemberId
      );
      return Guid.TryParse(memberIdValue, out var memberId)
         ? memberId
         : throw new InvalidOperationException(
            "The member authentication claim is missing."
         );
   }

   private static string? NormalizeQuery(string? query)
   {
      var normalizedQuery = query?.Trim();
      return string.IsNullOrWhiteSpace(normalizedQuery)
         ? null
         : normalizedQuery;
   }
}
