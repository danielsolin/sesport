using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SESport.Data.AI;
using SESport.Web.Pages.Admin.Runs;
using SESport.Web.Services;

namespace SESport.Web.Pages.Admin.Ajax.Update;

public sealed class RunFieldModel(
   AiRepository repository,
   BroadcastParticipationService participationService
) : PageModel
{
   public async Task<IActionResult> OnPostAsync(
      Guid id,
      string field,
      string? value,
      CancellationToken cancellationToken
   )
   {
      try
      {
         field = NormalizeField(field);
         value = value?.Trim();

         if(id == Guid.Empty)
         {
            return BadRequest(new { error = "Run ID is required." });
         }

         if(string.IsNullOrWhiteSpace(field))
         {
            return BadRequest(new { error = "Field is required." });
         }

         if(string.Equals(field, "archive", StringComparison.Ordinal))
         {
            return await ArchiveRunAsync(id, cancellationToken);
         }

         if(!string.Equals(
            field,
            "executionenvironment",
            StringComparison.Ordinal
         ))
         {
            return BadRequest(new { error = "Unsupported field." });
         }

         var run = await repository.GetRunAsync(id, cancellationToken);

         if(run is null)
         {
            return NotFound();
         }

         if(!string.Equals(run.StatusId, "pending", StringComparison.Ordinal))
         {
            return BadRequest(new
            {
               error =
                  "Execution environment can only be changed while the run " +
                  "is pending."
            });
         }

         var executionEnvironments =
            await repository.GetExecutionEnvironmentOptionsAsync(
               cancellationToken
            );
         var requestedExecutionEnvironment =
            string.IsNullOrWhiteSpace(value) ? null : value;

         if(requestedExecutionEnvironment is not null &&
            !executionEnvironments.Contains(
               requestedExecutionEnvironment,
               StringComparer.Ordinal
            ))
         {
            return BadRequest(new
            {
               error = "Select a valid execution environment."
            });
         }

         await repository.UpdateRunExecutionEnvironmentAsync(
            id,
            requestedExecutionEnvironment,
            cancellationToken
         );

         return new JsonResult(new
         {
            updated = true,
            field = "execution-environment",
            value = requestedExecutionEnvironment ?? string.Empty,
            displayValue =
               DetailsModel.FormatExecutionEnvironmentDisplayName(
                  requestedExecutionEnvironment
               )
         });
      }
      catch(Exception exception)
      {
         return new JsonResult(new { error = exception.Message })
         {
            StatusCode = StatusCodes.Status500InternalServerError
         };
      }
   }

   private async Task<IActionResult> ArchiveRunAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      var run = await repository.GetRunAsync(id, cancellationToken);

      if(run is null)
      {
         return NotFound();
      }

      if(!Guid.TryParse(run.CorrelationId, out var broadcastId))
      {
         return BadRequest(new
         {
            error = "Run is not linked to a broadcast."
         });
      }

      if(!await repository.ArchiveRunAsync(id, cancellationToken))
      {
         return NotFound();
      }

      var results =
         await participationService.GetParticipationCheckResultsAsync(
            [broadcastId],
            cancellationToken
         );

      object result = results.Count > 0
         ? results[0]
         : new
         {
            id = broadcastId.ToString(),
            checks = Array.Empty<object>()
         };

      return new JsonResult(new
      {
         updated = true,
         field = "archive",
         result
      });
   }

   private static string NormalizeField(string? field)
   {
      return field?
         .Trim()
         .Replace("-", string.Empty, StringComparison.Ordinal)
         .Replace("_", string.Empty, StringComparison.Ordinal)
         .ToLowerInvariant() ?? string.Empty;
   }
}
