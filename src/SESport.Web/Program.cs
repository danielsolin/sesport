using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;
using SESport.Web.Extensions;
using SESport.Web.Services;
using SESport.Data;

var builder = WebApplication.CreateBuilder(args);
var adminPassword = builder.Configuration["Admin:Password"];
var defaultConnectionString =
   "Host=localhost;Port=5432;Database=sesport;" +
   "Username=sesport;Password=sesport";
builder.Services.AddSingleton(
   _ => NpgsqlDataSource.Create(
      builder.Configuration.GetConnectionString("Default") ??
      defaultConnectionString
   )
);
builder.Services.AddAiPlatform(builder.Configuration);
builder.Services.AddSingleton<AdminDatePreferenceStore>();
builder.Services.AddScoped<ActivityEditPageService>();
builder.Services.AddScoped<ActivityIndexPageService>();
builder.Services.AddScoped<ActivityRepository>();
builder.Services.AddScoped<PublicActivityTimelineBuilder>();
builder.Services.AddScoped<AdminRepository>();
builder.Services.AddScoped<AuditRepository>();
builder.Services.AddScoped<BroadcastRepository>();
builder.Services.AddScoped<BroadcastParticipationService>();
builder.Services
   .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
   .AddCookie(
      options =>
      {
         options.LoginPath = "/Admin/Login";
         options.AccessDeniedPath = "/Admin/Login";
      }
   );
builder.Services.AddAuthorization(
   options =>
   {
      options.AddPolicy(
         "Admin",
         policy =>
         {
            policy.RequireAuthenticatedUser();
         }
      );
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

if (!app.Environment.IsDevelopment())
{
   app.UseExceptionHandler("/Error");
   app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
