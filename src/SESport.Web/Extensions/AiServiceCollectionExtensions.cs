using SESport.AI.Clients;
using SESport.AI.Interfaces;
using SESport.AI.Jobs;
using SESport.AI.Prompts;
using SESport.AI.WebPages;
using SESport.AI.WebSearch;
using SESport.Core.AI;
using SESport.Core.Configuration;
using SESport.Web.Services;
using SESport.Data.AI;

namespace SESport.Web.Extensions;

public static class AiServiceCollectionExtensions
{
   public static IServiceCollection AddAiPlatform(
      this IServiceCollection services
   )
   {
      services.AddScoped<AiRepository>();
      services.AddScoped<IAiJobDefinitionRepository, AiRepository>();
      services.AddScoped<IAiJobRunRepository, AiRepository>();
      services.AddScoped<AiAdminRepository>();
      services.AddSingleton<AiJobExecutionGate>();
      services.AddSingleton<IAiPromptRenderer, TemplatePromptRenderer>();
      services.AddSingleton<SearchRateLimiter>();
      services.AddSingleton<WebSearchCache>();
      services.AddScoped<AiJobRunner>();
      services.AddScoped<IAiJobRunner>(
         serviceProvider => serviceProvider.GetRequiredService<AiJobRunner>()
      );
      services.AddScoped<IAiJobProcessor, ActivityTeaserJobProcessor>();
      // OpenRouter is dormant for AI jobs. Keep registration available for
      // archived configs, but do not assume LlamaServerClient feature parity.
      services.AddHttpClient<
         IAiProviderClient,
         OpenRouterClient
      >(client =>
      {
         client.Timeout = AiDefaults.OpenRouterHttpClientTimeout;
      });
      services.AddHttpClient<
         IAiProviderClient,
         LlamaServerClient
      >(client =>
      {
         client.Timeout = AiDefaults.LlamaServerHttpClientTimeout;
      });
      services.AddHttpClient<SearxngWebSearchClient>(
         client =>
         {
            client.Timeout = AiDefaults.SearxngHttpClientTimeout;
         }
      );
      services.AddScoped<IWebSearchClient>(serviceProvider =>
         new CachedWebSearchClient(
            serviceProvider.GetRequiredService<SearxngWebSearchClient>(),
            serviceProvider.GetRequiredService<WebSearchCache>(),
            serviceProvider.GetRequiredService<SearxngWebSearchClientOptions>()
         )
      );
      services.AddHttpClient<GoogleWebSearchClient>(
         client =>
         {
            client.Timeout = AiDefaults.GoogleWebSearchHttpClientTimeout;
         }
      );
      services.AddHttpClient<
         IWebPageContentClient,
         WebPageContentClient
      >(client =>
      {
         client.Timeout = AiDefaults.WebPageContentHttpClientTimeout;
      });

      return services;
   }
}
