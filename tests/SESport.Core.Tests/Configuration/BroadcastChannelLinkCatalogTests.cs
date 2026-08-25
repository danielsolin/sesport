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
      "Allente",
      "https://www.allente.se/tv-guide/",
      "Allente"
   )]
   [InlineData(
      "Apple TV",
      "https://tv.apple.com/se",
      "Apple TV"
   )]
   [InlineData(
      "DAZN Sverige",
      "https://www.dazn.com/en-SE/home",
      "DAZN Sverige"
   )]
   [InlineData(
      "DBET",
      "https://www.dbet.com/sv/",
      "DBET"
   )]
   [InlineData(
      "Expressen",
      "https://livesport.expressen.se/sv/",
      "Expressen"
   )]
   [InlineData(
      "HBO Max",
      "https://www.max.com/se/sv/sports",
      "HBO Max"
   )]
   [InlineData(
      "Kanal 9",
      "https://www.allente.se/tv-guide/",
      "Kanal 9"
   )]
   [InlineData(
      "Prime Video",
      "https://www.primevideo.com/-/sv/sports",
      "Prime Video"
   )]
   [InlineData(
      "Sportbladet Plus",
      "https://www.aftonbladet.se/sportbladet",
      "Sportbladet Plus"
   )]
   [InlineData(
      "TV4 Fotboll",
      "https://www.tv4play.se/kanaler",
      "TV4 Fotboll"
   )]
   [InlineData(
      "TV4 Hockey",
      "https://www.tv4play.se/kanaler",
      "TV4 Hockey"
   )]
   [InlineData(
      "TV4 Motor",
      "https://www.tv4play.se/kanaler",
      "TV4 Motor"
   )]
   [InlineData(
      "TV4 Play",
      "https://www.tv4play.se/kanaler",
      "TV4 Play"
   )]
   [InlineData(
      "TV4 Play Sport",
      "https://www.tv4play.se/sport",
      "TV4 Play Sport"
   )]
   [InlineData(
      "TV4 Sportkanalen",
      "https://www.tv4play.se/kanaler",
      "TV4 Sportkanalen"
   )]
   [InlineData(
      "TV4 Sport Live 1",
      "https://www.tv4play.se/kanaler",
      "TV4 Sport Live 1"
   )]
   [InlineData(
      "TV4 Sport Live 2",
      "https://www.tv4play.se/kanaler",
      "TV4 Sport Live 2"
   )]
   [InlineData(
      "TV4 Tennis",
      "https://www.tv4play.se/kanaler",
      "TV4 Tennis"
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
      "V Sport Football",
      "https://viaplay.se/se-sv/",
      "V Sport Football"
   )]
   [InlineData(
      "V Sport Motor",
      "https://viaplay.se/se-sv/",
      "V Sport Motor"
   )]
   [InlineData(
      "V Sport Ultra HD",
      "https://viaplay.se/se-sv/",
      "V Sport Ultra HD"
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

   [Theory]
   [InlineData("V Sport Footbal")]
   [InlineData("Viaplayl")]
   public void FindDoesNotAcceptMisspelledChannelNames(string channelName)
   {
      Assert.Null(BroadcastChannelLinkCatalog.Find(channelName));
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
