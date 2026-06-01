using System.Text.Json;
using SESport.Core.AIActivitySearch;
using SESport.Core.Identifiers;
using SESport.Core.Ingestion;

namespace SESport.Tools.AIActivitySearch.Watchlist;

internal static class EntityWatchlistReader
{
   public static async Task<EntityWatchlistDocument> LoadAsync(
      string dataPath,
      CancellationToken cancellationToken
   )
   {
      await using var stream = File.OpenRead(dataPath);
      var document =
         await JsonSerializer.DeserializeAsync<EntityWatchlistDocument>(
            stream,
            JsonOptions.Value,
            cancellationToken
         );

      return document ?? throw new InvalidOperationException(
         "Entity watchlist was empty."
      );
   }

   public static IEnumerable<ActivitySearchEntity> SelectEntities(
      EntityWatchlistDocument document,
      ToolOptions options
   )
   {
      var entities = document.Entities;

      if (!string.IsNullOrWhiteSpace(options.EntityId))
      {
         entities = entities
            .Where(entity => string.Equals(
               entity.Id,
               options.EntityId,
               StringComparison.OrdinalIgnoreCase
            ))
            .ToList();
      }

      return entities
         .Take(options.Take)
         .Select(ToSearchEntity);
   }

   private static ActivitySearchEntity ToSearchEntity(
      EntityWatchlistEntity entity
   )
   {
      return new ActivitySearchEntity(
         new ExternalEntityId(entity.Id),
         entity.Name,
         entity.Type,
         new ImportedSport(
            new ExternalEntityId(entity.Sport.Id),
            entity.Sport.Name
         ),
         entity.SwedenConnection,
         entity.CurrentRelationshipOrStatus,
         entity.LikelyActivityTypes,
         entity.SuggestedEvidenceSources,
         entity.Notes
      );
   }
}
