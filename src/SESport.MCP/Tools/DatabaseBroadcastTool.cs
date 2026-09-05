using SESport.Core.Formatting;
using SESport.Data.Broadcasts;
using SESport.Data.Models;

using System.Globalization;

namespace SESport.MCP.Tools;

using SESport.MCP.Models;

public sealed class DatabaseBroadcastTool(
   BroadcastReadRepository repository
)
{
   [McpServerTool(
      Name = McpToolNames.DbGetBroadcast,
      UseStructuredContent = true
   )]
   [Description(
      "Gets the next unprocessed and visible SESport broadcast for the " +
      "selected SESport date. It returns at most one broadcast. Call " +
      "db_update_broadcast after reviewing it, then call this tool again " +
      "until found is false."
   )]
   public async Task<DbBroadcastGetResponse> GetBroadcastAsync(
      [Description("SESport date in YYYY-MM-DD format.")]
      string date,
      CancellationToken cancellationToken = default
   )
   {
      var broadcast = await repository.GetNextUnprocessedAsync(
         ParseDate(date),
         cancellationToken
      );

      return broadcast is null
         ? new DbBroadcastGetResponse(false, null)
         : new DbBroadcastGetResponse(true, MapDetails(broadcast));
   }

   [McpServerTool(
      Name = McpToolNames.DbUpdateBroadcast,
      UseStructuredContent = true
   )]
   [Description(
      "Updates the title and description of one visible unprocessed " +
      "broadcast and atomically marks it as processed. The broadcast is " +
      "not hidden. Use the ID returned by db_get_broadcast."
   )]
   public async Task<DbBroadcastUpdateResponse> UpdateBroadcastAsync(
      [Description("The broadcast UUID returned by db_get_broadcast.")]
      Guid id,
      [Description("The replacement broadcast title.")]
      string title,
      [Description(
         "The replacement broadcast description. Use null to clear it."
      )]
      string? description,
      CancellationToken cancellationToken = default
   )
   {
      if(id == Guid.Empty)
      {
         throw new ArgumentException(
            "A broadcast ID is required.",
            nameof(id)
         );
      }

      var normalizedTitle = NormalizeRequired(title, nameof(title));
      var normalizedDescription = NormalizeOptional(description);
      var broadcast = await repository.UpdateTextAndMarkProcessedAsync(
         id,
         normalizedTitle,
         normalizedDescription,
         cancellationToken
      );

      return broadcast is null
         ? new DbBroadcastUpdateResponse(false, false, null)
         : new DbBroadcastUpdateResponse(true, true, MapDetails(broadcast));
   }

   private static DateOnly ParseDate(string value)
   {
      if(DateOnly.TryParseExact(
         value?.Trim(),
         DateDisplay.DateOnlyFormat,
         CultureInfo.InvariantCulture,
         DateTimeStyles.None,
         out var date
      ))
      {
         return date;
      }

      throw new ArgumentException(
         "date must use the YYYY-MM-DD format.",
         nameof(value)
      );
   }

   private static string NormalizeRequired(string value, string name)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         throw new ArgumentException(
            "The value cannot be empty.",
            name
         );
      }

      return value.Trim();
   }

   private static string? NormalizeOptional(string? value)
   {
      return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
   }

   private static DbBroadcastDetails MapDetails(
      BroadcastReadModel broadcast
   )
   {
      return new DbBroadcastDetails(
         broadcast.Id,
         broadcast.ImportRunId,
         broadcast.SourceKey,
         broadcast.ExternalId,
         broadcast.Fingerprint,
         broadcast.ChannelId,
         broadcast.ChannelName,
         broadcast.Title,
         broadcast.Description,
         broadcast.Categories,
         broadcast.IsReplay,
         broadcast.OriginalAirDate,
         broadcast.StartsAt,
         broadcast.EndsAt,
         broadcast.TimeZoneId,
         broadcast.RawProgrammeXml,
         broadcast.ImageUrl,
         broadcast.CreatedAt,
         broadcast.UpdatedAt,
         broadcast.HiddenAt,
         broadcast.ProcessedAt,
         broadcast.Organization is null
            ? null
            : new DbBroadcastOrganization(
               broadcast.Organization.Id,
               broadcast.Organization.Name,
               broadcast.Organization.SportId,
               broadcast.Organization.SportName
            ),
         broadcast.ActivityGroup is null
            ? null
            : new DbBroadcastActivityGroup(
               broadcast.ActivityGroup.Id,
               broadcast.ActivityGroup.Title,
               broadcast.ActivityGroup.DraftTitle,
               broadcast.ActivityGroup.SourceKindId,
               broadcast.ActivityGroup.SourceActivityId
            ),
         broadcast.Sources
            .Select(source => new DbBroadcastSource(
               source.Kind,
               source.Url,
               source.Title,
               source.Excerpt,
               source.ObservedAt,
               source.CreatedAt
            ))
            .ToArray(),
         broadcast.LinkedActivityIds
      );
   }
}
