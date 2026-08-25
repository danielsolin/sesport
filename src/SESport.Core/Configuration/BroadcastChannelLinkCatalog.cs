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
