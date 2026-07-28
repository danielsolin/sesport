using SESport.Data.Repositories;

namespace SESport.Web.Extensions;

public static class WebServiceCollectionExtensions
{
   public static IServiceCollection AddWebApplicationServices(
      this IServiceCollection services
   )
   {
      services.AddScoped<ActivityRepository>();
      services.AddScoped<FactRepository>();
      services.AddScoped<SourceReferenceRepository>();
      services.AddScoped<AdminRepository>();
      services.AddScoped<AdminBroadcastRepository>();
      services.AddScoped<DashboardRepository>();
      services.AddSingleton<ActivityDatePreferenceStore>();
      services.AddSingleton<BroadcastDatePreferenceStore>();
      services.AddSingleton<RunDatePreferenceStore>();
      services.AddSingleton<EntityDatePreferenceStore>();
      services.AddScoped<ActivityAiInputBuilder>();
      services.AddScoped<ActivityEditPageService>();
      services.AddScoped<ActivityIndexPageService>();
      services.AddScoped<PublicActivityTimelineBuilder>();
      services.AddScoped<BroadcastParticipationService>();
      services.AddHostedService<ActivityAiResultCatchUpWorker>();
      services.AddHostedService<AiPendingRunWorker>();
      services.AddHostedService<AiRunTimeoutWorker>();

      return services;
   }
}
