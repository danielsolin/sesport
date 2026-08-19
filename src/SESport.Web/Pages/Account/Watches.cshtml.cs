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

   public IReadOnlyList<MemberPersonListItem> WatchedEntities {
      get;
      private set;
   } = [];

   public async Task OnGetAsync(CancellationToken cancellationToken)
   {
      var memberId = GetMemberId();
      WatchedEntities = await watchRepository.GetWatchedEntitiesAsync(
         memberId,
         cancellationToken
      );
   }

   public async Task<IActionResult> OnGetSearchAsync(
      string? q,
      CancellationToken cancellationToken
   )
   {
      var query = NormalizeQuery(q);
      var results = query is null
         ? Array.Empty<MemberPersonListItem>()
         : await watchRepository.SearchPeopleAsync(
            query,
            GetMemberId(),
            MaxSearchResults,
            cancellationToken
         );

      return Partial("_WatchSearchResults", results);
   }

   public async Task<IActionResult> OnPostAddAsync(
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      await watchRepository.TryAddEntityWatchAsync(
         GetMemberId(),
         entityId,
         cancellationToken
      );

      return RedirectToPage();
   }

   public async Task<IActionResult> OnPostRemoveAsync(
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      await watchRepository.RemoveEntityWatchAsync(
         GetMemberId(),
         entityId,
         cancellationToken
      );

      return RedirectToPage();
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
