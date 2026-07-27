using SESport.AI.Clients;
using SESport.AI.Interfaces;
using SESport.AI.Jobs;
using SESport.AI.Prompts;
using SESport.AI.WebPages;
using SESport.AI.WebSearch;
using SESport.Core.AI;
using SESport.Core.Configuration;
using SESport.Data.AI;
using SESport.Web.Services;

namespace SESport.Web.Extensions;

public static class AiServiceCollectionExtensions
{
   public static IServiceCollection AddAiPlatform(
      this IServiceCollection services
   )
   {
      services.AddScoped<AiRepository>();
      services.AddScoped<AiAutomationRepository>();
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
      services.AddScoped<PersonFactsService>();
      services.AddScoped<ActivityAiInputBuilder>();
      services.AddScoped<IAiAutomationService, AiAutomationService>();
      services.AddScoped<TextTranslationService>();
      services.AddScoped<IAiJobProcessor, AiJobPostProcessor>();
      // OpenRouter is dormant for AI jobs. Keep registration available for
      // archived configs, but do not assume LlamaServerClient feature parity.
      services.AddHttpClient<OpenRouterClient>(client =>
      {
         client.Timeout = AiDefaults.OpenRouterHttpClientTimeout;
      });
      services.AddHttpClient<LlamaServerClient>(client =>
      {
         client.Timeout = AiDefaults.LlamaServerHttpClientTimeout;
      });
      services.AddTransient<GoogleTranslateClient>();
      services.AddTransient<IAiProviderClient>(serviceProvider =>
         serviceProvider.GetRequiredService<OpenRouterClient>()
      );
      services.AddTransient<IAiProviderClient>(serviceProvider =>
         serviceProvider.GetRequiredService<LlamaServerClient>()
      );
      services.AddTransient<IAiProviderClient>(serviceProvider =>
         serviceProvider.GetRequiredService<GoogleTranslateClient>()
      );
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
