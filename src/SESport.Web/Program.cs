using Microsoft.AspNetCore.Authentication.Cookies;

using SESport.Core.Configuration;
using SESport.Core.AI;
using SESport.Data;
using SESport.Web.Extensions;
using SESport.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var adminOptions = builder.Configuration.GetSection("Admin")
   .Get<AdminLoginOptions>() ??
   new AdminLoginOptions();
var searxngOptions = builder.Configuration.GetSection("SearXNG")
   .Get<SearxngWebSearchClientOptions>() ??
   new SearxngWebSearchClientOptions();
var configuredWebStatsOptions = builder.Configuration.GetSection("WebStats")
   .Get<WebStatsOptions>() ??
   new WebStatsOptions();
var webStatsOptions = configuredWebStatsOptions with
{
   ReportDirectory = WebStatsReportDirectoryResolver.Resolve(
      configuredWebStatsOptions.ReportDirectory,
      builder.Environment.ContentRootPath,
      AppContext.BaseDirectory
   )
};
builder.Services.AddSingleton(
   _ => PostgresDataSourceFactory.CreateDefault(
      builder.Configuration.GetConnectionString("Default")
   )
);
builder.Services.AddSingleton(adminOptions);
builder.Services.AddSingleton(searxngOptions);
builder.Services.AddSingleton(webStatsOptions);
builder.Services.AddAiPlatform();
builder.Services.AddSingleton<ActivityDatePreferenceStore>();
builder.Services.AddSingleton<BroadcastDatePreferenceStore>();
builder.Services.AddSingleton<RunDatePreferenceStore>();
builder.Services.AddSingleton<EntityDatePreferenceStore>();
builder.Services.AddScoped<ActivityEditPageService>();
builder.Services.AddScoped<ActivityIndexPageService>();
builder.Services.AddScoped<ActivityRepository>();
builder.Services.AddScoped<FactRepository>();
builder.Services.AddScoped<SourceReferenceRepository>();
builder.Services.AddScoped<PublicActivityTimelineBuilder>();
builder.Services.AddScoped<AdminRepository>();
builder.Services.AddScoped<AdminBroadcastRepository>();
builder.Services.AddScoped<DashboardRepository>();
builder.Services.AddScoped<BroadcastParticipationService>();
builder.Services.AddHostedService<ActivityAiResultCatchUpWorker>();
builder.Services.AddHostedService<AiPendingRunWorker>();
builder.Services.AddHostedService<AiRunTimeoutWorker>();
builder.Services
   .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
   .AddCookie(
      options =>
      {
         options.LoginPath = "/Admin/Login";
         options.AccessDeniedPath = "/Admin/Login";
      }
   );
builder.Services
   .AddAuthorizationBuilder()
   .AddPolicy(
      "Admin",
      policy =>
      {
         policy.RequireAuthenticatedUser();
      }
   );
builder.Services.AddRazorPages(
   options =>
   {
      options.Conventions.AuthorizeFolder("/Admin", "Admin");
      options.Conventions.AllowAnonymousToPage("/Admin/Login");
   }
);

var app = builder.Build();

app.Logger.LogInformation(
   "Execution environment: {ExecutionEnvironment}",
   ExecutionEnvironment.Current
);
app.Logger.LogInformation(
   "SearXNG env vars present in process: baseUrl={HasBaseUrl}",
   !string.IsNullOrWhiteSpace(
      Environment.GetEnvironmentVariable("SearXNG__BaseUrl")
   )
);
app.Logger.LogInformation(
   "SearXNG config bound: baseUrl={BaseUrl}",
   searxngOptions.BaseUrl ??
      SearxngWebSearchClientOptions.DefaultBaseUrl
);

if(!app.Environment.IsDevelopment())
{
   app.UseExceptionHandler("/Error");
   app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(
   async (context, next) =>
   {
      context.Response.OnStarting(
         () =>
         {
            if(
               context.Response.ContentType?.StartsWith(
                  "text/html",
                  StringComparison.OrdinalIgnoreCase
               ) == true
            )
            {
               context.Response.Headers.CacheControl = "no-cache";
            }

            return Task.CompletedTask;
         }
      );
      await next();
   }
);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
