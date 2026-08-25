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

   [Theory]
   [InlineData(
      "Viaplay",
      "https://viaplay.se/se-sv/",
      "Viaplay"
   )]
   [InlineData(
      "V Sport Premium",
      "https://viaplay.se/se-sv/",
      "V Sport Premium"
   )]
   [InlineData(
      "V Sport Golf",
      "https://viaplay.se/se-sv/",
      "V Sport Golf"
   )]
   [InlineData(
      "Disney+",
      "https://www.disneyplus.com/sv-se",
      "Disney+"
   )]
   [InlineData(
      "Eurosport 1",
      "https://www.max.com/se/sv/sports",
      "Eurosport 1"
   )]
   [InlineData(
      "Eurosport 2",
      "https://www.max.com/se/sv/sports",
      "Eurosport 2"
   )]
   public void FindMatchesGenericChannel(
      string channelName,
      string expectedUrl,
      string expectedCanonicalName
   )
   {
      var definition = BroadcastChannelLinkCatalog.Find(channelName);

      Assert.NotNull(definition);
      Assert.Equal(expectedUrl, definition.Url);
      Assert.Equal(expectedCanonicalName, definition.CanonicalName);
   }

   [Fact]
   public void FindReturnsNullForUnmappedChannel()
   {
      Assert.Null(BroadcastChannelLinkCatalog.Find("Unknown Channel"));
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
