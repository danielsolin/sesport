using SESport.Core.Configuration;

namespace SESport.Core.Tests.Configuration;

public sealed class BroadcastChannelLinkCatalogTests
{
   [Fact]
   public void FindMatchesCanonicalChannelName()
   {
      var definition = BroadcastChannelLinkCatalog.Find("SVT1");

      Assert.NotNull(definition);
      Assert.Equal(
         "https://www.svtplay.se/kanaler/svt1?start=auto",
         definition.Url
      );
   }

   [Fact]
   public void FindMatchesAliasAndCountryPrefix()
   {
      var definition = BroadcastChannelLinkCatalog.Find("SE - SVT 2");

      Assert.NotNull(definition);
      Assert.Equal("SVT2", definition.CanonicalName);
   }

   [Fact]
   public void FindReturnsNullForUnmappedChannel()
   {
      Assert.Null(BroadcastChannelLinkCatalog.Find("Eurosport 1"));
   }

   [Fact]
   public void DefinitionsHaveUniqueCanonicalNamesAndAliases()
   {
      var names = BroadcastChannelLinkCatalog.Definitions
         .SelectMany(definition => new[] { definition.CanonicalName }
            .Concat(definition.Aliases))
         .Select(name => name.Trim())
         .ToArray();

      Assert.Equal(
         names.Length,
         names.Distinct(StringComparer.OrdinalIgnoreCase).Count()
      );
   }
}
