namespace SESport.Tools.AIActivitySearch;

internal static class ToolPathResolver
{
   public static string ResolveDataPath(string dataPath)
   {
      if(Path.IsPathRooted(dataPath))
      {
         return Path.GetFullPath(dataPath);
      }

      var currentDirectoryPath = Path.GetFullPath(dataPath);
      if(File.Exists(currentDirectoryPath))
      {
         return currentDirectoryPath;
      }

      var repoRelativePath = FindRepositoryRelativePath(dataPath);
      return repoRelativePath ?? currentDirectoryPath;
   }

   public static string ResolveRepositoryRelativePath(string relativePath)
   {
      var repoRoot = FindRepositoryRoot();

      return Path.GetFullPath(Path.Combine(
         repoRoot ?? Environment.CurrentDirectory,
         relativePath
      ));
   }

   private static string? FindRepositoryRelativePath(string relativePath)
   {
      foreach(var startDirectory in GetSearchStartDirectories())
      {
         var candidate = FindInParents(startDirectory, relativePath);
         if(candidate is not null)
         {
            return candidate;
         }
      }

      return null;
   }

   private static string? FindRepositoryRoot()
   {
      foreach(var startDirectory in GetSearchStartDirectories())
      {
         var directory = new DirectoryInfo(startDirectory);

         while(directory is not null)
         {
            if(File.Exists(Path.Combine(
               directory.FullName,
               "data",
               "entity-watchlist.json"
            )))
            {
               return directory.FullName;
            }

            directory = directory.Parent;
         }
      }

      return null;
   }

   private static IEnumerable<string> GetSearchStartDirectories()
   {
      yield return AppContext.BaseDirectory;
      yield return Environment.CurrentDirectory;
   }

   private static string? FindInParents(
      string startDirectory,
      string relativePath
   )
   {
      var directory = new DirectoryInfo(startDirectory);

      while(directory is not null)
      {
         var candidate = Path.GetFullPath(Path.Combine(
            directory.FullName,
            relativePath
         ));

         if(File.Exists(candidate))
         {
            return candidate;
         }

         directory = directory.Parent;
      }

      return null;
   }
}
