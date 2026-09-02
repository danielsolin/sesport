using SESport.AI.WebPages;

namespace SESport.Core.Tests.AI;

public class WebPageContentCacheTests
{
   [Fact]
   public async Task CachesCleanContent()
   {
      var cache = new WebPageContentCache();
      var url = new Uri("https://example.test/article");
      var calls = 0;
      var expected = CreateContent(url);

      Func<CancellationToken, Task<WebPageContent?>> fetcher = token =>
      {
         calls++;
         return Task.FromResult<WebPageContent?>(expected);
      };

      var first = await cache.GetOrFetchAsync(
         url,
         CancellationToken.None,
         fetcher
      );
      var second = await cache.GetOrFetchAsync(
         new Uri("https://example.test/article#section"),
         CancellationToken.None,
         fetcher
      );

      Assert.Same(expected, first);
      Assert.Same(expected, second);
      Assert.Equal(1, calls);
   }

   [Fact]
   public async Task DoesNotCacheWarningBearingContent()
   {
      var cache = new WebPageContentCache();
      var url = new Uri("https://example.test/partial");
      var calls = 0;

      Func<CancellationToken, Task<WebPageContent?>> fetcher = token =>
      {
         calls++;
         return Task.FromResult<WebPageContent?>(
            CreateContent(url) with
            {
               RenderWarning = "Rendered content may be incomplete."
            }
         );
      };

      await cache.GetOrFetchAsync(url, CancellationToken.None, fetcher);
      await cache.GetOrFetchAsync(url, CancellationToken.None, fetcher);

      Assert.Equal(2, calls);
   }

   [Fact]
   public async Task ConcurrentRequestsShareOneFetch()
   {
      var cache = new WebPageContentCache();
      var url = new Uri("https://example.test/concurrent");
      var fetchStarted = new TaskCompletionSource(
         TaskCreationOptions.RunContinuationsAsynchronously
      );
      var releaseFetch = new TaskCompletionSource<WebPageContent?>(
         TaskCreationOptions.RunContinuationsAsynchronously
      );
      var calls = 0;

      Func<CancellationToken, Task<WebPageContent?>> fetcher = async token =>
      {
         calls++;
         fetchStarted.SetResult();
         return await releaseFetch.Task;
      };

      var firstTask = cache.GetOrFetchAsync(
         url,
         CancellationToken.None,
         fetcher
      );
      await fetchStarted.Task;
      var secondTask = cache.GetOrFetchAsync(
         url,
         CancellationToken.None,
         fetcher
      );

      var expected = CreateContent(url);
      releaseFetch.SetResult(expected);

      Assert.Same(expected, await firstTask);
      Assert.Same(expected, await secondTask);
      Assert.Equal(1, calls);
   }

   [Fact]
   public async Task CallerCancellationDoesNotCancelSharedFetch()
   {
      var cache = new WebPageContentCache();
      var url = new Uri("https://example.test/cancellation");
      var fetchStarted = new TaskCompletionSource(
         TaskCreationOptions.RunContinuationsAsynchronously
      );
      var releaseFetch = new TaskCompletionSource<WebPageContent?>(
         TaskCreationOptions.RunContinuationsAsynchronously
      );

      Func<CancellationToken, Task<WebPageContent?>> fetcher = async token =>
      {
         fetchStarted.SetResult();
         return await releaseFetch.Task;
      };

      var firstTask = cache.GetOrFetchAsync(
         url,
         CancellationToken.None,
         fetcher
      );
      await fetchStarted.Task;

      using var cancellation = new CancellationTokenSource();
      var canceledTask = cache.GetOrFetchAsync(
         url,
         cancellation.Token,
         fetcher
      );
      cancellation.Cancel();

      await Assert.ThrowsAnyAsync<OperationCanceledException>(
         () => canceledTask
      );

      var expected = CreateContent(url);
      releaseFetch.SetResult(expected);
      Assert.Same(expected, await firstTask);
   }

   private static WebPageContent CreateContent(Uri url)
   {
      return new WebPageContent(
         "Test page",
         url.ToString(),
         null,
         [],
         "Useful body text.",
         true,
         "Useful body text."
      );
   }
}
