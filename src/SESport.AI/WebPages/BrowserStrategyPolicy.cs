namespace SESport.AI.WebPages;

/// <summary>
/// Process-wide browser strategy policy. A strategy that succeeded for an
/// origin becomes the preferred strategy for that origin; it is never
/// consumed. Launch failures mark a strategy temporarily unavailable, but
/// prior calls can never cause every strategy to be skipped: when all
/// strategies are unavailable the configured default order is returned.
/// </summary>
internal sealed class BrowserStrategyPolicy
{
   private static readonly object StateLock = new();
   private static readonly Dictionary<string, OriginPreference>
      PreferencesByOrigin = new(StringComparer.OrdinalIgnoreCase);
   private static readonly Dictionary<string, DateTimeOffset>
      UnavailableUntilByStrategy = new(StringComparer.Ordinal);

   internal IReadOnlyList<BrowserStrategyDescriptor> GetStrategies(
      Uri url
   )
   {
      var ordered = new List<BrowserStrategyDescriptor>();

      lock(StateLock)
      {
         PruneStaleState();

         var originKey = GetOriginKey(url);
         PreferencesByOrigin.TryGetValue(
            originKey,
            out var preference
         );

         var preferred =
            preference?.StrategyId is { } preferredId
               ? BrowserStrategyDescriptor.All.FirstOrDefault(
                  strategy => strategy.Id == preferredId
               )
               : null;

         if(preferred is not null &&
            !IsUnavailable(preferred.Id))
         {
            ordered.Add(preferred);
         }

         foreach(var strategy in BrowserStrategyDescriptor.All)
         {
            if(strategy == preferred || IsUnavailable(strategy.Id))
            {
               continue;
            }

            ordered.Add(strategy);
         }

         if(ordered.Count == 0)
         {
            // Every strategy is temporarily unavailable. Reset the
            // cooldowns so a fetch can never be skipped because of
            // historical launch failures alone.
            UnavailableUntilByStrategy.Clear();
            ordered.AddRange(BrowserStrategyDescriptor.All);
         }
      }

      return ordered;
   }

   internal void ReportSuccess(Uri url, string strategyId)
   {
      lock(StateLock)
      {
         PruneStaleState();
         TrimOriginsIfNeeded();

         PreferencesByOrigin[GetOriginKey(url)] = new OriginPreference(
            strategyId,
            DateTimeOffset.UtcNow
         );
      }
   }

   internal void ReportLaunchFailure(string strategyId)
   {
      lock(StateLock)
      {
         PruneStaleState();

         UnavailableUntilByStrategy[strategyId] =
            DateTimeOffset.UtcNow +
            WebPageFetchDefaults.BrowserLaunchFailureCooldown;
      }
   }

   private static void PruneStaleState()
   {
      var now = DateTimeOffset.UtcNow;

      foreach(var key in PreferencesByOrigin
         .Where(entry => now - entry.Value.Timestamp >
            WebPageFetchDefaults.BrowserPreferenceLifetime)
         .Select(entry => entry.Key)
         .ToArray())
      {
         PreferencesByOrigin.Remove(key);
      }

      foreach(var key in UnavailableUntilByStrategy
         .Where(entry => now >= entry.Value)
         .Select(entry => entry.Key)
         .ToArray())
      {
         UnavailableUntilByStrategy.Remove(key);
      }
   }

   private static void TrimOriginsIfNeeded()
   {
      while(PreferencesByOrigin.Count >=
         WebPageFetchDefaults
            .BrowserPreferenceMaximumOriginCount)
      {
         var oldest = PreferencesByOrigin
            .OrderBy(entry => entry.Value.Timestamp)
            .FirstOrDefault();

         if(oldest.Key is null)
         {
            return;
         }

         PreferencesByOrigin.Remove(oldest.Key);
      }
   }

   private static bool IsUnavailable(string strategyId)
   {
      return UnavailableUntilByStrategy.TryGetValue(
         strategyId,
         out var unavailableUntil
      ) && DateTimeOffset.UtcNow < unavailableUntil;
   }

   private static string GetOriginKey(Uri url)
   {
      return $"{url.Scheme}://{url.Authority}".ToLowerInvariant();
   }

   private sealed record OriginPreference(string StrategyId,
      DateTimeOffset Timestamp);
}
