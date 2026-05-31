using System.Text.Json;
using System.Text.Json.Serialization;
using SESport.Core.AIActivitySearch;
using SESport.Core.Domain;
using SESport.Core.Ingestion;
using SESport.Core.Identifiers;
using SESport.Core.Sources;

var options = ToolOptions.Parse(args);

if (options.ShowHelp)
{
   PrintHelp();
   return 0;
}

var dataPath = Path.GetFullPath(options.DataPath);

if (!File.Exists(dataPath))
{
   Console.Error.WriteLine($"Entity watchlist not found: {dataPath}");
   return 1;
}

var document = await LoadDocumentAsync(dataPath);
var selectedEntities = SelectEntities(document, options).ToList();

if (selectedEntities.Count == 0)
{
   Console.Error.WriteLine("No matching entities were found.");
   return 1;
}

using var httpClient = new HttpClient
{
   Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
};
var modelClient = CreateModelClient(httpClient, options);
var searchService = new ActivitySearchService(modelClient);
var results = new List<ActivitySearchResult>();
var items = new List<ActivitySearchRunItemOutput>();
var runStartedAt = DateTimeOffset.UtcNow;
var runDirectory = options.GetRunDirectory(runStartedAt);
var consecutiveFailures = 0;

if (runDirectory is not null)
{
   Directory.CreateDirectory(Path.Combine(runDirectory, "entities"));
   Directory.CreateDirectory(Path.Combine(runDirectory, "failures"));
   Console.Error.WriteLine(
      $"Writing structured run output to {runDirectory}."
   );
}

