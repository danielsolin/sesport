using SESport.AI.WebSearch;

namespace SESport.Core.Tests.AI;

public sealed class WebSearchCacheTests
{
   [Fact]
   public void CapacityEvictsOldestEntry()
   {
      var clock = new TestClock();
      var cache = new WebSearchCache(clock, 2);
      var first = Key("first");
      var second = Key("second");
      var third = Key("third");
      cache.Store(first, Response());
      clock.Advance(TimeSpan.FromSeconds(1));
      cache.Store(second, Response());
      clock.Advance(TimeSpan.FromSeconds(1));
      cache.Store(third, Response());

      Assert.False(cache.TryGet(first, out _));
      Assert.True(cache.TryGet(second, out _));
      Assert.True(cache.TryGet(third, out _));
   }

   [Fact]
   public void RefreshAtCapacityPreservesOtherEntries()
   {
      var cache = new WebSearchCache(null, 2);
      var first = Key("first");
      var second = Key("second");
      cache.Store(first, Response());
      cache.Store(second, Response());
      var updated = Response();
      cache.Store(first, updated);

      Assert.True(cache.TryGet(first, out var actual));
      Assert.Same(updated, actual);
      Assert.True(cache.TryGet(second, out _));
   }

   [Fact]
   public void ExpiredResultsAreNotReturned()
   {
      var clock = new TestClock();
      var cache = new WebSearchCache(clock, 2);
      var key = Key("expired");
      cache.Store(key, Response());
      clock.Advance(WebSearchCacheDefaults.DefaultTtl);

      Assert.False(cache.TryGet(key, out _));
      cache.Store(Key("fresh"), Response());
      Assert.True(cache.TryGet(Key("fresh"), out _));
   }

   private static WebSearchCacheKey Key(string query)
   {
      return new WebSearchCacheKey(query, 1, "test");
   }

   private static WebSearchResponse Response()
   {
      return new WebSearchResponse(
         [new WebSearchResult("Result", "https://example.test/", null)]
      );
   }

   private sealed class TestClock : TimeProvider
   {
      private DateTimeOffset now = DateTimeOffset.UnixEpoch;

      public override DateTimeOffset GetUtcNow() => now;

      public void Advance(TimeSpan duration)
      {
         now += duration;
      }
   }
}
