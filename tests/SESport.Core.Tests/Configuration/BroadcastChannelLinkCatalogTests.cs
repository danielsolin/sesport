using SESport.Core.Configuration;

namespace SESport.Core.Tests.Configuration;

public sealed class BroadcastChannelLinkCatalogTests
{
   [Fact]
   public void FindMatchesCanonicalChannelName()
   {
      var definition = CreateCatalog().Find("SVT1");

      Assert.NotNull(definition);
      Assert.Equal("svt1-link", definition.Url);
   }

   [Fact]
   public void FindMatchesAliasAndCountryPrefix()
   {
      var definition = CreateCatalog().Find("SE - SVT 2");

      Assert.NotNull(definition);
      Assert.Equal("SVT2", definition.CanonicalName);
   }

   [Theory]
   [InlineData("SVT1")]
   [InlineData("SVT2")]
   [InlineData("SVT24")]
   [InlineData("SVT Barn")]
   [InlineData("Kunskapskanalen")]
   [InlineData("SVT Play")]
   [InlineData("TV4")]
   [InlineData("TV12")]
   [InlineData("TV4 Fotboll")]
   [InlineData("TV4 Hockey")]
   [InlineData("TV4 Motor")]
   [InlineData("TV4 Play")]
   [InlineData("TV4 Play Sport")]
   [InlineData("TV4 Sportkanalen")]
   [InlineData("TV4 Sport Live 1")]
   [InlineData("TV4 Sport Live 2")]
   [InlineData("TV4 Tennis")]
   [InlineData("Allente")]
   [InlineData("Apple TV")]
   [InlineData("DAZN Sverige")]
   [InlineData("DBET")]
   [InlineData("Expressen")]
   [InlineData("HBO Max")]
   [InlineData("Kanal 9")]
   [InlineData("Prime Video")]
   [InlineData("Sportbladet Plus")]
   [InlineData("Viaplay")]
   [InlineData("V Sport 1")]
   [InlineData("V Sport Premium")]
   [InlineData("V Sport Golf")]
   [InlineData("V Sport Extra")]
   [InlineData("V Sport Football")]
   [InlineData("Disney+")]
   [InlineData("Eurosport 1")]
   [InlineData("Eurosport 2")]
   [InlineData("Viaplay Sport")]
   [InlineData("V Sport Motor")]
   [InlineData("V Sport Ultra HD")]
   public void FindMatchesLoadedChannelDefinition(string channelName)
   {
      var definition = CreateCatalog().Find(channelName);

      Assert.NotNull(definition);
   }

   [Fact]
   public void FindReturnsNullForUnmappedChannel()
   {
      Assert.Null(CreateCatalog().Find("Unknown Channel"));
   }

   [Theory]
   [InlineData("V Sport Footbal")]
   [InlineData("Viaplayl")]
   public void FindDoesNotAcceptMisspelledChannelNames(string channelName)
   {
      Assert.Null(CreateCatalog().Find(channelName));
   }

   [Fact]
   public void DefinitionsHaveUniqueCanonicalNamesAndAliases()
   {
      var names = CreateCatalog().Definitions
         .SelectMany(definition => new[] { definition.CanonicalName }
            .Concat(definition.Aliases))
         .Select(name => name.Trim())
         .ToArray();

      Assert.Equal(
         names.Length,
         names.Distinct(StringComparer.OrdinalIgnoreCase).Count()
      );
   }

   private static BroadcastChannelLinkCatalog CreateCatalog()
   {
      return new BroadcastChannelLinkCatalog(
         [
            new("SVT1", "svt1-link", ["SVT 1"]),
            new("SVT2", "svt2-link", ["SVT 2"]),
            new("SVT24", "svt24-link", ["SVT 24"]),
            new("SVT Barn", "svt-barn-link", []),
            new("Kunskapskanalen", "kunskapskanalen-link", []),
            new("SVT Play", "svt-play-link", []),
            new("TV4", "tv4-link", []),
            new("TV12", "tv12-link", []),
            new("TV4 Fotboll", "tv4-fotboll-link", []),
            new("TV4 Hockey", "tv4-hockey-link", []),
            new("TV4 Motor", "tv4-motor-link", []),
            new("TV4 Play", "tv4-play-link", []),
            new("TV4 Play Sport", "tv4-play-sport-link", []),
            new("TV4 Sportkanalen", "tv4-sportkanalen-link", []),
            new("TV4 Sport Live 1", "tv4-sport-live-1-link", []),
            new("TV4 Sport Live 2", "tv4-sport-live-2-link", []),
            new("TV4 Tennis", "tv4-tennis-link", []),
            new("Allente", "allente-link", []),
            new("Apple TV", "apple-tv-link", []),
            new("DAZN Sverige", "dazn-link", []),
            new("DBET", "dbet-link", []),
            new("Expressen", "expressen-link", []),
            new("HBO Max", "hbo-max-link", []),
            new("Kanal 9", "kanal-9-link", []),
            new("Prime Video", "prime-video-link", []),
            new("Sportbladet Plus", "sportbladet-plus-link", []),
            new("Viaplay", "viaplay-link", []),
            new("V Sport 1", "v-sport-1-link", ["V Sport1"]),
            new("V Sport Premium", "v-sport-premium-link", []),
            new("V Sport Golf", "v-sport-golf-link", []),
            new("V Sport Extra", "v-sport-extra-link", []),
            new("V Sport Football", "v-sport-football-link", []),
            new("Disney+", "disney-plus-link", ["Disney +"]),
            new(
               "Eurosport 1",
               "eurosport-1-link",
               ["Eurosport 1 HD"]
            ),
            new(
               "Eurosport 2",
               "eurosport-2-link",
               ["Eurosport 2 HD"]
            ),
            new("Viaplay Sport", "viaplay-sport-link", []),
            new("V Sport Motor", "v-sport-motor-link", []),
            new("V Sport Ultra HD", "v-sport-ultra-hd-link", [])
         ]
      );
   }
}
