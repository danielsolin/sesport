using System.Text.Json;

using Npgsql;

using SESport.Core.AIActivitySearch;
using SESport.Core.Identifiers;
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

      await using var dataSource = NpgsqlDataSource.Create(
         options.ConnectionString
      );
      await using var repository = new ActivityProposalRepository(dataSource);

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

         var proposals = RemapPrimaryEntityLinks(result);
         await EnsureLinkedEntitiesExistAsync(
            dataSource,
            file,
            proposals,
            CancellationToken.None
         );

         var savedCount = await repository.SaveAsync(
            proposals,
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

   private static IReadOnlyCollection<ActivityProposal> RemapPrimaryEntityLinks(
      ActivitySearchResult result
   )
   {
      var currentEntityId = ToEntityId(result.Entity.WatchlistId.Value);

      return result.Proposals.Select(proposal => proposal with
      {
         EntityLinks = proposal.EntityLinks
            .Select(link => ShouldRemapLink(result, link)
               ? link with { EntityId = currentEntityId }
               : link)
            .ToList()
      }).ToList();
   }

   private static bool ShouldRemapLink(
      ActivitySearchResult result,
      ActivityProposalEntityLink link
   )
   {
      return string.IsNullOrWhiteSpace(link.ContextName) ||
         link.ContextName.Equals(
            result.Entity.Name,
            StringComparison.OrdinalIgnoreCase
         );
   }

   private static async Task EnsureLinkedEntitiesExistAsync(
      NpgsqlDataSource dataSource,
      string file,
      IReadOnlyCollection<ActivityProposal> proposals,
      CancellationToken cancellationToken
   )
   {
      var entityIds = proposals
         .SelectMany(proposal => proposal.EntityLinks)
         .Select(link => link.EntityId.Value)
         .Distinct()
         .ToList();

      if(entityIds.Count == 0)
      {
         return;
      }

      const string sql = """
         select id
         from tracked_entities
         where id = any(@ids)
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("ids", entityIds);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      var foundIds = new HashSet<Guid>();

      while(await reader.ReadAsync(cancellationToken))
      {
         foundIds.Add(reader.GetGuid(0));
      }

      var missingIds = entityIds
         .Where(id => !foundIds.Contains(id))
         .ToList();

      if(missingIds.Count == 0)
      {
         return;
      }

      throw new InvalidOperationException(
         $"Cannot import {file} because {missingIds.Count} linked " +
         "tracked entity/entities are missing from PostgreSQL. Run " +
         "`dotnet run --project tools/SESport.ImportEntities` first. " +
         $"Missing entity IDs: {string.Join(", ", missingIds)}"
      );
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

   private static EntityId ToEntityId(string stableKey)
   {
      return new EntityId(DeterministicGuid.Create($"entity:{stableKey}"));
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
