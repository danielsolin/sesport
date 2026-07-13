using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Broadcast;
using SESport.Data;

namespace SESport.Web.Pages.Admin.Ajax.Update;

public sealed class BroadcastFieldModel(
   AdminBroadcastRepository repository,
   AdminRepository adminRepository
) : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      Guid id,
      string field,
      string? value,
      CancellationToken cancellationToken
   )
   {
      field = field?.Trim().ToLowerInvariant() ?? string.Empty;
      value = value?.Trim();

      if(id == Guid.Empty)
      {
         return BadRequest(new { error = "Broadcast ID is required." });
      }

      if(string.IsNullOrWhiteSpace(field))
      {
         return BadRequest(new { error = "Field is required." });
      }

      try
      {
         if(string.Equals(field, "title", StringComparison.Ordinal))
         {
            if(string.IsNullOrWhiteSpace(value))
            {
               return BadRequest(new { error = "Title cannot be empty." });
            }

            await repository.UpdateTitleAsync(
               id,
               value,
               cancellationToken
            );

            return new JsonResult(new
            {
               updated = true,
               field = "title",
               value
            });
         }

         if(string.Equals(field, "categories", StringComparison.Ordinal))
         {
            var categories = NormalizeCategories(value);

            await repository.UpdateCategoriesAsync(
               id,
               categories,
               cancellationToken
            );

            return new JsonResult(new
            {
               updated = true,
               field = "categories",
               value = categories
            });
         }

         if(string.Equals(field, "organization", StringComparison.Ordinal))
         {
            var organizationEntityId =
               await ValidateOrganizationEntityIdAsync(
                  value,
                  cancellationToken
               );

            if(!string.IsNullOrWhiteSpace(value) &&
               organizationEntityId is null)
            {
               return BadRequest(new
               {
                  error = "Selected organization is invalid."
               });
            }

            await repository.UpdateOrganizationAsync(
               id,
               organizationEntityId,
               cancellationToken
            );

            return new JsonResult(new
            {
               updated = true,
               field = "organization",
               value = organizationEntityId?.ToString() ?? string.Empty
            });
         }

         return BadRequest(new { error = "Unsupported field." });
      }
      catch(Exception exception)
      {
         return new JsonResult(new { error = exception.Message })
         {
            StatusCode = StatusCodes.Status500InternalServerError
         };
      }
   }

   private async Task<Guid?> ValidateOrganizationEntityIdAsync(
      string? value,
      CancellationToken cancellationToken
   )
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      if(!Guid.TryParse(value, out var entityId))
      {
         return null;
      }

      var entity = await adminRepository.GetEntityForEditAsync(
         entityId,
         cancellationToken
      );

      if(entity is null ||
         !BroadcastEntityFilter.IsOrganizationEntityType(
            entity.EntityTypeId
         ))
      {
         return null;
      }

      return entityId;
   }

   private static string[] NormalizeCategories(string? value)
   {
      return value?
         .Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries
               | StringSplitOptions.TrimEntries
         )
         .Where(category => !string.IsNullOrWhiteSpace(category))
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToArray() ?? [];
   }
}
