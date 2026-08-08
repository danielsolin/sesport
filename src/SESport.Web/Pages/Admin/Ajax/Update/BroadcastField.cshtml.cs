using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SESport.Core.Broadcast;

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
      CancellationToken cancellationToken,
      Guid? activityGroupId = null
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

         if(string.Equals(field, "description", StringComparison.Ordinal))
         {
            await repository.UpdateDescriptionAsync(
               id,
               string.IsNullOrWhiteSpace(value) ? null : value,
               cancellationToken
            );

            return new JsonResult(new
            {
               updated = true,
               field = "description",
               value = value ?? string.Empty
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

            var broadcast = await repository.GetByIdAsync(
               id,
               cancellationToken
            );

            return new JsonResult(new
            {
               updated = true,
               field = "organization",
               value = organizationEntityId?.ToString() ?? string.Empty,
               organizationEntityId = broadcast?.OrganizationEntityId
                  ?.ToString() ?? string.Empty,
               groupValue = broadcast is null
                  ? string.Empty
                  : BroadcastListDisplayFormatter.FormatGroupValue(
                     broadcast.Title,
                     broadcast.ActivityGroupTitle,
                     broadcast.ActivityGroupDraftTitle
                  ),
               activityGroupId =
                  broadcast?.ActivityGroupId?.ToString() ?? string.Empty,
               activityGroupTitle =
                  broadcast?.ActivityGroupTitle ?? string.Empty,
               activityGroupDraftTitle =
                  broadcast?.ActivityGroupDraftTitle ?? string.Empty,
               activityGroupSourceKindId =
                  broadcast?.ActivityGroupSourceKindId ?? string.Empty,
               groupText = broadcast is null
                  ? "-"
                  : BroadcastListDisplayFormatter.FormatGroupText(
                     broadcast.Title,
                     broadcast.ActivityGroupSourceKindId,
                     broadcast.ActivityGroupId,
                     broadcast.ActivityGroupTitle,
                     broadcast.ActivityGroupDraftTitle
                  )
            });
         }

         if(string.Equals(field, "group", StringComparison.Ordinal))
         {
            var updated = activityGroupId is not null
               ? await repository.UpdateActivityGroupAsync(
                  id,
                  activityGroupId.Value,
                  cancellationToken
               )
               : await repository.UpdateActivityGroupTitleAsync(
                  id,
                  value ?? string.Empty,
                  cancellationToken
               );

            if(!updated)
            {
               return BadRequest(new
               {
                  error = activityGroupId is not null
                     ? "Selected ActivityGroup is not relevant for the " +
                        "selected organization."
                     : "ActivityGroup is not editable yet."
               });
            }

            var broadcast = await repository.GetByIdAsync(
               id,
               cancellationToken
            );

            return new JsonResult(new
            {
               updated = true,
               field = "group",
               value,
               groupValue = broadcast is null
                  ? value
                  : BroadcastListDisplayFormatter.FormatGroupValue(
                     broadcast.Title,
                     broadcast.ActivityGroupTitle,
                     broadcast.ActivityGroupDraftTitle
                  ),
               activityGroupId =
                  broadcast?.ActivityGroupId?.ToString() ?? string.Empty,
               activityGroupTitle =
                  broadcast?.ActivityGroupTitle ?? string.Empty,
               activityGroupDraftTitle =
                  broadcast?.ActivityGroupDraftTitle ?? string.Empty,
               activityGroupSourceKindId =
                  broadcast?.ActivityGroupSourceKindId ?? string.Empty,
               groupText = broadcast is null
                  ? value
                  : BroadcastListDisplayFormatter.FormatGroupText(
                     broadcast.Title,
                     broadcast.ActivityGroupSourceKindId,
                     broadcast.ActivityGroupId,
                     broadcast.ActivityGroupTitle,
                     broadcast.ActivityGroupDraftTitle
                  )
            });
         }

         return BadRequest(new { error = "Unsupported field." });
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         return this.UnexpectedJsonError(exception);
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
