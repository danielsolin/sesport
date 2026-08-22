using System.Net;

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
      services.AddScoped<ActivityParticipantAiResultRepository>();
      services.AddScoped<AdminRepository>();
      services.AddScoped<AdminBroadcastRepository>();
      services.AddScoped<DashboardRepository>();
      services.AddScoped<PublicStatisticsRepository>();
      services.AddScoped<TodoRepository>();
      services.AddScoped<MemberRepository>();
      services.AddScoped<MemberWatchRepository>();
      services.AddScoped<MemberPushRepository>();
      services.AddScoped<MemberPushNotificationSender>();
      services.AddScoped<IMemberRepository>(
         serviceProvider => serviceProvider
            .GetRequiredService<MemberRepository>()
      );
      services.AddScoped<MemberAuthService>();
      services.AddSingleton<ActivityDatePreferenceStore>();
      services.AddSingleton<BroadcastDatePreferenceStore>();
      services.AddSingleton<RunDatePreferenceStore>();
      services.AddSingleton<EntityDatePreferenceStore>();
      services.AddSingleton<FilterPreferenceStore>();
      services.AddScoped<ActivityAiInputBuilder>();
      services.AddScoped<ActivityParticipantAiResultService>();
      services.AddScoped<ActivityEditPageService>();
      services.AddScoped<ActivityIndexPageService>();
      services.AddScoped<PublicActivityTimelineBuilder>();
      services.AddScoped<BroadcastParticipationService>();
      services.AddHttpClient<WikimediaCommonsImageClient>(
         client =>
         {
            client.Timeout = WikimediaImageDefaults.HttpClientTimeout;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
               "SESport EntityImageReplacement/1.0 " +
               "(https://github.com/danielsolin/sesport)"
            );
            client.DefaultRequestHeaders.AcceptEncoding.ParseAdd(
               "gzip, deflate, br"
            );
         }
      ).ConfigurePrimaryHttpMessageHandler(
         () => new HttpClientHandler
         {
            AutomaticDecompression = DecompressionMethods.All
         }
      );
      services.AddScoped<
         IEntityImageReplacementService,
         WikimediaCommonsEntityImageReplacementService
      >();
      services.AddHostedService<ActivityAiResultCatchUpWorker>();
      services.AddHostedService<AiPendingRunWorker>();
      services.AddHostedService<AiRunTimeoutWorker>();
      services.AddHostedService<MemberPushNotificationWorker>();

      return services;
   }
}
