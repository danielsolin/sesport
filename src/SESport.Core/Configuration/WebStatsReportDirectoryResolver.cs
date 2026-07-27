namespace SESport.Core.Configuration;

public static class WebStatsReportDirectoryResolver
{
   private static readonly string[] ReportPathParts =
   [
      "data",
      "web-stats"
   ];

   public static string Resolve(
      string configuredDirectory,
      string contentRootPath,
      string applicationDirectory
   )
   {
      if(!string.IsNullOrWhiteSpace(configuredDirectory))
      {
         return Path.GetFullPath(
            configuredDirectory,
            contentRootPath
         );
      }

      var repositoryDirectory = FindInParentDirectories(contentRootPath);
      if(repositoryDirectory is not null)
      {
         return repositoryDirectory;
      }

      return Path.Combine(
         [applicationDirectory, .. ReportPathParts]
      );
   }

   private static string? FindInParentDirectories(string startPath)
   {
      var directory = new DirectoryInfo(startPath);

      while(directory is not null)
      {
         var candidate = Path.Combine(
            [directory.FullName, .. ReportPathParts]
         );

         if(Directory.Exists(candidate))
         {
            return candidate;
         }

         directory = directory.Parent;
      }

      return null;
   }
}
