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
      services.AddScoped<PersonBioService>();
      services.AddScoped<TextTranslationService>();
      services.AddScoped<IAiJobProcessor, ActivityTeaserJobProcessor>();
      // OpenRouter is dormant for AI jobs. Keep registration available for
      // archived configs, but do not assume LlamaServerClient feature parity.
      services.AddHttpClient<OpenRouterClient>(client =>
      {
         client.Timeout = TimeSpan.FromSeconds(300);
      });
      services.AddHttpClient<LlamaServerClient>(client =>
      {
         client.Timeout = TimeSpan.FromMinutes(20);
      });
      services.AddHttpClient<GoogleTranslateClient>(client =>
      {
         client.Timeout = TimeSpan.FromSeconds(60);
      });
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
            client.Timeout = TimeSpan.FromSeconds(60);
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
         client.Timeout = TimeSpan.FromSeconds(30);
      });

      return services;
   }
}
