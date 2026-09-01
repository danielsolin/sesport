using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Data.Models;

namespace SESport.Web.Pages.Admin.Config.BroadcastChannelLinks;

public class EditModel(AdminRepository repository) : PageModel
{
   [BindProperty]
   public BroadcastChannelLinkEditModel Link { get; set; } = new();

   public string? LoadError { get; private set; }

   public async Task<IActionResult> OnGetAsync(
      string? canonicalName,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(canonicalName))
      {
         return Page();
      }

      var existing = await repository.GetBroadcastChannelLinkAsync(
         canonicalName,
         cancellationToken
      );

      if(existing is null)
      {
         return NotFound();
      }

      Link = new BroadcastChannelLinkEditModel
      {
         OriginalCanonicalName = existing.CanonicalName,
         CanonicalName = existing.CanonicalName,
         Url = existing.Url,
         Aliases = string.Join(", ", existing.Aliases),
         IsActive = existing.IsActive,
      };

      return Page();
   }

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      NormalizeLinkValues();
      ValidateLink();

      if(!ModelState.IsValid)
      {
         return Page();
      }

      var aliases = ParseAliases();

      try
      {
         await ValidateUniqueNamesAsync(aliases, cancellationToken);

         if(!ModelState.IsValid)
         {
            return Page();
         }

         await repository.SaveBroadcastChannelLinkAsync(
            Link.OriginalCanonicalName,
            Link.CanonicalName,
            Link.Url,
            aliases,
            Link.IsActive,
            cancellationToken
         );
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         LoadError = this.LogUnexpectedError(exception);
         return Page();
      }

      return RedirectToPage("./Index");
   }

   private void NormalizeLinkValues()
   {
      Link.CanonicalName = Link.CanonicalName.Trim();
      Link.Url = Link.Url.Trim();
      Link.OriginalCanonicalName = string.IsNullOrWhiteSpace(
         Link.OriginalCanonicalName
      )
         ? null
         : Link.OriginalCanonicalName.Trim();
   }

   private void ValidateLink()
   {
      if(string.IsNullOrWhiteSpace(Link.CanonicalName))
      {
         ModelState.AddModelError("Link.CanonicalName",
            "Channel name is required.");
      }

      if(Link.OriginalCanonicalName is not null &&
         !string.Equals(
            Link.OriginalCanonicalName,
            Link.CanonicalName,
            StringComparison.Ordinal
         ))
      {
         ModelState.AddModelError(
            "Link.CanonicalName",
            "Channel name cannot be changed for an existing link."
         );
      }

      if(string.IsNullOrWhiteSpace(Link.Url))
      {
         ModelState.AddModelError("Link.Url", "URL is required.");
      }
      else if(!Uri.TryCreate(
         Link.Url, UriKind.Absolute, out var uri
      ) || (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps))
      {
         ModelState.AddModelError("Link.Url",
            "URL must be a valid http(s) address.");
      }
   }

   private IReadOnlyList<string> ParseAliases()
   {
      return Link.Aliases
         .Split(',', StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries)
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   private async Task ValidateUniqueNamesAsync(
      IReadOnlyList<string> aliases,
      CancellationToken cancellationToken
   )
   {
      var names = new[] { Link.CanonicalName }
         .Concat(aliases)
         .Select(NormalizeChannelName)
         .ToArray();

      if(names.Any(string.IsNullOrWhiteSpace))
      {
         ModelState.AddModelError(
            "Link.Aliases",
            "The channel name and aliases must not be empty."
         );
         return;
      }

      if(names.Length != names.Distinct(
            StringComparer.OrdinalIgnoreCase
         ).Count())
      {
         ModelState.AddModelError(
            "Link.Aliases",
            "The channel name and aliases must be unique."
         );
         return;
      }

      var rows = await repository.GetBroadcastChannelLinksAsync(
         cancellationToken
      );
      var conflictingRow = rows.FirstOrDefault(row =>
         !string.Equals(
            row.CanonicalName,
            Link.OriginalCanonicalName,
            StringComparison.Ordinal
         ) && names.Any(name => row.Matches(name))
      );

      if(conflictingRow is not null)
      {
         ModelState.AddModelError(
            "Link.CanonicalName",
            "The channel name or an alias already belongs to another link."
         );
      }
   }

   private static string NormalizeChannelName(string value)
   {
      return PrimaryCountry.NormalizeBroadcastChannelName(value).Trim();
   }
}
