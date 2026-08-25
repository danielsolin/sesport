namespace SESport.Core.Configuration;

public sealed record BroadcastChannelLinkDefinition(
   string CanonicalName,
   string Url,
   IReadOnlyList<string> Aliases
)
{
   public bool Matches(string channelName)
   {
      return MatchesName(CanonicalName, channelName) ||
         Aliases.Any(alias => MatchesName(alias, channelName));
   }

   private static bool MatchesName(
      string expectedName,
      string actualName
   )
   {
      return string.Equals(
         PrimaryCountry.NormalizeBroadcastChannelName(expectedName)
            .Trim(),
         PrimaryCountry.NormalizeBroadcastChannelName(actualName).Trim(),
         StringComparison.OrdinalIgnoreCase
      );
   }
}

public static class BroadcastChannelLinkCatalog
{
   public static IReadOnlyList<BroadcastChannelLinkDefinition> Definitions
   {
      get;
   } =
   [
      new(
         "SVT1",
         "https://www.svtplay.se/kanaler/svt1?start=auto",
         ["SVT 1"]
      ),
      new(
         "SVT2",
         "https://www.svtplay.se/kanaler/svt2?start=auto",
         ["SVT 2"]
      ),
      new(
         "SVT24",
         "https://www.svtplay.se/kanaler/svt24?start=auto",
         ["SVT 24"]
      ),
      new(
         "SVT Barn",
         "https://www.svtplay.se/kanaler/svtbarn?start=auto",
         []
      ),
      new(
         "Kunskapskanalen",
         "https://www.svtplay.se/kanaler/kunskapskanalen?start=auto",
         []
      ),
      new(
         "SVT Play",
         "https://www.svtplay.se/kanaler?start=auto",
         []
      ),
      new(
         "TV4",
         "https://www.tv4play.se/kanaler/tv4",
         []
      ),
      new(
         "TV12",
         "https://www.tv4play.se/kanaler/tv12",
         []
      ),
      new(
         "Viaplay",
         "https://viaplay.se/se-sv/",
         []
      ),
      new(
         "V Sport 1",
         "https://viaplay.se/se-sv/",
         ["V Sport1"]
      ),
      new(
         "V Sport Premium",
         "https://viaplay.se/se-sv/",
         []
      ),
      new(
         "V Sport Golf",
         "https://viaplay.se/se-sv/",
         []
      ),
      new(
         "V Sport Extra",
         "https://viaplay.se/se-sv/",
         []
      ),
      new(
         "Disney+",
         "https://www.disneyplus.com/sv-se",
         ["Disney +"]
      ),
      new(
         "Eurosport 1",
         "https://www.max.com/se/sv/sports",
         ["Eurosport 1 HD"]
      ),
      new(
         "Eurosport 2",
         "https://www.max.com/se/sv/sports",
         ["Eurosport 2 HD"]
      ),
      new(
         "Viaplay Sport",
         "https://viaplay.se/se-sv/viaplay-sport-tv",
         []
      )
   ];

   public static BroadcastChannelLinkDefinition? Find(
      string? channelName
   )
   {
      return string.IsNullOrWhiteSpace(channelName)
         ? null
         : Definitions.FirstOrDefault(
            definition => definition.Matches(channelName)
         );
   }
}
