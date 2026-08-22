using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Core.Broadcast;
using SESport.Core.Formatting;
using SESport.Web.Pages.Admin.Broadcasts;
using System.Globalization;

namespace SESport.Web.Pages.Admin.Ajax.Update;

public sealed class BroadcastFieldModel(
   AdminBroadcastRepository repository,
   AdminRepository adminRepository,
   BroadcastParticipationService? participationService = null
) : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      Guid id,
      string field,
      string? value,
      CancellationToken cancellationToken,
      Guid? activityGroupId = null,
      [FromForm(Name = RouteKeys.Date)] DateOnly? date = null,
      [FromForm(Name = RouteKeys.SortColumn)] string? sortColumn = null,
      [FromForm(Name = RouteKeys.SortAsc)] bool sortAsc = true,
      [FromForm(Name = RouteKeys.ShowHidden)] bool showHidden = false,
      [FromForm(Name = RouteKeys.HideReplays)] bool hideReplays = false,
      [FromForm(Name = RouteKeys.SelectedSports)]
         List<string>? selectedSports = null
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

            return await RenderRowOrJsonAsync(
               id,
               date,
               sortColumn,
               sortAsc,
               showHidden,
               hideReplays,
               selectedSports,
               new
               {
                  updated = true,
                  field = "title",
                  value
               },
               cancellationToken
            );
         }

         if(string.Equals(field, "channel", StringComparison.Ordinal))
         {
            if(string.IsNullOrWhiteSpace(value))
            {
               return BadRequest(new { error = "Channel cannot be empty." });
            }

            await repository.UpdateChannelAsync(
               id,
               value,
               cancellationToken
            );

            return await RenderRowOrJsonAsync(
               id,
               date,
               sortColumn,
               sortAsc,
               showHidden,
               hideReplays,
               selectedSports,
               new
               {
                  updated = true,
                  field = "channel",
                  value
               },
               cancellationToken
            );
         }

         if(string.Equals(field, "start-time", StringComparison.Ordinal)
            || string.Equals(field, "end-time", StringComparison.Ordinal))
         {
            if(!TimeOnly.TryParseExact(
               value ?? string.Empty,
               DateDisplay.TimeOnlyMinutesFormat,
               CultureInfo.InvariantCulture,
               DateTimeStyles.None,
               out var parsedTime
            ))
            {
               return BadRequest(new { error = "Time must use HH:mm." });
            }

            var timeUpdate = string.Equals(
               field,
               "start-time",
               StringComparison.Ordinal
            )
               ? await repository.UpdateStartTimeAsync(
                  id,
                  parsedTime,
                  cancellationToken
               )
               : await repository.UpdateEndTimeAsync(
                  id,
                  parsedTime,
                  cancellationToken
               );

            if(timeUpdate is null)
            {
               var broadcast = await repository.GetByIdAsync(
                  id,
                  cancellationToken
               );

               if(broadcast is null)
               {
                  return NotFound(new { error = "Broadcast not found." });
               }

               var error = field == "start-time"
                  ? "Start time must be before end time."
                  : "End time must be after start time.";
               return BadRequest(new { error });
            }

            var updatedValue = field == "start-time"
               ? timeUpdate.StartTimeText
               : timeUpdate.EndTimeText;

            return await RenderRowOrJsonAsync(
               id,
               date,
               sortColumn,
               sortAsc,
               showHidden,
               hideReplays,
               selectedSports,
               new
               {
                  updated = true,
                  field,
                  value = updatedValue,
                  startTimeText = timeUpdate.StartTimeText,
                  endTimeText = timeUpdate.EndTimeText
               },
               cancellationToken
            );
         }

         if(string.Equals(field, "description", StringComparison.Ordinal))
         {
            await repository.UpdateDescriptionAsync(
               id,
               string.IsNullOrWhiteSpace(value) ? null : value,
               cancellationToken
            );

            return await RenderRowOrJsonAsync(
               id,
               date,
               sortColumn,
               sortAsc,
               showHidden,
               hideReplays,
               selectedSports,
               new
               {
                  updated = true,
                  field = "description",
                  value = value ?? string.Empty
               },
               cancellationToken
            );
         }

         if(string.Equals(field, "categories", StringComparison.Ordinal))
         {
            var categories = NormalizeCategories(value);

            await repository.UpdateCategoriesAsync(
               id,
               categories,
               cancellationToken
            );

            return await RenderRowOrJsonAsync(
               id,
               date,
               sortColumn,
               sortAsc,
               showHidden,
               hideReplays,
               selectedSports,
               new
               {
                  updated = true,
                  field = "categories",
                  value = categories
               },
               cancellationToken
            );
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

            if(!WantsHtmlResponse())
            {
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
                  activityGroupTitle = broadcast?.ActivityGroupTitle
                     ?? string.Empty,
                  activityGroupDraftTitle = broadcast?.ActivityGroupDraftTitle
                     ?? string.Empty,
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

            return await RenderRowAsync(
               id,
               date,
               sortColumn,
               sortAsc,
               showHidden,
               hideReplays,
               selectedSports,
               cancellationToken
            );
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

            if(!WantsHtmlResponse())
            {
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
                  activityGroupTitle = broadcast?.ActivityGroupTitle
                     ?? string.Empty,
                  activityGroupDraftTitle = broadcast?.ActivityGroupDraftTitle
                     ?? string.Empty,
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

            return await RenderRowAsync(
               id,
               date,
               sortColumn,
               sortAsc,
               showHidden,
               hideReplays,
               selectedSports,
               cancellationToken
            );
         }

         return BadRequest(new { error = "Unsupported field." });
      }
      catch(Exception exception)
         when(!cancellationToken.IsCancellationRequested)
      {
         return this.UnexpectedJsonError(exception);
      }
   }

   private async Task<IActionResult> RenderRowOrJsonAsync(
      Guid id,
      DateOnly? date,
      string? sortColumn,
      bool sortAsc,
      bool showHidden,
      bool hideReplays,
      IReadOnlyList<string>? selectedSports,
      object jsonPayload,
      CancellationToken cancellationToken
   )
   {
      if(!WantsHtmlResponse())
      {
         return new JsonResult(jsonPayload);
      }

      return await RenderRowAsync(
         id,
         date,
         sortColumn,
         sortAsc,
         showHidden,
         hideReplays,
         selectedSports,
         cancellationToken
      );
   }

   private async Task<IActionResult> RenderRowAsync(
      Guid id,
      DateOnly? date,
      string? sortColumn,
      bool sortAsc,
      bool showHidden,
      bool hideReplays,
      IReadOnlyList<string>? selectedSports,
      CancellationToken cancellationToken
   )
   {
      var broadcast = await repository.GetByIdAsync(id, cancellationToken);

      if(broadcast is null)
      {
         return NotFound(new { error = "Broadcast not found." });
      }

      var broadcasts = participationService is null
         ? [broadcast]
         : await participationService.ApplyParticipationChecksAsync(
            [broadcast],
            cancellationToken
         );

      return Partial(
         "/Pages/Admin/Broadcasts/_BroadcastRow.cshtml",
         BroadcastRowViewModel.Create(
            broadcasts[0],
            Url,
            Request,
            date,
            sortColumn,
            sortAsc,
            showHidden,
            hideReplays,
            selectedSports,
            ViewData["SearchUrl"] as string
         )
      );
   }

   private bool WantsHtmlResponse() =>
      PageContext?.HttpContext?.Request.Headers.Accept.ToString().Contains(
         "text/html",
         StringComparison.OrdinalIgnoreCase
      ) == true;

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
         !BroadcastEntityFilter.IsBroadcastOrganizationEntityType(
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
