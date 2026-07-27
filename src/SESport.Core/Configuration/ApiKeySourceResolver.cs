namespace SESport.Core.Configuration;

public static class ApiKeySourceResolver
{
   public static string? Resolve(string? apiKeySource)
   {
      if(string.IsNullOrWhiteSpace(apiKeySource))
      {
         return null;
      }

      const string environmentPrefix = "environment:";
      const string keyPrefix = "key:";

      if(apiKeySource.StartsWith(
         environmentPrefix,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         var envVar = apiKeySource[environmentPrefix.Length..].Trim();
         return string.IsNullOrWhiteSpace(envVar)
            ? null
            : Environment.GetEnvironmentVariable(envVar);
      }

      if(apiKeySource.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase))
      {
         var apiKey = apiKeySource[keyPrefix.Length..].Trim();
         return string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
      }

      return null;
   }
}
