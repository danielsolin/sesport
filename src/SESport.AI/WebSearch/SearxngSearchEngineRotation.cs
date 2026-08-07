using System.Collections.ObjectModel;

namespace SESport.AI.WebSearch;

internal static class SearxngSearchEngineRotation
{
   internal static IReadOnlyList<string> NormalizeEngines(
      IReadOnlyList<string>? engines
   )
   {
      var normalizedEngines = engines?
         .Select(NormalizeEngine)
         .Where(engine => !string.IsNullOrWhiteSpace(engine))
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();

      if(normalizedEngines is null || normalizedEngines.Count == 0)
      {
         return SearxngWebSearchClientOptions.DefaultEngines;
      }

      return new ReadOnlyCollection<string>(normalizedEngines);
   }

   internal static string GetEngineForAttempt(
      IReadOnlyList<string>? engines,
      int searchAttempt
   )
   {
      var normalizedEngines = NormalizeEngines(engines);

      var index = searchAttempt % normalizedEngines.Count;

      if(index < 0)
      {
         index += normalizedEngines.Count;
      }

      return normalizedEngines[index];
   }

   internal static int GetEngineCount(IReadOnlyList<string>? engines)
   {
      return NormalizeEngines(engines).Count;
   }

   private static string NormalizeEngine(string engine)
   {
      return engine.Trim().ToLowerInvariant();
   }
}
