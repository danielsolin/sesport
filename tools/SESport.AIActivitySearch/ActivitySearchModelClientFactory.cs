using SESport.Core.AIActivitySearch;

namespace SESport.Tools.AIActivitySearch;

internal static class ActivitySearchModelClientFactory
{
   public static IActivitySearchModelClient Create(
      HttpClient httpClient,
      ToolOptions options
   )
   {
      if (options.LmStudioPluginId is not null)
      {
         return new LmStudioChatActivitySearchClient(
            httpClient,
            new LmStudioChatActivitySearchClientOptions(
               options.LmStudioBaseAddress,
               options.Model,
               options.LmStudioPluginId,
               options.LmStudioAllowedTools,
               options.ApiKey
            )
         );
      }

      return new OpenAiResponsesActivitySearchClient(
         httpClient,
         new OpenAiResponsesActivitySearchClientOptions(
            options.BaseAddress,
            options.Model,
            options.ApiKey,
            options.WebSearchToolType
         )
      );
   }
}
