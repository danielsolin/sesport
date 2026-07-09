using System.Text.Json;

namespace SESport.Core.Tests.Data;

public class EntityWatchlistDataTests
{
   [Fact]
   public void EntityWatchlistDataContainsNormalizedSeedEntities()
   {
      using var document = JsonDocument.Parse(
         File.ReadAllText(
            Path.Combine(
               FindRepositoryRoot(),
               "data",
               "deepresearch-watchlist",
               "entity-watchlist.json"
            )
         )
      );

      var root = document.RootElement;
      var entities = root.GetProperty("entities");

      Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
      Assert.True(entities.GetArrayLength() > 100);
      Assert.Contains(
         entities.EnumerateArray(),
         entity =>
            entity.GetProperty("id").GetString() == "tre-kronor" &&
            entity.GetProperty("type").GetString() == "national_team" &&
            entity.GetProperty("sport").GetProperty("id").GetString() ==
               "ice-hockey"
      );
      Assert.DoesNotContain(
         entities.EnumerateArray(),
         entity =>
            entity.GetProperty("sport").GetProperty("id").GetString() is
               "motor-racing" or "motorsport-rally" or "rally" or
               "volleyball-beach-volleyball"
      );
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