for (var entityIndex = 0; entityIndex < selectedEntities.Count; entityIndex++)
{
   var entity = selectedEntities[entityIndex];
   var windowStart = options.SearchDate.AddDays(-options.LookBackDays);
   var windowEnd = options.SearchDate.AddDays(options.LookAheadDays);
   var itemStartedAt = DateTimeOffset.UtcNow;

   Console.Error.WriteLine(
      $"Searching {entity.Name} ({entity.WatchlistId.Value})..."
   );
   Console.Error.WriteLine(
      $"Date window: {windowStart:yyyy-MM-dd} through {windowEnd:yyyy-MM-dd}."
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

      var resultPath = runDirectory is null
         ? null
         : await WriteEntityResultAsync(
            runDirectory,
            entityIndex,
            result,
            options.IncludeRaw
         );
      var item = ActivitySearchRunItemOutput.Completed(
         entity,
         result.Proposals.Count,
         resultPath,
         itemStartedAt,
         DateTimeOffset.UtcNow
      );
      items.Add(item);

      Console.Error.WriteLine(
         $"Found {result.Proposals.Count} proposal(s) for {entity.Name}."
      );
   }
   catch (Exception ex)
   {
      consecutiveFailures++;

      var failurePath = runDirectory is null
         ? null
         : await WriteEntityFailureAsync(
            runDirectory,
            entityIndex,
            entity,
            ex,
            itemStartedAt
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

      if (!options.ContinueOnError)
      {
         throw;
      }

      if (consecutiveFailures >= options.StopAfterFailures)
      {
         Console.Error.WriteLine(
            $"Stopping after {consecutiveFailures} consecutive failure(s)."
         );
         break;
      }
   }

   if (runDirectory is not null)
   {
      await WriteRunManifestAsync(
         runDirectory,
         CreateRunOutput(
            options,
            runDirectory,
            runStartedAt,
            DateTimeOffset.UtcNow,
            results,
            items
         )
      );
   }

   if (
      options.DelaySeconds > 0 &&
      entityIndex + 1 < selectedEntities.Count
   )
   {
      await Task.Delay(TimeSpan.FromSeconds(options.DelaySeconds));
   }
}

var runOutput = CreateRunOutput(
   options,
   runDirectory,
   runStartedAt,
   DateTimeOffset.UtcNow,
   results,
   items
);
var output = JsonSerializer.Serialize(runOutput, JsonOptions.Value);

if (options.OutputPath is not null)
{
   var outputPath = Path.GetFullPath(options.OutputPath);
   await File.WriteAllTextAsync(outputPath, output);
   Console.Error.WriteLine(
      $"Wrote activity search output to {outputPath}."
   );
}
else if (runDirectory is null)
{
   Console.WriteLine(output);
}

if (runDirectory is not null)
{
   await WriteRunManifestAsync(runDirectory, runOutput);
   Console.Error.WriteLine(
      $"Wrote activity search manifest to {runDirectory}\\manifest.json."
   );
}

return 0;

static IActivitySearchModelClient CreateModelClient(
   HttpClient httpClient,
   ToolOptions options
)
{
   if (options.LmStudioPluginId is not null)
   {
      return new LmStudioChatActivitySearchClient(
         httpClient,
         new LmStudioChatActivitySearchClientOptions(
            options.LmStudioBaseAddress,
            options.Model,
            options.LmStudioPluginId,
            options.LmStudioAllowedTools,
            options.ApiKey
         )
      );
   }

   return new OpenAiResponsesActivitySearchClient(
      httpClient,
      new OpenAiResponsesActivitySearchClientOptions(
         options.BaseAddress,
         options.Model,
         options.ApiKey,
         options.WebSearchToolType
      )
   );
}

static async Task<EntityWatchlistDocument> LoadDocumentAsync(
   string dataPath
)
{
   await using var stream = File.OpenRead(dataPath);
   var document =
      await JsonSerializer.DeserializeAsync<EntityWatchlistDocument>(
         stream,
         JsonOptions.Value
      );

   return document ?? throw new InvalidOperationException(
      "Entity watchlist was empty."
   );
}

static IEnumerable<ActivitySearchEntity> SelectEntities(
   EntityWatchlistDocument document,
   ToolOptions options
)
{
   var entities = document.Entities;

   if (!string.IsNullOrWhiteSpace(options.EntityId))
   {
      entities = entities
         .Where(entity => string.Equals(
            entity.Id,
            options.EntityId,
            StringComparison.OrdinalIgnoreCase
         ))
         .ToList();
   }

   return entities
      .Take(options.Take)
      .Select(ToSearchEntity);
}

static ActivitySearchEntity ToSearchEntity(
   EntityWatchlistEntity entity
)
{
   return new ActivitySearchEntity(
      new ExternalEntityId(entity.Id),
      entity.Name,
      entity.Type,
      new ImportedSport(
         new ExternalEntityId(entity.Sport.Id),
         entity.Sport.Name
      ),
      entity.SwedenConnection,
      entity.CurrentRelationshipOrStatus,
      entity.LikelyActivityTypes,
      entity.SuggestedEvidenceSources,
      entity.Notes
   );
}

static void PrintHelp()
{
   Console.WriteLine(
      """
      SESport.AIActivitySearch

      Runs AI activity search for entities in data/entity-watchlist.json.

      Defaults target LM Studio at http://127.0.0.1:1234/v1 with gpt-oss-20b.

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
        --lmstudio-url <url> LM Studio native /api/v1 base URL.
                            Default: http://127.0.0.1:1234/api/v1.
        --model <name>      Model name. Default: gpt-oss-20b, or
                            openai/gpt-oss-20b with --lmstudio-plugin.
        --api-key <key>     API key. Falls back to SESPORT_AI_API_KEY and
                            OPENAI_API_KEY.
        --web-tool <type>   Web search tool type. Default: web_search.
                            For LM Studio, try altra/web-search.
        --lmstudio-plugin <id>
                            Use LM Studio /api/v1/chat integrations with
                            this plugin id, for example altra/web-search.
        --lmstudio-tools <list>
                            Comma-separated plugin tools. Default: search.
        --no-web-search     Do not include the web_search tool.
        --data <path>       Entity watchlist path.
        --output <path>     Write JSON output to a file instead of stdout.
        --run-dir <path>    Write a structured run directory with a manifest
                            and one JSON file per entity.
        --overnight         Safe batch mode. Writes a run directory, searches
                            all entities unless --entity or --take is set,
                            continues after errors, waits between entities,
                            and stops after repeated failures.
        --all               Search all entities when --entity is not set.
        --continue-on-error Continue with the next entity after failures.
        --delay <seconds>   Delay between entities. Default: 0, or 5 with
                            --overnight.
        --stop-after-failures <count>
                            Stop after this many consecutive failures.
                            Default: unlimited, or 5 with --overnight.
        --include-raw       Include raw model content and full raw response.
        --help              Show this help.
      """
   );
}

static ActivitySearchRunOutput CreateRunOutput(
   ToolOptions options,
   string? runDirectory,
   DateTimeOffset startedAt,
   DateTimeOffset completedAt,
   IReadOnlyCollection<ActivitySearchResult> results,
   IReadOnlyCollection<ActivitySearchRunItemOutput> items
)
{
   return new ActivitySearchRunOutput(
      startedAt,
      completedAt,
      runDirectory,
      options.ClientMode,
      options.EffectiveBaseAddress.ToString(),
      options.Model,
      options.AllowWebSearch,
      options.WebSearchToolType,
      options.LmStudioPluginId,
      options.SearchDate,
      options.SearchDate.AddDays(-options.LookBackDays),
      options.SearchDate.AddDays(options.LookAheadDays),
      options.MaxProposals,
      options.ContinueOnError,
      items,
      results.Select(result => ActivitySearchResultOutput.From(
         result,
         options.IncludeRaw
      )).ToList()
   );
}

static async Task WriteRunManifestAsync(
   string runDirectory,
   ActivitySearchRunOutput output
)
{
   await File.WriteAllTextAsync(
      Path.Combine(runDirectory, "manifest.json"),
      JsonSerializer.Serialize(output, JsonOptions.Value)
   );
}

static async Task<string> WriteEntityResultAsync(
   string runDirectory,
   int entityIndex,
   ActivitySearchResult result,
   bool includeRaw
)
{
   var relativePath = Path.Combine(
      "entities",
      CreateEntityFileName(entityIndex, result.Entity)
   );
   var output = ActivitySearchResultOutput.From(result, includeRaw);

   await File.WriteAllTextAsync(
      Path.Combine(runDirectory, relativePath),
      JsonSerializer.Serialize(output, JsonOptions.Value)
   );

   return NormalizePath(relativePath);
}

static async Task<string> WriteEntityFailureAsync(
   string runDirectory,
   int entityIndex,
   ActivitySearchEntity entity,
   Exception exception,
   DateTimeOffset startedAt
)
{
   var relativePath = Path.Combine(
      "failures",
      CreateEntityFileName(entityIndex, entity)
   );
   var output = ActivitySearchFailureOutput.From(
      entity,
      exception,
      startedAt,
      DateTimeOffset.UtcNow
   );

   await File.WriteAllTextAsync(
      Path.Combine(runDirectory, relativePath),
      JsonSerializer.Serialize(output, JsonOptions.Value)
   );

   return NormalizePath(relativePath);
}

static string CreateEntityFileName(
   int entityIndex,
   ActivitySearchEntity entity
)
{
   var entityId = SanitizeFileName(entity.WatchlistId.Value);

   return $"{entityIndex + 1:0000}-{entityId}.json";
}

static string SanitizeFileName(string value)
{
   var invalidCharacters = Path.GetInvalidFileNameChars();
   var characters = value.Select(character =>
      invalidCharacters.Contains(character) ? '-' : character
   );

   return string.Concat(characters);
}

static string NormalizePath(string value)
{
   return value.Replace('\\', '/');
}

internal sealed record ActivitySearchRunOutput(
   DateTimeOffset StartedAt,
   DateTimeOffset CompletedAt,
   string? RunDirectory,
   string ClientMode,
   string BaseAddress,
   string Model,
   bool AllowWebSearch,
   string WebSearchToolType,
   string? LmStudioPluginId,
   DateOnly SearchDate,
   DateOnly WindowStart,
   DateOnly WindowEnd,
   int MaxProposals,
   bool ContinueOnError,
   IReadOnlyCollection<ActivitySearchRunItemOutput> Items,
   IReadOnlyCollection<ActivitySearchResultOutput> Results
);

internal sealed record ActivitySearchRunItemOutput(
   string EntityId,
   string EntityName,
   string Status,
   int? ProposalCount,
   string? ResultPath,
   string? FailurePath,
   string? ErrorType,
   string? ErrorMessage,
   DateTimeOffset StartedAt,
   DateTimeOffset CompletedAt,
   double DurationSeconds
)
{
   public static ActivitySearchRunItemOutput Completed(
      ActivitySearchEntity entity,
      int proposalCount,
      string? resultPath,
      DateTimeOffset startedAt,
      DateTimeOffset completedAt
   )
   {
      return new ActivitySearchRunItemOutput(
         entity.WatchlistId.Value,
         entity.Name,
         "completed",
         proposalCount,
         resultPath,
         null,
         null,
         null,
         startedAt,
         completedAt,
         GetDurationSeconds(startedAt, completedAt)
      );
   }

   public static ActivitySearchRunItemOutput Failed(
      ActivitySearchEntity entity,
      string? failurePath,
      Exception exception,
      DateTimeOffset startedAt,
      DateTimeOffset completedAt
   )
   {
      return new ActivitySearchRunItemOutput(
         entity.WatchlistId.Value,
         entity.Name,
         "failed",
         null,
         null,
         failurePath,
         exception.GetType().Name,
         exception.Message,
         startedAt,
         completedAt,
         GetDurationSeconds(startedAt, completedAt)
      );
   }

   private static double GetDurationSeconds(
      DateTimeOffset startedAt,
      DateTimeOffset completedAt
   )
   {
      return Math.Round((completedAt - startedAt).TotalSeconds, 3);
   }
}

internal sealed record ActivitySearchFailureOutput(
   ActivitySearchEntity Entity,
   string ErrorType,
   string ErrorMessage,
   string? StackTrace,
   DateTimeOffset StartedAt,
   DateTimeOffset CompletedAt,
   double DurationSeconds
)
{
   public static ActivitySearchFailureOutput From(
      ActivitySearchEntity entity,
      Exception exception,
      DateTimeOffset startedAt,
      DateTimeOffset completedAt
   )
   {
      return new ActivitySearchFailureOutput(
         entity,
         exception.GetType().FullName ?? exception.GetType().Name,
         exception.Message,
         exception.ToString(),
         startedAt,
         completedAt,
         Math.Round((completedAt - startedAt).TotalSeconds, 3)
      );
   }
}

internal sealed record ActivitySearchResultOutput(
   ActivitySearchEntity Entity,
   IReadOnlyCollection<ActivityProposalOutput> Proposals,
   string? RawContent,
   string? RawResponse
)
{
   public static ActivitySearchResultOutput From(
      ActivitySearchResult result,
      bool includeRaw
   )
   {
      return new ActivitySearchResultOutput(
         result.Entity,
         result.Proposals.Select(proposal => ActivityProposalOutput.From(
            proposal,
            includeRaw
         )).ToList(),
         includeRaw ? result.RawContent : null,
         includeRaw ? result.RawResponse : null
      );
   }
}

internal sealed record ActivityProposalOutput(
   ActivityProposalId Id,
   ActivityProposalProducerType ProducerType,
   Source Source,
   ExternalEntityId? ExternalId,
   string Fingerprint,
   string Title,
   string? Description,
   string? RawContent,
   ActivityType Type,
   ImportedSport Sport,
   string? Context,
   ActivityTime Time,
   IReadOnlyCollection<ActivityProposalEntityLink> EntityLinks,
   IReadOnlyCollection<ActivityProposalEvidence> Evidence,
   decimal? Confidence,
   ActivityProposalStatus Status,
   ActivityProposalGroupId? GroupId,
   ActivityId? ActivityId
)
{
   public static ActivityProposalOutput From(
      ActivityProposal proposal,
      bool includeRaw
   )
   {
      return new ActivityProposalOutput(
         proposal.Id,
         proposal.ProducerType,
         proposal.Source,
         proposal.ExternalId,
         proposal.Fingerprint,
         proposal.Title,
         proposal.Description,
         includeRaw ? proposal.RawContent : null,
         proposal.Type,
         proposal.Sport,
         proposal.Context,
         proposal.Time,
         proposal.EntityLinks,
         proposal.Evidence,
         proposal.Confidence,
         proposal.Status,
         proposal.GroupId,
         proposal.ActivityId
      );
   }
}

internal sealed record EntityWatchlistDocument(
   int SchemaVersion,
   IReadOnlyCollection<EntityWatchlistEntity> Entities
);

internal sealed record EntityWatchlistEntity(
   string Id,
   string Name,
   string Type,
   EntityWatchlistSport Sport,
   string SwedenConnection,
   string? CurrentRelationshipOrStatus,
   IReadOnlyCollection<string> LikelyActivityTypes,
   string? SuggestedEvidenceSources,
   string? Notes
);

internal sealed record EntityWatchlistSport(
   string Id,
   string Name
);

internal sealed record ToolOptions(
   string DataPath,
   string? EntityId,
   int Take,
   int MaxProposals,
   DateOnly SearchDate,
   int LookBackDays,
   int LookAheadDays,
   Uri BaseAddress,
   Uri LmStudioBaseAddress,
   string Model,
   string? ApiKey,
   bool AllowWebSearch,
   string WebSearchToolType,
   string? LmStudioPluginId,
   IReadOnlyCollection<string> LmStudioAllowedTools,
   int TimeoutSeconds,
   bool Overnight,
   bool ContinueOnError,
   int DelaySeconds,
   int StopAfterFailures,
   bool IncludeRaw,
   string? RunDirectoryPath,
   string? OutputPath,
   bool ShowHelp
)
{
   public string ClientMode => LmStudioPluginId is null
      ? "openai-responses"
      : "lmstudio-chat";

   public Uri EffectiveBaseAddress => LmStudioPluginId is null
      ? BaseAddress
      : LmStudioBaseAddress;

   public string? GetRunDirectory(DateTimeOffset startedAt)
   {
      if (RunDirectoryPath is not null)
      {
         return Path.GetFullPath(RunDirectoryPath);
      }

      if (!Overnight)
      {
         return null;
      }

      return Path.GetFullPath(Path.Combine(
         "data",
         "ai-activity-search-runs",
         startedAt.ToLocalTime().ToString("yyyyMMdd-HHmmss")
      ));
   }

   public static ToolOptions Parse(string[] args)
   {
      var dataPath = "data/entity-watchlist.json";
      string? entityId = null;
      var take = 1;
      var takeWasSet = false;
      var allEntities = false;
      var maxProposals = 5;
      var searchDate = DateOnly.FromDateTime(DateTime.Now);
      var lookBackDays = 0;
      var lookAheadDays = 30;
      int? timeoutSeconds = null;
      int? delaySeconds = null;
      int? stopAfterFailures = null;
      var baseAddress = new Uri("http://127.0.0.1:1234/v1/");
      var lmStudioBaseAddress = new Uri("http://127.0.0.1:1234/api/v1/");
      var model = "gpt-oss-20b";
      var modelWasSet = false;
      var apiKey = Environment.GetEnvironmentVariable("SESPORT_AI_API_KEY") ??
         Environment.GetEnvironmentVariable("OPENAI_API_KEY");
      var allowWebSearch = true;
      var webSearchToolType =
         Environment.GetEnvironmentVariable("SESPORT_AI_WEB_TOOL") ??
         "web_search";
      string? lmStudioPluginId =
         Environment.GetEnvironmentVariable("SESPORT_AI_LMSTUDIO_PLUGIN");
      var lmStudioAllowedTools = ParseCommaSeparated(
         Environment.GetEnvironmentVariable("SESPORT_AI_LMSTUDIO_TOOLS") ??
         "search"
      );
      string? outputPath = null;
      string? runDirectoryPath = null;
      var includeRaw = false;
      var overnight = false;
      var continueOnError = false;
      var showHelp = false;

      for (var index = 0; index < args.Length; index++)
      {
         var arg = args[index];

         switch (arg)
         {
            case "--entity":
               entityId = ReadValue(args, ref index, arg);
               break;
            case "--take":
               take = int.Parse(ReadValue(args, ref index, arg));
               takeWasSet = true;
               break;
            case "--all":
               allEntities = true;
               break;
            case "--max":
               maxProposals = int.Parse(ReadValue(args, ref index, arg));
               break;
            case "--date":
               searchDate = DateOnly.Parse(ReadValue(args, ref index, arg));
               break;
            case "--look-back":
               lookBackDays = int.Parse(ReadValue(args, ref index, arg));
               break;
            case "--look-ahead":
               lookAheadDays = int.Parse(ReadValue(args, ref index, arg));
               break;
            case "--timeout":
               timeoutSeconds = int.Parse(ReadValue(args, ref index, arg));
               break;
            case "--base-url":
               baseAddress = new Uri(ReadValue(args, ref index, arg));
               break;
            case "--lmstudio-url":
               lmStudioBaseAddress = new Uri(ReadValue(args, ref index, arg));
               break;
            case "--model":
               model = ReadValue(args, ref index, arg);
               modelWasSet = true;
               break;
            case "--api-key":
               apiKey = ReadValue(args, ref index, arg);
               break;
            case "--web-tool":
               webSearchToolType = ReadValue(args, ref index, arg);
               break;
            case "--lmstudio-plugin":
               lmStudioPluginId = ReadValue(args, ref index, arg);
               break;
            case "--lmstudio-tools":
               lmStudioAllowedTools = ParseCommaSeparated(
                  ReadValue(args, ref index, arg)
               );
               break;
            case "--no-web-search":
               allowWebSearch = false;
               break;
            case "--data":
               dataPath = ReadValue(args, ref index, arg);
               break;
            case "--output":
               outputPath = ReadValue(args, ref index, arg);
               break;
            case "--run-dir":
               runDirectoryPath = ReadValue(args, ref index, arg);
               break;
            case "--overnight":
               overnight = true;
               break;
            case "--continue-on-error":
               continueOnError = true;
               break;
            case "--delay":
               delaySeconds = int.Parse(ReadValue(args, ref index, arg));
               break;
            case "--stop-after-failures":
               stopAfterFailures = int.Parse(
                  ReadValue(args, ref index, arg)
               );
               break;
            case "--include-raw":
               includeRaw = true;
               break;
            case "--help":
            case "-h":
               showHelp = true;
               break;
            default:
               throw new ArgumentException($"Unknown option: {arg}");
         }
      }

      if ((overnight || allEntities) && entityId is null && !takeWasSet)
      {
         take = int.MaxValue;
      }

      if (overnight)
      {
         continueOnError = true;
      }

      if (lmStudioPluginId is not null && !modelWasSet)
      {
         model = "openai/gpt-oss-20b";
      }

      timeoutSeconds ??= lmStudioPluginId is null ? 100 : 300;
      delaySeconds ??= overnight ? 5 : 0;
      stopAfterFailures ??= overnight ? 5 : int.MaxValue;

      return new ToolOptions(
         dataPath,
         entityId,
         Math.Max(1, take),
         Math.Max(1, maxProposals),
         searchDate,
         Math.Max(0, lookBackDays),
         Math.Max(1, lookAheadDays),
         EnsureTrailingSlash(baseAddress),
         EnsureTrailingSlash(lmStudioBaseAddress),
         model,
         apiKey,
         allowWebSearch,
         webSearchToolType,
         lmStudioPluginId,
         lmStudioAllowedTools,
         Math.Max(1, timeoutSeconds.Value),
         overnight,
         continueOnError,
         Math.Max(0, delaySeconds.Value),
         Math.Max(1, stopAfterFailures.Value),
         includeRaw,
         runDirectoryPath,
         outputPath,
         showHelp
      );
   }

   private static string ReadValue(
      string[] args,
      ref int index,
      string optionName
   )
   {
      if (index + 1 >= args.Length)
      {
         throw new ArgumentException($"{optionName} requires a value.");
      }

      index++;

      return args[index];
   }

   private static Uri EnsureTrailingSlash(Uri uri)
   {
      var value = uri.ToString();

      return value.EndsWith('/') ? uri : new Uri(value + "/");
   }

   private static IReadOnlyCollection<string> ParseCommaSeparated(
      string value
   )
   {
      return value
         .Split(',', StringSplitOptions.RemoveEmptyEntries)
         .Select(item => item.Trim())
         .Where(item => item.Length > 0)
         .ToList();
   }
}

internal static class JsonOptions
{
   public static JsonSerializerOptions Value { get; } = new(
      JsonSerializerDefaults.Web
   )
   {
      WriteIndented = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   };
}
