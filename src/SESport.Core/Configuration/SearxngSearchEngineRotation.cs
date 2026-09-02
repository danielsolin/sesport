using System.Collections.ObjectModel;

namespace SESport.Core.Configuration;

public static class SearxngSearchEngineRotation
{
   public static IReadOnlyList<string> NormalizeEngines(
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

   public static string GetEngineForAttempt(
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

   public static int GetEngineCount(IReadOnlyList<string>? engines)
   {
      return NormalizeEngines(engines).Count;
   }

   private static string NormalizeEngine(string engine)
   {
      return engine.Trim().ToLowerInvariant();
   }
}
