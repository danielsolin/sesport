using Microsoft.AspNetCore.Authentication.Cookies;
using Lib.Net.Http.WebPush;

using SESport.Data;

var builder = WebApplication.CreateBuilder(args);
var adminOptions = builder.Configuration.GetSection(
      ApplicationConfigurationKeys.AdminSection
   )
   .Get<AdminLoginOptions>() ??
   new AdminLoginOptions();
var codexCliOptions = builder.Configuration.GetSection(
      ApplicationConfigurationKeys.CodexCliSection
   )
   .Get<CodexCliOptions>() ??
   new CodexCliOptions();
var searxngOptions = builder.Configuration.GetSection(
      ApplicationConfigurationKeys.SearxngSection
   )
   .Get<SearxngWebSearchClientOptions>() ??
   new SearxngWebSearchClientOptions();
var memberAuthOptions = builder.Configuration.GetSection(
      ApplicationConfigurationKeys.MemberAuthSection
   )
   .Get<MemberAuthOptions>() ??
   new MemberAuthOptions();
var memberPushOptions = builder.Configuration.GetSection(
      ApplicationConfigurationKeys.MemberPushSection
   )
   .Get<MemberPushOptions>() ??
   new MemberPushOptions();
var smtpEmailOptions = builder.Configuration.GetSection(
      ApplicationConfigurationKeys.SmtpSection
   )
   .Get<SmtpEmailOptions>() ??
   new SmtpEmailOptions();
var configuredWebStatsOptions = builder.Configuration.GetSection(
      ApplicationConfigurationKeys.WebStatsSection
   )
   .Get<WebStatsOptions>() ??
   new WebStatsOptions();
var publicSiteOptions = builder.Configuration.GetSection(
      ApplicationConfigurationKeys.PublicSiteSection
   )
   .Get<PublicSiteOptions>() ??
   new PublicSiteOptions();
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
      builder.Configuration.GetConnectionString(
         ApplicationConfigurationKeys.DefaultConnectionString
      )
   )
);
builder.Services.AddSingleton(adminOptions);
builder.Services.AddSingleton(codexCliOptions);
builder.Services.AddSingleton(searxngOptions);
builder.Services.AddSingleton(memberAuthOptions);
builder.Services.AddSingleton(memberPushOptions);
builder.Services.AddSingleton(smtpEmailOptions);
builder.Services.AddSingleton(webStatsOptions);
builder.Services.AddSingleton(publicSiteOptions);
builder.Services.AddHttpClient<PushServiceClient>();
builder.Services.AddWebApplicationServices();
builder.Services.AddAiPlatform();
builder.Services.AddSingleton<IMemberEmailSender, SmtpEmailSender>();
builder.Services
   .AddAuthentication(
      options =>
      {
         options.DefaultAuthenticateScheme =
            MemberAuthenticationDefaults.Scheme;
         options.DefaultChallengeScheme = MemberAuthenticationDefaults.Scheme;
      }
   )
   .AddCookie(
      CookieAuthenticationDefaults.AuthenticationScheme,
      options =>
      {
         options.LoginPath = "/Admin/Login";
         options.AccessDeniedPath = "/Admin/Login";
      }
   )
   .AddCookie(
      MemberAuthenticationDefaults.Scheme,
      options =>
      {
         options.Cookie.Name = MemberAuthenticationDefaults.CookieName;
         options.LoginPath = "/login";
         options.AccessDeniedPath = "/login";
         options.ExpireTimeSpan = memberAuthOptions.MemberCookieLifetime;
         options.SlidingExpiration = true;
         options.Cookie.HttpOnly = true;
         options.Cookie.SameSite = SameSiteMode.Lax;
         options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
      }
   );
builder.Services
   .AddAuthorizationBuilder()
   .AddPolicy(
      "Admin",
      policy =>
      {
         policy.AddAuthenticationSchemes(
            CookieAuthenticationDefaults.AuthenticationScheme
         );
         policy.RequireAuthenticatedUser();
         policy.RequireRole(AdminAuthenticationDefaults.Role);
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
   ConfigurationEnvironment.HasSearxngBaseUrl
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
