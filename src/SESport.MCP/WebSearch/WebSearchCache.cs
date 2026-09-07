namespace SESport.AI.WebSearch;

public sealed class WebSearchCache
{
   private readonly Lock stateLock = new();
   private readonly Dictionary<WebSearchCacheKey, CacheEntry> entries = [];
   private readonly TimeProvider timeProvider;
   private readonly int maximumEntryCount;

   public WebSearchCache()
      : this(null)
   {
   }

   internal WebSearchCache(
      TimeProvider? timeProvider,
      int maximumEntryCount = WebSearchCacheDefaults.MaximumEntryCount
   )
   {
      this.timeProvider = timeProvider ?? TimeProvider.System;
      this.maximumEntryCount = maximumEntryCount;
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

         if(entry.Response.Results.Count == 0)
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
      if(response.Results.Count == 0)
      {
         return;
      }

      lock(stateLock)
      {
         var now = timeProvider.GetUtcNow();
         foreach(var pair in entries.ToArray())
         {
            if(pair.Value.ExpiresAt <= now)
            {
               entries.Remove(pair.Key);
            }
         }

         if(maximumEntryCount <= 0)
         {
            return;
         }

         if(!entries.ContainsKey(key) && entries.Count >= maximumEntryCount)
         {
            var oldest = entries.MinBy(pair => pair.Value.ExpiresAt);
            entries.Remove(oldest.Key);
         }

         entries[key] = new CacheEntry(
            response,
            now + WebSearchCacheDefaults.DefaultTtl
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
   string Engine,
   bool IncludeSocialMedia = false
);
