using Microsoft.Extensions.DependencyInjection;
using SESport.Core.AI;
using SESport.Core.AI.Abstractions;
using SESport.Core.AI.Providers;
using SESport.Core.AI.Rendering;
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
      services.AddSingleton<IAiPromptRenderer, TemplatePromptRenderer>();
      services.AddScoped<IAiJobRunner, AiJobRunner>();
      services.AddHttpClient<
         IAiProviderClient,
         OpenRouterResponsesAiProviderClient
      >();

      return services;
   }
}
