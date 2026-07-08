namespace SESport.AI.WebSearch;

public sealed class WebSearchCache
{
   private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(30);

   private readonly Lock stateLock = new();
   private readonly Dictionary<WebSearchCacheKey, CacheEntry> entries = [];
   private readonly TimeProvider timeProvider;

   public WebSearchCache()
      : this(null)
   {
   }

   internal WebSearchCache(TimeProvider? timeProvider)
   {
      this.timeProvider = timeProvider ?? TimeProvider.System;
   }

   public bool TryGet(
      WebSearchCacheKey key,
      out WebSearchResponse response
   )
   {
      lock(stateLock)
      {
         if(!entries.TryGetValue(key, out var entry))
         {
            response = new WebSearchResponse([]);
            return false;
         }

         if(entry.ExpiresAt <= timeProvider.GetUtcNow())
         {
            entries.Remove(key);
            response = new WebSearchResponse([]);
            return false;
         }

         response = entry.Response;
         return true;
      }
   }

   public void Store(WebSearchCacheKey key, WebSearchResponse response)
   {
      lock(stateLock)
      {
         entries[key] = new CacheEntry(
            response,
            timeProvider.GetUtcNow() + DefaultTtl
         );
      }
   }

   private sealed record CacheEntry(
      WebSearchResponse Response,
      DateTimeOffset ExpiresAt
   );
}

public sealed record WebSearchCacheKey(
   string Query,
   int MaxResults,
   string Engine
);
