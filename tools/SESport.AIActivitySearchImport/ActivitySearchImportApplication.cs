using System.Text.Json;

using SESport.Core.AIActivitySearch;
using SESport.Core.Ingestion;
using SESport.Data.Ingestion;

namespace SESport.Tools.AIActivitySearchImport;

internal static class ActivitySearchImportApplication
{
   private static readonly JsonSerializerOptions JsonOptions = new(
      JsonSerializerDefaults.Web
   );

   public static async Task<int> RunAsync(string[] args)
   {
      var options = ImportOptions.Parse(args);

      if(options.ShowHelp)
      {
         PrintHelp();
         return 0;
      }

      var files = ResolveInputFiles(options);

      if(files.Count == 0)
      {
         Console.Error.WriteLine("No AI activity search result JSON files found.");
         return 1;
      }

      var importedFileCount = 0;
      var skippedFileCount = 0;
      var importedProposalCount = 0;

      await using var repository = ActivityProposalRepository.Connect(
         options.ConnectionString
      );

      foreach(var file in files)
      {
         var result = await ReadResultAsync(file);

         if(result is null)
         {
            skippedFileCount++;
            Console.Error.WriteLine($"Skipped {file}: no proposals array found.");
            continue;
         }

         if(result.Proposals.Count == 0)
         {
            skippedFileCount++;
            Console.Error.WriteLine($"Skipped {file}: no proposals to import.");
            continue;
         }

         var savedCount = await repository.SaveAsync(
            result.Proposals,
            CancellationToken.None
         );

         importedFileCount++;
         importedProposalCount += savedCount;

         Console.Error.WriteLine(
            $"Imported {savedCount} proposal(s) from {Path.GetFileName(file)}."
         );
      }

      Console.Error.WriteLine(
         $"Imported {importedProposalCount} proposal(s) from " +
         $"{importedFileCount} file(s). Skipped {skippedFileCount} file(s)."
      );

      return 0;
   }

   private static IReadOnlyList<string> ResolveInputFiles(
      ImportOptions options
   )
   {
      if(options.Files.Count > 0)
      {
         return options.Files
            .Select(ResolvePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
      }

      var dataPath = ResolvePath(options.DataPath);

      if(File.Exists(dataPath))
      {
         return [dataPath];
      }

      if(!Directory.Exists(dataPath))
      {
         Console.Error.WriteLine($"Data path does not exist: {dataPath}");
         return [];
      }

      return Directory
         .EnumerateFiles(dataPath, "*.json", SearchOption.AllDirectories)
         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   private static async Task<ActivitySearchResult?> ReadResultAsync(
      string file
   )
   {
      await using var stream = File.OpenRead(file);
      using var document = await JsonDocument.ParseAsync(stream);

      if(!document.RootElement.TryGetProperty("proposals", out var proposals))
      {
         return null;
      }

      if(proposals.ValueKind != JsonValueKind.Array)
      {
         throw new InvalidOperationException(
            $"Invalid proposals payload in {file}: proposals must be an array."
         );
      }

      var result = document.RootElement.Deserialize<ActivitySearchResult>(
         JsonOptions
      );

      if(result?.Proposals is null)
      {
         throw new InvalidOperationException(
            $"Invalid activity search result JSON in {file}."
         );
      }

      return result;
   }

   private static string ResolvePath(string path)
   {
      if(Path.IsPathRooted(path))
      {
         return Path.GetFullPath(path);
      }

      return Path.GetFullPath(Path.Combine(FindRepositoryRoot(), path));
   }

   private static string FindRepositoryRoot()
   {
      var directory = new DirectoryInfo(AppContext.BaseDirectory);

      while(directory is not null)
      {
         if(File.Exists(Path.Combine(directory.FullName, "SESport.sln")))
         {
            return directory.FullName;
         }

         directory = directory.Parent;
      }

      return Directory.GetCurrentDirectory();
   }

   private static void PrintHelp()
   {
      Console.WriteLine(
         """
         SESport.AIActivitySearchImport

         Imports AI activity search result JSON files into PostgreSQL as
         activity proposals.

         Options:
           --data <path>       Directory or JSON file to import.
                               Default: data/ai-activity-search-results.
           --file <path>       Import one JSON file. Can be provided multiple
                               times. Positional JSON file paths also work.
           --connection-string <value>
                               PostgreSQL connection string.
           --help              Show this help.
         """
      );
   }
}
