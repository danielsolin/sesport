using System.Text.Json;

using SESport.Core.AIActivitySearch;
using SESport.Data.Ingestion;
using SESport.Tools.AIActivitySearch.Output;
using SESport.Tools.AIActivitySearch.Watchlist;

namespace SESport.Tools.AIActivitySearch;

internal static class ActivitySearchToolApplication
{
   public static async Task<int> RunAsync(string[] args)
   {
      var options = ToolOptions.Parse(args);

      if(options.ShowHelp)
      {
         PrintHelp();
         return 0;
      }

      var dataPath = ToolPathResolver.ResolveDataPath(options.DataPath);

      if(!File.Exists(dataPath))
      {
         Console.Error.WriteLine($"Entity watchlist not found: {dataPath}");
         return 1;
      }

      var document = await EntityWatchlistReader.LoadAsync(
         dataPath,
         CancellationToken.None
      );
      var selectedEntities = EntityWatchlistReader
         .SelectEntities(document, options)
         .ToList();

      if(selectedEntities.Count == 0)
      {
         Console.Error.WriteLine("No matching entities were found.");
         return 1;
      }

      using var httpClient = new HttpClient
      {
         Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
      };

      var modelClient = ActivitySearchModelClientFactory.Create(
         httpClient,
         options
      );

      var searchService = new ActivitySearchService(modelClient);

      await using var proposalRepository = options.WriteToDatabase
         ? ActivityProposalRepository.Connect(options.ConnectionString)
         : null;

      var results = new List<ActivitySearchResult>();
      var items = new List<ActivitySearchRunItemOutput>();
      var runStartedAt = DateTimeOffset.UtcNow;
      var runDirectory = options.GetRunDirectory(runStartedAt);

      if(runDirectory is not null)
      {
         Directory.CreateDirectory(Path.Combine(runDirectory, "entities"));
         Directory.CreateDirectory(Path.Combine(runDirectory, "failures"));
         Console.Error.WriteLine(
            $"Writing structured run output to {runDirectory}."
         );
      }

      Console.Error.WriteLine(
         $"AI client: {options.ClientMode}, {options.EffectiveBaseAddress}, " +
         $"{options.Model}, API key {options.ApiKeyDescription}."
      );

      var exitCode = options.Overnight
         ? await RunOvernightAsync(
            searchService,
            proposalRepository,
            options,
            runStartedAt,
            runDirectory,
            results,
            items,
            selectedEntities
         )
         : await RunSinglePassAsync(
            searchService,
            proposalRepository,
            options,
            runStartedAt,
            runDirectory,
            results,
            items,
            selectedEntities
         );

      await WriteFinalOutputAsync(
         options,
         runDirectory,
         runStartedAt,
         results,
         items
      );

      return exitCode;
   }

   private static async Task<int> RunSinglePassAsync(
      ActivitySearchService searchService,
      ActivityProposalRepository? proposalRepository,
      ToolOptions options,
      DateTimeOffset runStartedAt,
      string? runDirectory,
      List<ActivitySearchResult> results,
      List<ActivitySearchRunItemOutput> items,
      IReadOnlyList<ActivitySearchEntity> selectedEntities
   )
   {
      var consecutiveFailures = 0;

      for(var enIdx = 0; enIdx < selectedEntities.Count; enIdx++)
      {
         var searchOutcome = await SearchEntityAsync(
            searchService,
            proposalRepository,
            options,
            selectedEntities[enIdx],
            enIdx,
            runStartedAt,
            runDirectory,
            results,
            items,
            consecutiveFailures
         );

         consecutiveFailures = searchOutcome.ConsecutiveFailures;

         if(searchOutcome.StopReason == SearchStopReason.HardStop)
         {
            return 2;
         }

         if(!searchOutcome.ShouldContinue)
         {
            break;
         }

         await DelayBetweenEntitiesAsync(options, enIdx + 1, selectedEntities.Count);
      }

      return 0;
   }

