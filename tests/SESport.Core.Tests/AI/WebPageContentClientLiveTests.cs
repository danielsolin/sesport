using SESport.AI.Providers;

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
         "Sweden",
         page!.MainText,
         StringComparison.OrdinalIgnoreCase
      );
      Assert.Contains("SWE", page.MainText);
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
         "Sweden",
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
}
