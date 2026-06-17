using System.Runtime.CompilerServices;

namespace SESport.Core.Tests;

public static class TestEnvironmentBootstrap
{
   [ModuleInitializer]
   public static void Initialize()
   {
      var repositoryRoot = FindRepositoryRoot();
      var envPath = Path.Combine(repositoryRoot, ".env");

      if(!File.Exists(envPath))
      {
         return;
      }

      foreach(var line in File.ReadAllLines(envPath))
      {
         if(string.IsNullOrWhiteSpace(line))
         {
            continue;
         }

         var trimmedLine = line.Trim();
         if(trimmedLine.StartsWith('#'))
         {
            continue;
         }

         var equalsIndex = trimmedLine.IndexOf('=');
         if(equalsIndex <= 0)
         {
            continue;
         }

         var key = trimmedLine[..equalsIndex].Trim();
         var value = trimmedLine[(equalsIndex + 1)..].Trim();

         if(string.IsNullOrWhiteSpace(key))
         {
            continue;
         }

         if(Environment.GetEnvironmentVariable(key) is null)
         {
            Environment.SetEnvironmentVariable(key, value);
         }
      }
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

      throw new InvalidOperationException("Could not find repository root.");
   }
}
