using ModelContextProtocol.AspNetCore;

using SESport.MCP;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(
   Environment.GetEnvironmentVariable("SESPORT_MCP_URL")
      ?? "http://127.0.0.1:5110"
);

builder.Services.AddLogging();
builder.Logging.AddFilter(
   "System.Net.Http.HttpClient.IWebPageContentClient",
   LogLevel.Warning
);

var searxngOptions = new ConfigurationBuilder()
   .AddEnvironmentVariables()
   .Build()
   .GetSection(ApplicationConfigurationKeys.SearxngSection)
   .Get<SearxngWebSearchClientOptions>() ??
   new SearxngWebSearchClientOptions();
builder.Services.AddSingleton(searxngOptions);

builder.Services.AddSingleton<SearchRateLimiter>();
builder.Services.AddSingleton<WebSearchCache>();
builder.Services.AddHttpClient<SearxngWebSearchClient>(
   client => client.Timeout = AiDefaults.SearxngHttpClientTimeout
);
builder.Services.AddScoped<IWebSearchClient>(
   serviceProvider => new CachedWebSearchClient(
      serviceProvider.GetRequiredService<SearxngWebSearchClient>(),
      serviceProvider.GetRequiredService<WebSearchCache>(),
      serviceProvider.GetRequiredService<SearxngWebSearchClientOptions>()
   )
);
builder.Services.AddHttpClient<
   IWebPageContentClient,
   WebPageContentClient
>(
   client =>
      client.Timeout = AiDefaults.WebPageContentHttpClientTimeout
);
builder.Services.AddScoped<WebSearchTool>();
builder.Services.AddScoped<WebPageTool>();
builder.Services.AddScoped<WebFindInPageTool>();

var serializerOptions = new JsonSerializerOptions
{
   DefaultIgnoreCondition = JsonIgnoreCondition.Never,
   TypeInfoResolver = new DefaultJsonTypeInfoResolver()
};

builder.Services.AddMcpServer()
   .WithHttpTransport(options =>
   {
      options.SessionMode = HttpServerSessionMode.Stateless;
   })
   .WithTools<WebSearchTool>(serializerOptions)
   .WithTools<WebPageTool>(serializerOptions)
   .WithTools<WebFindInPageTool>(serializerOptions);

var app = builder.Build();

app.MapMcp();
app.Run();