   private static async Task<int> RunOvernightAsync(
      ActivitySearchService searchService,
      ActivityProposalRepository? proposalRepository,
      ToolOptions options,
      DateTimeOffset runStartedAt,
      string? runDirectory,
      List<ActivitySearchResult> results,
      List<ActivitySearchRunItemOutput> items,
      IReadOnlyList<ActivitySearchEntity> selectedEntities
   )
   {
      const int maxPasses = 3;
      var entitiesWithProposals = new HashSet<string>(
         StringComparer.OrdinalIgnoreCase
      );
      var remainingEntities = selectedEntities.ToList();
      var itemIndex = 0;
      var rateLimitBackoff = TimeSpan.FromSeconds(options.DelaySeconds);

      for(var pass = 1; pass <= maxPasses && remainingEntities.Count > 0; pass++)
      {
         Console.Error.WriteLine(
            $"Starting overnight pass {pass}/{maxPasses} for " +
            $"{remainingEntities.Count} entity/entities without proposals."
         );

         var passEntities = remainingEntities;
         remainingEntities = [];

         for(var index = 0; index < passEntities.Count; index++)
         {
            var entity = passEntities[index];
            var searchOutcome = await SearchEntityAsync(
               searchService,
               proposalRepository,
               options,
               entity,
               itemIndex,
               runStartedAt,
               runDirectory,
               results,
               items,
               consecutiveFailures: 0
            );
            itemIndex++;

            if(searchOutcome.StopReason == SearchStopReason.HardStop)
            {
               return 2;
            }

            if(searchOutcome.StopReason == SearchStopReason.Backoff)
            {
               rateLimitBackoff = IncreaseRateLimitBackoff(rateLimitBackoff);
               Console.Error.WriteLine(
                  $"Waiting {rateLimitBackoff.TotalSeconds:0} second(s) " +
                  $"before continuing."
               );
               await Task.Delay(rateLimitBackoff);
            }
            else
            {
               rateLimitBackoff = TimeSpan.FromSeconds(options.DelaySeconds);
            }

            if(searchOutcome.ProposalCount > 0)
            {
               entitiesWithProposals.Add(entity.WatchlistId.Value);
            }
            else if(pass < maxPasses)
            {
               remainingEntities.Add(entity);
            }

            await DelayBetweenEntitiesAsync(options, index + 1, passEntities.Count);
         }
      }

      Console.Error.WriteLine(
         $"Overnight run found proposals for {entitiesWithProposals.Count} " +
         $"of {selectedEntities.Count} selected entity/entities."
      );

      return 0;
   }

   private static async Task<EntitySearchOutcome> SearchEntityAsync(
      ActivitySearchService searchService,
      ActivityProposalRepository? proposalRepository,
      ToolOptions options,
      ActivitySearchEntity entity,
      int entityIndex,
      DateTimeOffset runStartedAt,
      string? runDirectory,
      List<ActivitySearchResult> results,
      List<ActivitySearchRunItemOutput> items,
      int consecutiveFailures
   )
   {
      var windowStart = options.SearchDate.AddDays(-options.LookBackDays);
      var windowEnd = options.SearchDate.AddDays(options.LookAheadDays);
      var itemStartedAt = DateTimeOffset.UtcNow;

      Console.Error.Write(
         $"Searching '{entity.Name}': " +
         $"{windowStart:yyyy-MM-dd}->{windowEnd:yyyy-MM-dd}" +
         $"..."
      );

      try
      {
         var result = await searchService.SearchAsync(
            new ActivitySearchRequest(
               entity,
               options.SearchDate,
               options.MaxProposals,
               options.AllowWebSearch,
               options.LookBackDays,
               options.LookAheadDays
            ),
            CancellationToken.None
         );

         results.Add(result);
         consecutiveFailures = 0;
         var persistedProposalCount = proposalRepository is null
            ? 0
            : await proposalRepository.SaveAsync(
               result.Proposals,
               CancellationToken.None
            );
         var resultPath = runDirectory is null
            ? null
            : await ActivitySearchRunWriter.WriteEntityResultAsync(
               runDirectory,
               entityIndex,
               result,
               options.IncludeRaw,
               CancellationToken.None
            );

         items.Add(ActivitySearchRunItemOutput.Completed(
            entity,
            result.Proposals.Count,
            persistedProposalCount,
            resultPath,
            itemStartedAt,
            DateTimeOffset.UtcNow
         ));

         Console.Error.WriteLine(
            $" {result.Proposals.Count} proposal(s)."
         );

         if(options.WriteToDatabase)
         {
            Console.Error.WriteLine(
               $"Persisted {persistedProposalCount} proposal(s) to database."
            );
         }

         await WriteManifestAfterItemAsync(
            options,
            runDirectory,
            runStartedAt,
            results,
            items
         );

         return new EntitySearchOutcome(
            true,
            consecutiveFailures,
            result.Proposals.Count,
            SearchStopReason.None
         );
      }
      catch(Exception ex)
      {
         consecutiveFailures++;

         var failurePath = runDirectory is null
            ? null
            : await ActivitySearchRunWriter.WriteEntityFailureAsync(
               runDirectory,
               entityIndex,
               entity,
               ex,
               itemStartedAt,
               CancellationToken.None
            );
         items.Add(ActivitySearchRunItemOutput.Failed(
            entity,
            failurePath,
            ex,
            itemStartedAt,
            DateTimeOffset.UtcNow
         ));

         Console.Error.WriteLine($"{ex.Message}");

         var stopReason = GetStopReason(ex);

         if(stopReason == SearchStopReason.HardStop)
         {
            Console.Error.WriteLine(
               "Stopping because the AI provider returned a non-retryable " +
               "HTTP status."
            );

            await WriteManifestAfterItemAsync(
               options,
               runDirectory,
               runStartedAt,
               results,
               items
            );

            return new EntitySearchOutcome(
               false,
               consecutiveFailures,
               0,
               stopReason
            );
         }

         if(!options.ContinueOnError)
         {
            throw;
         }

         if(
            !options.Overnight &&
            consecutiveFailures >= options.StopAfterFailures
         )
         {
            Console.Error.WriteLine(
               $"Stopping after {consecutiveFailures} consecutive failure(s)."
            );

            return new EntitySearchOutcome(
               false,
               consecutiveFailures,
               0,
               stopReason
            );
         }

         await WriteManifestAfterItemAsync(
            options,
            runDirectory,
            runStartedAt,
            results,
            items
         );

         return new EntitySearchOutcome(
            true,
            consecutiveFailures,
            0,
            stopReason
         );
      }
   }

