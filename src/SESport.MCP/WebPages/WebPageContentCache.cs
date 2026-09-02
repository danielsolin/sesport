namespace SESport.AI.WebPages;

public sealed class WebPageContentCache
{
   private readonly Lock stateLock = new();
   private readonly Dictionary<string, CacheEntry> entries = [];
   private readonly Dictionary<string, InFlightEntry> inFlight = [];
   private readonly TimeProvider timeProvider;
   private readonly TimeSpan timeToLive;
   private readonly int maximumEntryCount;

   public WebPageContentCache()
      : this(null)
   {
   }

   internal WebPageContentCache(
      TimeProvider? timeProvider,
      TimeSpan? timeToLive = null,
      int? maximumEntryCount = null
   )
   {
      this.timeProvider = timeProvider ?? TimeProvider.System;
      this.timeToLive = timeToLive ?? WebPageCacheDefaults.DefaultTtl;
      this.maximumEntryCount = maximumEntryCount ??
         WebPageCacheDefaults.MaximumEntryCount;
   }

   public async Task<WebPageContent?> GetOrFetchAsync(
      Uri absoluteUrl,
      CancellationToken cancellationToken,
      Func<CancellationToken, Task<WebPageContent?>> fetcher
   )
   {
      ArgumentNullException.ThrowIfNull(absoluteUrl);
      ArgumentNullException.ThrowIfNull(fetcher);
      cancellationToken.ThrowIfCancellationRequested();

      var key = WebPageUrlPolicy.GetCanonicalCacheKey(absoluteUrl);
      TaskCompletionSource<WebPageContent?>? completion = null;
      Task<WebPageContent?> fetchTask;

      lock(stateLock)
      {
         RemoveExpiredEntries(timeProvider.GetUtcNow());

         if(entries.TryGetValue(key, out var entry))
         {
            return entry.Content;
         }

         if(!inFlight.TryGetValue(key, out var inFlightEntry))
         {
            completion = CreateCompletionSource();
            inFlightEntry = new InFlightEntry(completion);
            inFlight[key] = inFlightEntry;
         }

         fetchTask = inFlightEntry.Completion.Task;
      }

      if(completion is not null)
      {
         _ = PopulateAsync(key, fetcher, completion);
      }

      return await fetchTask.WaitAsync(cancellationToken);
   }

   private async Task PopulateAsync(
      string key,
      Func<CancellationToken, Task<WebPageContent?>> fetcher,
      TaskCompletionSource<WebPageContent?> completion
   )
   {
      try
      {
         var content = await fetcher(CancellationToken.None);

         if(content is not null && IsCacheable(content))
         {
            Store(key, content);
         }

         completion.TrySetResult(content);
      }
      catch(OperationCanceledException exception)
      {
         completion.TrySetCanceled(
            exception.CancellationToken.CanBeCanceled
               ? exception.CancellationToken
               : new CancellationToken(true)
         );
      }
      catch(Exception exception)
      {
         completion.TrySetException(exception);
      }
      finally
      {
         lock(stateLock)
         {
            if(inFlight.TryGetValue(key, out var current) &&
               ReferenceEquals(current.Completion, completion))
            {
               inFlight.Remove(key);
            }
         }
      }
   }

   private static TaskCompletionSource<WebPageContent?>
      CreateCompletionSource()
   {
      var completion = new TaskCompletionSource<WebPageContent?>(
         TaskCreationOptions.RunContinuationsAsynchronously
      );
      _ = completion.Task.ContinueWith(
         completed => _ = completed.Exception,
         CancellationToken.None,
         TaskContinuationOptions.OnlyOnFaulted |
            TaskContinuationOptions.ExecuteSynchronously,
         TaskScheduler.Default
      );
      return completion;
   }

   private void Store(string key, WebPageContent content)
   {
      var now = timeProvider.GetUtcNow();

      lock(stateLock)
      {
         RemoveExpiredEntries(now);

         if(maximumEntryCount <= 0)
         {
            return;
         }

         if(!entries.ContainsKey(key) &&
            entries.Count >= maximumEntryCount)
         {
            RemoveOldestEntry();
         }

         entries[key] = new CacheEntry(
            content,
            now + timeToLive,
            now
         );
      }
   }

   private void RemoveExpiredEntries(DateTimeOffset now)
   {
      foreach(var pair in entries.ToArray())
      {
         if(pair.Value.ExpiresAt <= now)
         {
            entries.Remove(pair.Key);
         }
      }
   }

   private void RemoveOldestEntry()
   {
      string? oldestKey = null;
      DateTimeOffset oldestTime = DateTimeOffset.MaxValue;

      foreach(var pair in entries)
      {
         if(pair.Value.StoredAt < oldestTime)
         {
            oldestKey = pair.Key;
            oldestTime = pair.Value.StoredAt;
         }
      }

      if(oldestKey is not null)
      {
         entries.Remove(oldestKey);
      }
   }

   private static bool IsCacheable(WebPageContent? content)
   {
      return content is not null &&
         content.HasBodyText &&
         !string.IsNullOrWhiteSpace(content.MainTextFull) &&
         content.MainTextFull.Length <=
            WebPageCacheDefaults.MaximumCacheableTextCharacters &&
         string.IsNullOrWhiteSpace(content.FetchErrorMessage) &&
         content.FetchErrorKind is null &&
         string.IsNullOrWhiteSpace(content.RenderWarning);
   }

   private sealed record CacheEntry(
      WebPageContent Content,
      DateTimeOffset ExpiresAt,
      DateTimeOffset StoredAt
   );

   private sealed record InFlightEntry(
      TaskCompletionSource<WebPageContent?> Completion
   );
}
