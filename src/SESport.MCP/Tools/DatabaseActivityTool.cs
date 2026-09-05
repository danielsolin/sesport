using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Activities;
using SESport.Data.Models;

using System.Globalization;

namespace SESport.MCP.Tools;
using SESport.MCP.Models;

public sealed class DatabaseActivityTool(
   ActivityReadRepository repository,
   ActivityDatabaseToolOptions options
)
{
   [McpServerTool(
      Name = DatabaseToolNames.SearchActivity,
      UseStructuredContent = true
   )]
   [Description(
      "Searches published SESport activities. Supply at least one of " +
      "text, date, or sport. Text and sport searches are " +
      "case-insensitive. Text searches activity titles, group titles, " +
      "participant names, and organization names. Sport is matched " +
      "against the database sport ID, name, and display name. Use the " +
      "returned activity ID with db_get_activity."
   )]
   public async Task<DbActivitySearchResponse> SearchActivityAsync(
      [Description(
         "Optional case-insensitive text for titles, participants, " +
         "or organizations."
      )]
      string? text = null,
      [Description("Optional SESport date in YYYY-MM-DD format.")]
      string? date = null,
      [Description(
         "Optional case-insensitive sport ID, name, or display name " +
         "from the database."
      )]
      string? sport = null,
      [Description("Maximum results. Defaults to the configured limit.")]
      int? limit = null,
      [Description("Number of grouped results to skip. Defaults to zero.")]
      int offset = 0,
      CancellationToken cancellationToken = default
   )
   {
      var normalizedText = Normalize(text);
      var normalizedSport = Normalize(sport);
      if(normalizedText is null &&
         normalizedSport is null &&
         string.IsNullOrWhiteSpace(date))
      {
         throw new ArgumentException(
            "At least one of text, date, or sport must be supplied."
         );
      }

      var parsedDate = ParseDate(date);
      var resolvedLimit = limit ?? options.DefaultSearchLimit;
      ValidateLimit(resolvedLimit);
      if(offset < 0 || offset > options.MaximumSearchOffset)
      {
         throw new ArgumentOutOfRangeException(nameof(offset));
      }

      var page = await repository.SearchAsync(
         normalizedText,
         parsedDate,
         normalizedSport,
         resolvedLimit,
         offset,
         cancellationToken
      );

      return new DbActivitySearchResponse(
         page.Results.Select(MapSearchResult).ToArray(),
         page.HasMore
      );
   }

   [McpServerTool(
      Name = DatabaseToolNames.GetActivity,
      UseStructuredContent = true
   )]
   [Description(
      "Gets one published SESport activity by the UUID returned from " +
      "db_search_activity, including its participants."
   )]
   public async Task<DbActivityGetResponse> GetActivityAsync(
      [Description("The activity UUID returned by db_search_activity.")]
      Guid id,
      CancellationToken cancellationToken = default
   )
   {
      var activity = await repository.GetPublishedAsync(
         id,
         cancellationToken
      );
      return activity is null
         ? new DbActivityGetResponse(false, null)
         : new DbActivityGetResponse(true, MapDetails(activity));
   }

   private void ValidateLimit(int limit)
   {
      if(limit < 1 || limit > options.MaximumSearchLimit)
      {
         throw new ArgumentOutOfRangeException(nameof(limit));
      }
   }

   private static string? Normalize(string? value)
   {
      return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
   }

   private static DateOnly? ParseDate(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      if(DateOnly.TryParseExact(
         value.Trim(),
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

   private static DbActivitySearchResult MapSearchResult(
      ActivitySearchReadModel activity
   )
   {
      return new DbActivitySearchResult(
         activity.Id,
         activity.Title,
         new DbActivityLookup(activity.SportId, activity.SportName),
         new DbActivityLookup(
            activity.ActivityTypeId,
            activity.ActivityTypeName
         ),
         activity.ActivityDate,
         FormatTime(activity.LocalStartTime),
         activity.ParticipantNames
      );
   }

   private static DbActivityDetails MapDetails(ActivityReadModel activity)
   {
      return new DbActivityDetails(
         activity.Id,
         activity.Title,
         activity.Description,
         new DbActivityLookup(activity.SportId, activity.SportName),
         new DbActivityLookup(
            activity.ActivityTypeId,
            activity.ActivityTypeName
         ),
         activity.ActivityDate,
         FormatTime(activity.LocalStartTime),
         FormatTime(activity.LocalEndTime),
         activity.StartsAt,
         activity.EndsAt,
         activity.TimeZoneId,
         activity.ActivityGroup is null
            ? null
            : new DbActivityGroup(
               activity.ActivityGroup.Id,
               activity.ActivityGroup.Title
            ),
         activity.Organization is null
            ? null
            : new DbActivityOrganization(
               activity.Organization.Id,
               activity.Organization.Name
            ),
         activity.Participants
            .Select(participant => new DbActivityParticipant(
               participant.Id,
               participant.Name,
               participant.BirthDate,
               participant.FormativeClub,
               participant.StartTime
            ))
            .ToArray()
      );
   }

   private static string? FormatTime(TimeOnly? value)
   {
      return value?.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
   }
}
