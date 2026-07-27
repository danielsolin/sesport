namespace SESport.Core.Configuration;

public static class ConfigurationEnvironment
{
   private const string SearxngBaseUrlVariable = "SearXNG__BaseUrl";

   public static bool HasSearxngBaseUrl =>
      !string.IsNullOrWhiteSpace(
         Environment.GetEnvironmentVariable(SearxngBaseUrlVariable)
      );
}
