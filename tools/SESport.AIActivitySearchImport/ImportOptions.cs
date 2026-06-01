namespace SESport.Tools.AIActivitySearchImport;

internal sealed record ImportOptions(
   string DataPath,
   IReadOnlyCollection<string> Files,
   string ConnectionString,
   bool ShowHelp
)
{
   public static ImportOptions Parse(string[] args)
   {
      var dataPath = "data/ai-activity-search-results";
      var files = new List<string>();
      var connectionString = DefaultConnectionString();
      var showHelp = false;

      for(var index = 0; index < args.Length; index++)
      {
         var arg = args[index];

         switch(arg)
         {
            case "--data":
               dataPath = ReadValue(args, ref index, arg);
               break;
            case "--file":
               files.Add(ReadValue(args, ref index, arg));
               break;
            case "--connection-string":
               connectionString = ReadValue(args, ref index, arg);
               break;
            case "--help":
            case "-h":
               showHelp = true;
               break;
            default:
               if(arg.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
               {
                  files.Add(arg);
                  break;
               }

               throw new ArgumentException($"Unknown option: {arg}");
         }
      }

      return new ImportOptions(
         dataPath,
         files,
         connectionString,
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

   private static string DefaultConnectionString()
   {
      return Environment.GetEnvironmentVariable("ConnectionStrings__SESport") ??
         Environment.GetEnvironmentVariable("SESPORT_CONNECTION_STRING") ??
         "Host=localhost;Port=5432;Database=sesport;Username=sesport;" +
         "Password=sesport";
   }
}
