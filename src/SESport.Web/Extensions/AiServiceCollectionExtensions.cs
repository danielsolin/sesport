using SESport.AI.Clients;
using SESport.AI.Jobs;
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
      services.AddSingleton<AiPendingRunWakeSignal>();
      services.AddSingleton<IAiPromptRenderer, TemplatePromptRenderer>();
      services.AddScoped<AiJobEligibilityService>();
      services.AddScoped<AiJobRunner>();
      services.AddScoped<IAiJobRunner>(
         serviceProvider => serviceProvider.GetRequiredService<AiJobRunner>()
      );
      services.AddScoped<PersonFactsService>();
      services.AddScoped<IAiAutomationService, AiAutomationService>();
      services.AddScoped<TextTranslationService>();
      services.AddScoped<IAiJobProcessor, AiJobPostProcessor>();
      services.AddTransient<CodexCliClient>();
      services.AddTransient<OpenCodeCliClient>();
      services.AddTransient<GoogleTranslateClient>();
      services.AddTransient<IAiProviderClient>(serviceProvider =>
         serviceProvider.GetRequiredService<CodexCliClient>()
      );
      services.AddTransient<IAiProviderClient>(serviceProvider =>
         serviceProvider.GetRequiredService<OpenCodeCliClient>()
      );
      services.AddTransient<IAiProviderClient>(serviceProvider =>
         serviceProvider.GetRequiredService<GoogleTranslateClient>()
      );

      return services;
   }
}
