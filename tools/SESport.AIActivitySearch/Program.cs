using System.Text.Json;
using System.Text.Json.Serialization;
using SESport.Core.AIActivitySearch;
using SESport.Core.Ingestion;
using SESport.Core.Identifiers;

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

using var httpClient = new HttpClient();
var modelClient = new OpenAiResponsesActivitySearchClient(
   httpClient,
   new OpenAiResponsesActivitySearchClientOptions(
      options.BaseAddress,
      options.Model,
      options.ApiKey,
      options.WebSearchToolType
   )
);
var searchService = new ActivitySearchService(modelClient);
var results = new List<ActivitySearchResult>();

foreach (var entity in selectedEntities)
{
   Console.Error.WriteLine(
      $"Searching {entity.Name} ({entity.WatchlistId.Value})..."
   );

   var result = await searchService.SearchAsync(
      new ActivitySearchRequest(
         entity,
         options.SearchDate,
         options.MaxProposals,
         options.AllowWebSearch
      ),
      CancellationToken.None
   );

   results.Add(result);

   Console.Error.WriteLine(
      $"Found {result.Proposals.Count} proposal(s) for {entity.Name}."
   );
}

var output = JsonSerializer.Serialize(
   new ActivitySearchRunOutput(
      options.BaseAddress.ToString(),
      options.Model,
      options.AllowWebSearch,
      options.WebSearchToolType,
      options.SearchDate,
      results
   ),
   JsonOptions.Value
);

if (options.OutputPath is null)
{
   Console.WriteLine(output);
}
else
{
   var outputPath = Path.GetFullPath(options.OutputPath);
   await File.WriteAllTextAsync(outputPath, output);
   Console.Error.WriteLine(
      $"Wrote activity search output to {outputPath}."
   );
}

return 0;

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
        --base-url <url>    OpenAI-compatible /v1 base URL.
        --model <name>      Model name. Default: gpt-oss-20b.
        --api-key <key>     API key. Falls back to SESPORT_AI_API_KEY and
                            OPENAI_API_KEY.
        --web-tool <type>   Web search tool type. Default: web_search.
                            For LM Studio, try altra/web-search.
        --no-web-search     Do not include the web_search tool.
        --data <path>       Entity watchlist path.
        --output <path>     Write JSON output to a file instead of stdout.
        --help              Show this help.
      """
   );
}

internal sealed record ActivitySearchRunOutput(
   string BaseAddress,
   string Model,
   bool AllowWebSearch,
   string WebSearchToolType,
   DateOnly SearchDate,
   IReadOnlyCollection<ActivitySearchResult> Results
);

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
   Uri BaseAddress,
   string Model,
   string? ApiKey,
   bool AllowWebSearch,
   string WebSearchToolType,
   string? OutputPath,
   bool ShowHelp
)
{
   public static ToolOptions Parse(string[] args)
   {
      var dataPath = "data/entity-watchlist.json";
      string? entityId = null;
      var take = 1;
      var maxProposals = 5;
      var searchDate = DateOnly.FromDateTime(DateTime.Now);
      var baseAddress = new Uri("http://127.0.0.1:1234/v1/");
      var model = "gpt-oss-20b";
      var apiKey = Environment.GetEnvironmentVariable("SESPORT_AI_API_KEY") ??
         Environment.GetEnvironmentVariable("OPENAI_API_KEY");
      var allowWebSearch = true;
      var webSearchToolType =
         Environment.GetEnvironmentVariable("SESPORT_AI_WEB_TOOL") ??
         "web_search";
      string? outputPath = null;
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
               break;
            case "--max":
               maxProposals = int.Parse(ReadValue(args, ref index, arg));
               break;
            case "--date":
               searchDate = DateOnly.Parse(ReadValue(args, ref index, arg));
               break;
            case "--base-url":
               baseAddress = new Uri(ReadValue(args, ref index, arg));
               break;
            case "--model":
               model = ReadValue(args, ref index, arg);
               break;
            case "--api-key":
               apiKey = ReadValue(args, ref index, arg);
               break;
            case "--web-tool":
               webSearchToolType = ReadValue(args, ref index, arg);
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
            case "--help":
            case "-h":
               showHelp = true;
               break;
            default:
               throw new ArgumentException($"Unknown option: {arg}");
         }
      }

      return new ToolOptions(
         dataPath,
         entityId,
         Math.Max(1, take),
         Math.Max(1, maxProposals),
         searchDate,
         EnsureTrailingSlash(baseAddress),
         model,
         apiKey,
         allowWebSearch,
         webSearchToolType,
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
