namespace SESport.Tools.AIActivitySearch;

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
   string? ApiKeySource,
   bool AllowWebSearch,
   string WebSearchToolType,
   string? LmStudioPluginId,
   IReadOnlyCollection<string> LmStudioAllowedTools,
   int TimeoutSeconds,
   bool Overnight,
   bool ContinueOnError,
   int DelaySeconds,
   int StopAfterFailures,
   bool WriteToDatabase,
   string ConnectionString,
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

   public string ApiKeyDescription => ApiKey is null
      ? "not configured"
      : $"{ApiKeySource ?? "explicit"} ({MaskSecret(ApiKey)})";

   public string? GetRunDirectory(DateTimeOffset startedAt)
   {
      if(RunDirectoryPath is not null)
      {
         return Path.GetFullPath(RunDirectoryPath);
      }

      return ToolPathResolver.ResolveRepositoryRelativePath(Path.Combine(
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
      var baseAddress = new Uri("https://openrouter.ai/api/v1/");
      var lmStudioBaseAddress = new Uri("http://127.0.0.1:1234/api/v1/");
      var model = "openrouter/free";
      var modelWasSet = false;
      string? explicitApiKey = null;
      var allowWebSearch = true;
      var writeToDatabase = false;
      var connectionString = DefaultConnectionString();
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

      for(var index = 0; index < args.Length; index++)
      {
         var arg = args[index];

         switch(arg)
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
               explicitApiKey = ReadValue(args, ref index, arg);
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
            case "--write-to-db":
               writeToDatabase = true;
               break;
            case "--connection-string":
               connectionString = ReadValue(args, ref index, arg);
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

      if((overnight || allEntities) && entityId is null && !takeWasSet)
      {
         take = int.MaxValue;
      }

      if(overnight)
      {
         continueOnError = true;
      }

      timeoutSeconds ??= lmStudioPluginId is null ? 100 : 300;
      delaySeconds ??= overnight ? 5 : 0;
      stopAfterFailures ??= overnight ? 5 : int.MaxValue;

      var resolvedApiKey = ResolveApiKey(
         explicitApiKey,
         lmStudioPluginId is not null,
         baseAddress
      );

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
         resolvedApiKey.Value,
         resolvedApiKey.Source,
         allowWebSearch,
         webSearchToolType,
         lmStudioPluginId,
         lmStudioAllowedTools,
         Math.Max(1, timeoutSeconds.Value),
         overnight,
         continueOnError,
         Math.Max(0, delaySeconds.Value),
         Math.Max(1, stopAfterFailures.Value),
         writeToDatabase,
         connectionString,
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
      if(index + 1 >= args.Length)
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

   private static IReadOnlyCollection<string> ParseCommaSeparated(string value)
   {
      return value
         .Split(',', StringSplitOptions.RemoveEmptyEntries)
         .Select(item => item.Trim())
         .Where(item => item.Length > 0)
         .ToList();
   }

   private static string DefaultConnectionString()
   {
      return Environment.GetEnvironmentVariable("ConnectionStrings__SESport") ??
         Environment.GetEnvironmentVariable("SESPORT_CONNECTION_STRING") ??
         "Host=localhost;Port=5432;Database=sesport;Username=sesport;" +
         "Password=sesport";
   }

   private static (string? Value, string? Source) ResolveApiKey(
      string? explicitApiKey,
      bool useLmStudio,
      Uri baseAddress
   )
   {
      if(!string.IsNullOrWhiteSpace(explicitApiKey))
      {
         return (explicitApiKey.Trim(), "--api-key");
      }

      if(useLmStudio)
      {
         return ReadEnvironmentKey("LMSTUDIO_API_KEY");
      }

      if(IsOpenRouterBaseAddress(baseAddress))
      {
         return ReadEnvironmentKey("OPENROUTER_API_KEY");
      }

      return ReadEnvironmentKey("OPENAI_API_KEY");
   }

   private static (string? Value, string? Source) ReadEnvironmentKey(
      string name
   )
   {
      var value = Environment.GetEnvironmentVariable(name);

      return string.IsNullOrWhiteSpace(value)
         ? (null, null)
         : (value.Trim(), name);
   }

   private static bool IsOpenRouterBaseAddress(Uri baseAddress)
   {
      return baseAddress.Host.Equals(
         "openrouter.ai",
         StringComparison.OrdinalIgnoreCase
      );
   }

   private static string MaskSecret(string secret)
   {
      if(secret.Length <= 12)
      {
         return "***";
      }

      return $"{secret[..8]}...{secret[^4..]}";
   }
}
