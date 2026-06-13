using SESport.AI;
using SESport.AI.Abstractions;
using SESport.AI.Persistence;
using SESport.AI.Providers;
using SESport.AI.Rendering;

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
      services.AddSingleton<IAiPromptRenderer, TemplatePromptRenderer>();
      services.AddScoped<IAiJobRunner, AiJobRunner>();
      services.AddHttpClient<
         IAiProviderClient,
         OpenRouterClient
      >(client =>
      {
         client.Timeout = TimeSpan.FromSeconds(300);
      });
      services.AddHttpClient<
         IAiProviderClient,
         LmStudioClient
      >();
      services.AddHttpClient<
         IAiProviderClient,
         LlamaServerClient
      >(client =>
      {
         client.Timeout = TimeSpan.FromMinutes(20);
      });
      services.AddHttpClient<IWebSearchClient, DuckDuckGoWebSearchClient>(
         client =>
         {
            client.Timeout = TimeSpan.FromSeconds(30);
         }
      );

      return services;
   }
}
