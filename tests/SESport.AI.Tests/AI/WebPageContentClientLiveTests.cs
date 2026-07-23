using System.Diagnostics;
using System.Text.Json;

using Microsoft.Playwright;

using SESport.AI.WebPages;

namespace SESport.Core.Tests.AI;

public class WebPageContentClientLiveTests
{
   private static readonly Uri LiveTestUri = new(
      "https://www.anwagolf.com/en_US/players/player_list.html"
   );
   private static readonly Uri ProtectedLiveTestUri = new(
      "https://www.europeantour.com/dpworld-tour/" +
      "open-d-italia-2026/entry-list"
   );
   private static readonly Uri EspnLiveTestUri = new(
      "https://www.espn.com/golf/leaderboard/_/tournamentId/401863493"
   );
   private static readonly Uri Gt4LiveTestUri = new(
      "https://www.gt4europeanseries.com/entry-list/2026/" +
      "spa-francorchamps"
   );
   private static readonly Uri LpgaLiveTestUri = new(
      "https://www.lpga.com/tournaments/" +
      "kpmgwomenspgachampionship/entries"
   );
   private static readonly Uri WikipediaSupercupLiveTestUri = new(
      "https://en.wikipedia.org/wiki/2026_Porsche_Supercup"
   );
   private static readonly Uri HugoTownsendLiveTestUri = new(
      "https://en.wikipedia.org/wiki/Hugo_Townsend"
   );
   private static readonly Uri FiaFormula2EntryListLiveTestUri = new(
      "https://www.fia.com/events/formula-2-championship/" +
      "season-2026/entry-list"
   );

   [Fact]
   public async Task FetchFiaEntryListExtractsEmbeddedDriverListImage()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      using var httpClient = CreateHttpClient();
      var client = new WebPageContentClient(httpClient);

      var page = await client.FetchAsync(
         FiaFormula2EntryListLiveTestUri.ToString(),
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.True(
         page!.RelevantImages is { Count: > 0 },
         $"Fetcher: {page.Fetcher}; error: {page.FetchErrorMessage}"
      );
      Assert.Contains(
         "1 | Rafael Camara | BRA | Invicta Racing",
         page!.MainTextFull
      );
   }

