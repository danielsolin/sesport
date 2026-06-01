using System.Text.Json;
using SESport.Core.AIActivitySearch;

namespace SESport.Tools.AIActivitySearch.Output;

internal static class ActivitySearchRunWriter
{
   public static async Task WriteManifestAsync(
      string runDirectory,
      ActivitySearchRunOutput output,
      CancellationToken cancellationToken
   )
   {
      await File.WriteAllTextAsync(
         Path.Combine(runDirectory, "manifest.json"),
         JsonSerializer.Serialize(output, JsonOptions.Value),
         cancellationToken
      );
   }

   public static async Task<string> WriteEntityResultAsync(
      string runDirectory,
      int entityIndex,
      ActivitySearchResult result,
      bool includeRaw,
      CancellationToken cancellationToken
   )
   {
      var relativePath = Path.Combine(
         "entities",
         CreateEntityFileName(entityIndex, result.Entity)
      );
      var output = ActivitySearchResultOutput.From(result, includeRaw);

      await File.WriteAllTextAsync(
         Path.Combine(runDirectory, relativePath),
         JsonSerializer.Serialize(output, JsonOptions.Value),
         cancellationToken
      );

      return NormalizePath(relativePath);
   }

   public static async Task<string> WriteEntityFailureAsync(
      string runDirectory,
      int entityIndex,
      ActivitySearchEntity entity,
      Exception exception,
      DateTimeOffset startedAt,
      CancellationToken cancellationToken
   )
   {
      var relativePath = Path.Combine(
         "failures",
         CreateEntityFileName(entityIndex, entity)
      );
      var output = ActivitySearchFailureOutput.From(
         entity,
         exception,
         startedAt,
         DateTimeOffset.UtcNow
      );

      await File.WriteAllTextAsync(
         Path.Combine(runDirectory, relativePath),
         JsonSerializer.Serialize(output, JsonOptions.Value),
         cancellationToken
      );

      return NormalizePath(relativePath);
   }

   private static string CreateEntityFileName(
      int entityIndex,
      ActivitySearchEntity entity
   )
   {
      var entityId = SanitizeFileName(entity.WatchlistId.Value);

      return $"{entityIndex + 1:0000}-{entityId}.json";
   }

   private static string SanitizeFileName(string value)
   {
      var invalidCharacters = Path.GetInvalidFileNameChars();
      var characters = value.Select(character =>
         invalidCharacters.Contains(character) ? '-' : character
      );

      return string.Concat(characters);
   }

   private static string NormalizePath(string value)
   {
      return value.Replace('\\', '/');
   }
}
