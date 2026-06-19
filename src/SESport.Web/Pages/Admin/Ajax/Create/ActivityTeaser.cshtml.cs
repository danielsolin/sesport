using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Data;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Ajax.Create;

public sealed class ActivityTeaserModel(ActivityEditPageService editService)
   : PageModel
{
   [BindProperty]
   public ActivityEditModel Activity { get; set; } = new();

   public async Task<IActionResult> OnPostAsync(
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(Activity.Title))
      {
         return BadRequest(new
         {
            error = "Title is required before generating a teaser."
         });
      }

      var result = await editService.GenerateTeaserAsync(
         Activity,
         cancellationToken
      );

      if(!string.IsNullOrWhiteSpace(result.ErrorMessage))
      {
         if(string.Equals(
            result.ErrorMessage,
            "The model returned invalid teaser JSON.",
            StringComparison.Ordinal
         ))
         {
            return BadRequest(new
            {
               error = result.ErrorMessage,
               prompt = result.Prompt,
               teaser = result.RawOutputText,
               teaserPreview = result.TeaserPreview
            });
         }

         return BadRequest(new
         {
            error = result.ErrorMessage,
            prompt = result.Prompt
         });
      }

      return new JsonResult(new
      {
         prompt = result.Prompt,
         teaser = result.Teaser
      });
   }
}
