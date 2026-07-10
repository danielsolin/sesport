namespace SESport.Core.Domain;

public static class PrimaryCountry
{
   public const string TwoLetterCode = "SE";
   public const string ThreeLetterCode = "SWE";
   public const string CountryName = "Sweden";
   public const string LocalDisplayName = "Sverige";
   public const string LanguageName = "Swedish";
   public const string BroadcastChannelPrefix = TwoLetterCode + " - ";
   public const string BroadcastChannelPrefixRegex =
      "^" + BroadcastChannelPrefix;

   public static string RemoveBroadcastChannelPrefix(string value)
   {
      return value.StartsWith(
         BroadcastChannelPrefix,
         StringComparison.OrdinalIgnoreCase
      )
         ? value[BroadcastChannelPrefix.Length..]
         : value;
   }
}
