namespace SESport.Core.Domain;

public static class PrimaryCountry
{
   public const string Id = "se";
   public const string TwoLetterCode = "SE";
   public const string ThreeLetterCode = "SWE";
   public const string CountryName = "Sweden";
   public const string LocalDisplayName = "Sverige";
   public const string LanguageName = "Swedish";
   public const string CultureName = "sv-SE";
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

   public static string NormalizeBroadcastChannelName(string value)
   {
      var normalizedValue = RemoveBroadcastChannelPrefix(value);

      normalizedValue = RemoveBroadcastChannelSuffix(
         normalizedValue,
         " (SE)"
      );
      normalizedValue = RemoveBroadcastChannelSuffix(
         normalizedValue,
         " SE"
      );

      return normalizedValue;
   }

   private static string RemoveBroadcastChannelSuffix(
      string value,
      string suffix
   )
   {
      return value.EndsWith(
            suffix,
            StringComparison.OrdinalIgnoreCase
         ) && value.Length > suffix.Length
         ? value[..^suffix.Length]
         : value;
   }
}