   [Fact]
   public async Task FetchPlayerListPageKeepsCountryCodes()
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
      Assert.Contains(
         PrimaryCountry.CountryName,
         page!.MainText,
         StringComparison.OrdinalIgnoreCase
      );
      Assert.Contains(PrimaryCountry.ThreeLetterCode, page.MainText);
      Assert.DoesNotContain("SWE_sm", page.MainText);
   }

   [Fact]
   public async Task FetchEspnLeaderboardPageShowsSwedishParticipant()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      using var httpClient = CreateHttpClient();
      var client = new WebPageContentClient(httpClient);

      var page = await client.FetchAsync(
         EspnLiveTestUri.ToString(),
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Contains("Meja Örtengren", page!.MainText);
      Assert.Contains(
         PrimaryCountry.CountryName,
         page.MainText,
         StringComparison.OrdinalIgnoreCase
      );
   }

   [Fact]
   public async Task FetchGt4EntryListPageShowsSwedishParticipants()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      using var httpClient = CreateHttpClient();
      var client = new WebPageContentClient(httpClient);

      var page = await client.FetchAsync(
         Gt4LiveTestUri.ToString(),
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.True(
         page!.MainText.Contains(
            "Daniel NILSSON",
            StringComparison.OrdinalIgnoreCase
         ) ||
         page.MainText.Contains(
            "Maximilian BOSTRÖM",
            StringComparison.OrdinalIgnoreCase
         ) ||
         page.MainText.Contains(
            "Edvin HELLSTEN",
            StringComparison.OrdinalIgnoreCase
         )
      );
      Assert.Contains("SWE", page.MainText);
   }

   [Fact]
   public async Task FetchLpgaEntriesPageShowsSwedishParticipants()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      using var httpClient = CreateHttpClient();
      var client = new WebPageContentClient(httpClient);

      var page = await client.FetchAsync(
         LpgaLiveTestUri.ToString(),
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.True(
         page!.MainText.Contains(
            "Anna Nordqvist",
            StringComparison.OrdinalIgnoreCase
         ) ||
         page.MainText.Contains(
            "Linn Grant",
            StringComparison.OrdinalIgnoreCase
         ) ||
         page.MainText.Contains(
            "Ingrid Lindblad",
            StringComparison.OrdinalIgnoreCase
         ) ||
         page.MainText.Contains(
            "Maja Stark",
            StringComparison.OrdinalIgnoreCase
         )
      );
      Assert.Contains("SWE", page.MainText);
   }

   [Fact]
   public async Task NormalizeWikipediaSupercupHtmlKeepsFlagCountryNames()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      var html = await FetchHtmlViaCurlAsync(
         WikipediaSupercupLiveTestUri,
         CancellationToken.None
      );
      var fragment = ExtractHtmlSnippet(
         html,
         "Luciano Martinez",
         1200
      );
      var tableFragment =
         "<table><tbody>" + fragment + "</tbody></table>";
      var normalizedText = await ApplyNormalizationScriptAsync(
         tableFragment,
         CancellationToken.None
      );

      Assert.NotEmpty(normalizedText);
      Assert.Contains(
         "Luciano Martinez",
         normalizedText,
         StringComparison.OrdinalIgnoreCase
      );
      Assert.Contains(
         "Argentina",
         normalizedText,
         StringComparison.OrdinalIgnoreCase
      );
      Assert.DoesNotContain(
         " icon ",
         normalizedText,
         StringComparison.OrdinalIgnoreCase
      );
   }

   [Fact]
   public async Task FetchWikipediaSupercupPageShowsArgentinaFlagCountryName()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      using var httpClient = CreateHttpClient();
      var client = new WebPageContentClient(httpClient);

      var page = await client.FetchAsync(
         WikipediaSupercupLiveTestUri.ToString(),
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Contains(
         "Luciano Martinez",
         page!.MainTextFull,
         StringComparison.OrdinalIgnoreCase
      );
      Assert.Contains(
         "Argentina",
         page.MainTextFull,
         StringComparison.OrdinalIgnoreCase
      );
   }

   [Fact]
   public async Task FetchWikipediaHugoTownsendPageShowsBirthDate()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      using var httpClient = CreateHttpClient();
      var client = new WebPageContentClient(httpClient);

      var page = await client.FetchAsync(
         HugoTownsendLiveTestUri.ToString(),
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.Contains(
         "18 January 1999",
         page!.MainTextFull,
         StringComparison.OrdinalIgnoreCase
      );
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

   [Fact]
   public async Task FetchFlashscoreSquadPageShowsRosterText()
   {
      if(!ShouldRunLiveTest())
      {
         return;
      }

      using var httpClient = CreateHttpClient();
      var client = new WebPageContentClient(httpClient);

      var page = await client.FetchAsync(
         "https://www.flashscore.se/lag/sverige/2i5WvP7a/trupp/",
         CancellationToken.None
      );

      Assert.NotNull(page);
      Assert.NotEmpty(page!.Title);
      Assert.Contains("Fanny", page.MainText);
      Assert.Contains("Hanna", page.MainText);
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

   private static async Task<string> FetchHtmlViaCurlAsync(
      Uri uri,
      CancellationToken cancellationToken
   )
   {
      var startInfo = new ProcessStartInfo
      {
         FileName = "curl",
         RedirectStandardOutput = true,
         RedirectStandardError = true,
         UseShellExecute = false,
         CreateNoWindow = true
      };

      startInfo.ArgumentList.Add("--silent");
      startInfo.ArgumentList.Add("--show-error");
      startInfo.ArgumentList.Add("--location");
      startInfo.ArgumentList.Add("--compressed");
      startInfo.ArgumentList.Add(uri.ToString());

      using var process = Process.Start(startInfo);

      if(process is null)
      {
         throw new InvalidOperationException("Unable to start curl.");
      }

      using var registration = cancellationToken.Register(() =>
      {
         try
         {
            if(!process.HasExited)
            {
               process.Kill(entireProcessTree: true);
            }
         }
         catch
         {
         }
      });

      var stdoutTask = process.StandardOutput.ReadToEndAsync(
         cancellationToken
      );
      var stderrTask = process.StandardError.ReadToEndAsync(
         cancellationToken
      );

      await process.WaitForExitAsync(cancellationToken);

      var stdout = await stdoutTask;
      _ = await stderrTask;

      if(process.ExitCode != 0)
      {
         throw new InvalidOperationException(
            $"curl failed with exit code {process.ExitCode}."
         );
      }

      return stdout;
   }

   private static async Task<string> ApplyNormalizationScriptAsync(
      string html,
      CancellationToken cancellationToken
   )
   {
      using var playwright = await Playwright.CreateAsync();
      await using var browser = await playwright.Chromium.LaunchAsync(
         new BrowserTypeLaunchOptions
         {
            Headless = true
         }
      );
      await using var context = await browser.NewContextAsync();
      await using var page = await context.NewPageAsync();

      cancellationToken.ThrowIfCancellationRequested();

      await page.SetContentAsync(html);
      await page.EvaluateAsync(
         WebPageNormalizationScript.Build(),
         JsonSerializer.Serialize(
            WebPageContentFetchSupport.CountryNamesByCode
         )
      );

      return await page.Locator("body").InnerTextAsync();
   }

   private static string ExtractHtmlSnippet(
      string html,
      string needle,
      int contextLength
   )
   {
      var index = html.IndexOf(
         needle,
         StringComparison.OrdinalIgnoreCase
      );

      if(index < 0)
      {
         throw new InvalidOperationException(
            $"Unable to find '{needle}' in the live HTML."
         );
      }

      var rowStart = html.LastIndexOf(
         "<tr",
         index,
         StringComparison.OrdinalIgnoreCase
      );
      var rowEnd = html.IndexOf(
         "</tr>",
         index + needle.Length,
         StringComparison.OrdinalIgnoreCase
      );

      if(rowStart >= 0 && rowEnd > rowStart)
      {
         rowEnd += "</tr>".Length;
         return html[rowStart..rowEnd];
      }

      var start = Math.Max(0, index - contextLength);
      var end = Math.Min(html.Length, index + needle.Length + contextLength);
      return html[start..end];
   }
}
