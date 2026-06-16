using SESport.AI;
using SESport.AI.Abstractions;
using SESport.AI.Persistence;
using SESport.AI.Providers;
using SESport.AI.Rendering;
using SESport.AI.Services;

namespace SESport.Web.Extensions;

public static class AiServiceCollectionExtensions
{
   public static IServiceCollection AddAiPlatform(
      this IServiceCollection services,
      IConfiguration configuration
   )
   {
      services.AddSingleton(
         _ => configuration.GetSection("SearXNG")
            .Get<SearxngWebSearchClientOptions>() ??
            new SearxngWebSearchClientOptions()
      );
      services.AddScoped<AiRepository>();
      services.AddScoped<IAiJobDefinitionRepository, AiRepository>();
      services.AddScoped<IAiJobRunRepository, AiRepository>();
      services.AddScoped<AiAdminRepository>();
      services.AddSingleton<AiJobExecutionGate>();
      services.AddSingleton<IAiPromptRenderer, TemplatePromptRenderer>();
      services.AddScoped<IAiJobRunner, AiJobRunner>();
      services.AddScoped<IAiJobProcessor, AiJobRunner>();
      services.AddHttpClient<
         IAiProviderClient,
         OpenRouterClient
      >(client =>
      {
         client.Timeout = TimeSpan.FromSeconds(300);
      });
      services.AddHttpClient<
         IAiProviderClient,
         LlamaServerClient
      >(client =>
      {
         client.Timeout = TimeSpan.FromMinutes(20);
      });
      services.AddHttpClient<
         IWebSearchClient,
         SearxngWebSearchClient
      >(client =>
      {
         client.Timeout = TimeSpan.FromSeconds(30);
      });
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
