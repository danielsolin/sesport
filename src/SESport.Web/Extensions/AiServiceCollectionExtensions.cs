using SESport.AI.Clients;
using SESport.AI.Jobs;
using SESport.AI.WebPages;
using SESport.AI.WebSearch;
using SESport.Core.AI;

namespace SESport.Web.Extensions;

public static class AiServiceCollectionExtensions
{
   public static IServiceCollection AddAiPlatform(
      this IServiceCollection services
   )
   {
      services.AddScoped<AiRepository>();
      services.AddScoped<AiJobDefinitionRepository>();
      services.AddScoped<AiJobRunRepository>();
      services.AddScoped<AiRunApplicationRepository>();
      services.AddScoped<AiAutomationRepository>();
      services.AddScoped<IAiJobDefinitionRepository>(
         serviceProvider => serviceProvider
            .GetRequiredService<AiJobDefinitionRepository>()
      );
      services.AddScoped<IAiJobRunRepository>(
         serviceProvider => serviceProvider
            .GetRequiredService<AiJobRunRepository>()
      );
      services.AddScoped<AiAdminRepository>();
      services.AddSingleton<AiJobExecutionGate>();
      services.AddSingleton<IAiPromptRenderer, TemplatePromptRenderer>();
      services.AddSingleton<SearchRateLimiter>();
      services.AddSingleton<WebSearchCache>();
      services.AddScoped<AiJobEligibilityService>();
      services.AddScoped<AiJobRunner>();
      services.AddScoped<IAiJobRunner>(
         serviceProvider => serviceProvider.GetRequiredService<AiJobRunner>()
      );
      services.AddScoped<PersonFactsService>();
      services.AddScoped<IAiAutomationService, AiAutomationService>();
      services.AddScoped<TextTranslationService>();
      services.AddScoped<IAiJobProcessor, AiJobPostProcessor>();
      // OpenRouter client registered for potential future use. Currently not actively used
      // in production scenarios, but kept available for flexibility.
      services.AddHttpClient<OpenRouterClient>(client =>
      {
         client.Timeout = AiDefaults.OpenRouterHttpClientTimeout;
      });
      services.AddHttpClient<LlamaServerClient>(client =>
      {
         client.Timeout = AiDefaults.LlamaServerHttpClientTimeout;
      });
      services.AddTransient<CodexCliClient>();
      services.AddTransient<OpenCodeCliClient>();
      services.AddTransient<GoogleTranslateClient>();
      services.AddTransient<IAiProviderClient>(serviceProvider =>
         serviceProvider.GetRequiredService<OpenRouterClient>()
      );
      services.AddTransient<IAiProviderClient>(serviceProvider =>
         serviceProvider.GetRequiredService<LlamaServerClient>()
      );
      services.AddTransient<IAiProviderClient>(serviceProvider =>
         serviceProvider.GetRequiredService<CodexCliClient>()
      );
      services.AddTransient<IAiProviderClient>(serviceProvider =>
         serviceProvider.GetRequiredService<OpenCodeCliClient>()
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
