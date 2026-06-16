using System.Reflection;

using SESport.AI.Providers;

namespace SESport.Core.Tests.AI;

public class WebPageContentClientLiveTests
{
   private const string LiveTestUrl =
      "https://www.lpga.com/tournaments/" +
      "meijer-lpga-classic-for-simply-give/entries";

   private static readonly Uri LiveTestUri = new(LiveTestUrl);

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
         LiveTestUrl,
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
   public async Task ExtractLpgaSupplementalTextIncludesPlayerData()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      using var httpClient = CreateHttpClient();
      var rawHtml = await httpClient.GetStringAsync(LiveTestUri);
      var supplementalText = InvokeSupplementalText(rawHtml);

      Console.WriteLine(
         supplementalText[..Math.Min(2000, supplementalText.Length)]
      );
      Assert.Contains("Ingrid Lindblad", supplementalText);
      Assert.Contains("SWE", supplementalText);
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
         Timeout = TimeSpan.FromSeconds(30)
      };
   }

   private static string InvokeSupplementalText(string rawHtml)
   {
      var method = typeof(WebPageContentClient).GetMethod(
         "ExtractSupplementalText",
         BindingFlags.NonPublic | BindingFlags.Static
      );

      if(method is null)
      {
         throw new InvalidOperationException(
            "ExtractSupplementalText was not found."
         );
      }

      return (string?)method.Invoke(null, new object[] { rawHtml })
         ?? string.Empty;
   }
}
