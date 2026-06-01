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
      var consecutiveFailures = 0;

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

         if(!searchOutcome.ShouldContinue)
         {
            break;
         }

         if(
            options.DelaySeconds > 0 &&
            enIdx + 1 < selectedEntities.Count
         )
         {
            await Task.Delay(TimeSpan.FromSeconds(options.DelaySeconds));
         }
      }

      await WriteFinalOutputAsync(
         options,
         runDirectory,
         runStartedAt,
         results,
         items
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

      Console.Error.WriteLine(
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
            $"Found {result.Proposals.Count} proposal(s) for {entity.Name}."
         );
         if(options.WriteToDatabase)
         {
            Console.Error.WriteLine(
               $"Persisted {persistedProposalCount} proposal(s) to database."
            );
         }
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

         Console.Error.WriteLine(
            $"Failed {entity.Name} ({entity.WatchlistId.Value}): {ex.Message}"
         );

         if(!options.ContinueOnError)
         {
            throw;
         }

         if(consecutiveFailures >= options.StopAfterFailures)
         {
            Console.Error.WriteLine(
               $"Stopping after {consecutiveFailures} consecutive failure(s)."
            );

            return new EntitySearchOutcome(false, consecutiveFailures);
         }
      }

      if(runDirectory is not null)
      {
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

      return new EntitySearchOutcome(true, consecutiveFailures);
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

   private sealed record EntitySearchOutcome(
      bool ShouldContinue,
      int ConsecutiveFailures
   );

   private static void PrintHelp()
   {
      Console.WriteLine(
         """
         SESport.AIActivitySearch

         Runs AI activity search for entities in data/entity-watchlist.json.

         Defaults target OpenRouter at https://openrouter.ai/api/v1 with
         openai/gpt-oss-20b.

         Options:
           --entity <id>       Search one watchlist entity by id.
           --take <count>      Number of entities to search when --entity is not
                               set. Default: 1.
           --max <count>       Maximum proposals per entity. Default: 5.
           --date <yyyy-mm-dd> Search date. Default: today.
           --look-back <days>  Days before search date to include. Default: 0.
           --look-ahead <days> Days after search date to include. Default: 30.
           --timeout <seconds> HTTP timeout. Default: 100, or 300 with
                               --lmstudio-plugin.
           --base-url <url>    OpenAI-compatible /v1 base URL.
                               Default: https://openrouter.ai/api/v1.
           --lmstudio-url <url> LM Studio native /api/v1 base URL.
                               Default: http://127.0.0.1:1234/api/v1.
           --model <name>      Model name. Default: openai/gpt-oss-20b.
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
           --overnight         Safe batch mode. Writes a run directory, searches
                               all entities unless --entity or --take is set,
                               continues after errors, waits between entities,
                               and stops after repeated failures.
           --all               Search all entities when --entity is not set.
           --continue-on-error Continue with the next entity after failures.
           --delay <seconds>   Delay between entities. Default: 0, or 5 with
                               --overnight.
           --stop-after-failures <count>
                               Stop after this many consecutive failure(s).
                               Default: unlimited, or 5 with --overnight.
           --include-raw       Include raw model content and full raw response.
           --help              Show this help.
         """
      );
   }
}