   private static async Task WriteManifestAfterItemAsync(
      ToolOptions options,
      string? runDirectory,
      DateTimeOffset runStartedAt,
      IReadOnlyCollection<ActivitySearchResult> results,
      IReadOnlyCollection<ActivitySearchRunItemOutput> items
   )
   {
      if(runDirectory is null)
      {
         return;
      }

      await ActivitySearchRunWriter.WriteManifestAsync(
         runDirectory,
         ActivitySearchRunOutput.Create(
            options,
            runDirectory,
            runStartedAt,
            DateTimeOffset.UtcNow,
            results,
            items
         ),
         CancellationToken.None
      );
   }

   private static async Task DelayBetweenEntitiesAsync(
      ToolOptions options,
      int completedInCurrentPass,
      int totalInCurrentPass
   )
   {
      if(
         options.DelaySeconds > 0 &&
         completedInCurrentPass < totalInCurrentPass
      )
      {
         await Task.Delay(TimeSpan.FromSeconds(options.DelaySeconds));
      }
   }

   private static TimeSpan IncreaseRateLimitBackoff(TimeSpan current)
   {
      var currentSeconds = Math.Max(5, current.TotalSeconds);
      return TimeSpan.FromSeconds(Math.Min(currentSeconds * 2, 300));
   }

   private static SearchStopReason GetStopReason(Exception exception)
   {
      if(exception is not HttpRequestException httpException)
      {
         return SearchStopReason.None;
      }

      return httpException.StatusCode switch
      {
         System.Net.HttpStatusCode.Unauthorized => SearchStopReason.HardStop,
         System.Net.HttpStatusCode.Forbidden => SearchStopReason.HardStop,
         System.Net.HttpStatusCode.PaymentRequired => SearchStopReason.HardStop,
         System.Net.HttpStatusCode.RequestTimeout => SearchStopReason.Backoff,
         System.Net.HttpStatusCode.Conflict => SearchStopReason.Backoff,
         System.Net.HttpStatusCode.Locked => SearchStopReason.Backoff,
         System.Net.HttpStatusCode.TooManyRequests => SearchStopReason.Backoff,
         >= System.Net.HttpStatusCode.InternalServerError
            and <= (System.Net.HttpStatusCode)599 => SearchStopReason.Backoff,
         _ => SearchStopReason.None
      };
   }

