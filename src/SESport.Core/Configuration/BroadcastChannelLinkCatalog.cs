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

public sealed class BroadcastChannelLinkCatalog
{
   public BroadcastChannelLinkCatalog(
      IEnumerable<BroadcastChannelLinkDefinition> definitions
   )
   {
      Definitions = Array.AsReadOnly(definitions.ToArray());
   }

   public IReadOnlyList<BroadcastChannelLinkDefinition> Definitions
   {
      get;
   }

   public BroadcastChannelLinkDefinition? Find(string? channelName)
   {
      return string.IsNullOrWhiteSpace(channelName)
         ? null
         : Definitions.FirstOrDefault(
            definition => definition.Matches(channelName)
         );
   }
}
