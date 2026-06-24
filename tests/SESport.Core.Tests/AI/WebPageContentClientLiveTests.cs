using SESport.AI.Providers;

namespace SESport.Core.Tests.AI;

public class WebPageContentClientLiveTests
{
   private const string LiveTestUrl =
      "https://www.lpga.com/tournaments/" +
      "meijer-lpga-classic-for-simply-give/entries";

   private static readonly Uri LiveTestUri = new(LiveTestUrl);
   private static readonly Uri ProtectedLiveTestUri = new(
      "https://www.europeantour.com/dpworld-tour/" +
      "open-d-italia-2026/entry-list"
   );

   [Fact]
   public async Task FetchLpgaEntriesPageDoesNotContainLayoutNoise()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      using var httpClient = CreateHttpClient();
      var client = new WebPageContentClient(httpClient);

      var page = await client.FetchAsync(
         LiveTestUri.ToString(),
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Contains("Meijer LPGA Classic for Simply Give", page!.Title);
      Assert.Contains("Ingrid Lindblad", page.MainText);
      Assert.Contains("SWE", page.MainText);
      Assert.DoesNotContain("0PX", page.MainText);
      Assert.DoesNotContain("SKIP TO MAIN CONTENT", page.MainText);
   }

   [Fact]
   public async Task FetchEuropeanTourEntryListPageIsNotAccessDenied()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      using var httpClient = CreateHttpClient();
      var client = new WebPageContentClient(httpClient);

      var page = await client.FetchAsync(
         ProtectedLiveTestUri.ToString(),
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.NotEqual("Access Denied", page!.Title);
      Assert.DoesNotContain("Access Denied", page.MainText);
      Assert.Contains("Entry List", page.Title);
   }

   [Fact]
   public async Task FetchWrcEntryListPageUsesCurlFallback()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      using var httpClient = CreateHttpClient();
      var client = new WebPageContentClient(httpClient);

      var page = await client.FetchAsync(
         "https://www.wrc.com/en/events/" +
         "wrc-eko-acropolis-rally-greece-2026/" +
         "entry-list-wrc-eko-acropolis-rally-greece-2026",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.NotEqual("Access Denied", page!.Title);
      Assert.DoesNotContain("Access Denied", page.MainText);
      Assert.Contains("WRC EKO Acropolis Rally Greece 2026", page.Title);
      Assert.True(
         page.MainText.Contains(
            "Oliver Solberg",
            StringComparison.OrdinalIgnoreCase
         )
      );
      Assert.True(
         page.MainText.Contains(
            "Mille Johansson",
            StringComparison.OrdinalIgnoreCase
         )
      );
   }

   private static bool ShouldRunLiveTest()
   {
      var enabled = Environment.GetEnvironmentVariable(
         "SESPORT_RUN_LIVE_WEBPAGE_TESTS"
      );

      return string.Equals(
         enabled,
         "1",
         StringComparison.OrdinalIgnoreCase
      ) || string.Equals(
         enabled,
         "true",
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static HttpClient CreateHttpClient()
   {
      var handler = new SocketsHttpHandler
      {
         AutomaticDecompression = System.Net.DecompressionMethods.All
      };

      return new HttpClient(handler)
      {
         Timeout = TimeSpan.FromMinutes(2)
      };
   }
}