   private static async Task WriteFinalOutputAsync(
      ToolOptions options,
      string? runDirectory,
      DateTimeOffset runStartedAt,
      IReadOnlyCollection<ActivitySearchResult> results,
      IReadOnlyCollection<ActivitySearchRunItemOutput> items
   )
   {
      var runOutput = ActivitySearchRunOutput.Create(
         options,
         runDirectory,
         runStartedAt,
         DateTimeOffset.UtcNow,
         results,
         items
      );

      var output = JsonSerializer.Serialize(runOutput, JsonOptions.Value);

      if(options.OutputPath is not null)
      {
         var outputPath = Path.GetFullPath(options.OutputPath);
         await File.WriteAllTextAsync(outputPath, output);
         Console.Error.WriteLine(
            $"Wrote activity search output to {outputPath}."
         );
      }
      else if(runDirectory is null)
      {
         Console.WriteLine(output);
      }

      if(runDirectory is not null)
      {
         await ActivitySearchRunWriter.WriteManifestAsync(
            runDirectory,
            runOutput,
            CancellationToken.None
         );
         Console.Error.WriteLine(
            $"Wrote activity search manifest to {runDirectory}\\manifest.json."
         );
      }
   }

   private enum SearchStopReason
   {
      None,
      HardStop,
      Backoff
   }

   private sealed record EntitySearchOutcome(
      bool ShouldContinue,
      int ConsecutiveFailures,
      int ProposalCount,
      SearchStopReason StopReason
   );

   private static void PrintHelp()
   {
      Console.WriteLine(
         """
         SESport.AIActivitySearch

         Runs AI activity search for entities in data/entity-watchlist.json.

         Defaults target OpenRouter at https://openrouter.ai/api/v1 with
         openrouter/free.

         Options:
           --entity <id>       Search one watchlist entity by id.
           --take <count>      Number of entities to search when --entity is not
                               set. Default: 1.
           --max <count>       Maximum proposals per entity. Default: 5.
           --date <yyyy-mm-dd> Search date. Default: today.
           --look-back <days>  Days before search date to include. Default: 0.
           --look-ahead <days> Days after search date to include. Default: 30.
           --timeout <seconds> HTTP timeout. Default: 600, or 1200 with
                               --lmstudio-plugin.
           --base-url <url>    OpenAI-compatible /v1 base URL.
                               Default: https://openrouter.ai/api/v1.
           --lmstudio-url <url> LM Studio native /api/v1 base URL.
                               Default: http://127.0.0.1:1234/api/v1.
           --model <name>      Model name. Default: openrouter/free.
           --api-key <key>     API key. Falls back to OPENROUTER_API_KEY for
                               OpenRouter,
                               OPENAI_API_KEY for OpenAI-compatible targets, or
                               LMSTUDIO_API_KEY for LM Studio.
           --web-tool <type>   Web search tool type. Default: web_search.
                               For LM Studio, try altra/web-search.
           --lmstudio-plugin <id>
                               Use LM Studio /api/v1/chat integrations with
                               this plugin id, for example altra/web-search.
           --lmstudio-tools <list>
                               Comma-separated plugin tools. Default: search.
           --no-web-search     Do not include the web_search tool.
           --write-to-db       Persist proposals directly to activity tables.
           --connection-string <value>
                               PostgreSQL connection string for --write-to-db.
           --data <path>       Entity watchlist path.
           --output <path>     Write JSON output to a file instead of stdout.
           --run-dir <path>    Override the structured run directory.
                               Default: data/ai-activity-search-runs/<timestamp>.
          --overnight         Persistent batch mode. Writes a run directory,
                              searches all entities unless --entity or --take is
                              set, retries entities without proposals across up
                              to three passes, continues after unknown errors,
                              stops on 401, 402, and 403, and backs off on
                              transient HTTP statuses.
           --all               Search all entities when --entity is not set.
           --continue-on-error Continue with the next entity after failures.
           --delay <seconds>   Delay between entities. Default: 0, or 5 with
                               --overnight.
          --stop-after-failures <count>
                              Stop after this many consecutive failure(s).
                              Default: unlimited. Ignored by --overnight.
           --include-raw       Include raw model content and full raw response.
           --help              Show this help.
         """
      );
   }
}
