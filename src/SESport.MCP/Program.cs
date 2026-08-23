using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SESport.AI.WebPages;
using SESport.AI.WebSearch;
using SESport.Core.Configuration;
using SESport.MCP;

var services = new ServiceCollection();
services.AddLogging();

var searxngOptions = new ConfigurationBuilder()
   .AddEnvironmentVariables()
   .Build()
   .GetSection(ApplicationConfigurationKeys.SearxngSection)
   .Get<SearxngWebSearchClientOptions>() ??
   new SearxngWebSearchClientOptions();
services.AddSingleton(searxngOptions);

services.AddSingleton<SearchRateLimiter>();
services.AddSingleton<WebSearchCache>();
services.AddHttpClient<SearxngWebSearchClient>(
   client => client.Timeout = AiDefaults.SearxngHttpClientTimeout
);
services.AddScoped<IWebSearchClient>(
   serviceProvider => new CachedWebSearchClient(
      serviceProvider.GetRequiredService<SearxngWebSearchClient>(),
      serviceProvider.GetRequiredService<WebSearchCache>(),
      serviceProvider.GetRequiredService<SearxngWebSearchClientOptions>()
   )
);
services.AddHttpClient<
   IWebPageContentClient,
   WebPageContentClient
>(
   client =>
      client.Timeout = AiDefaults.WebPageContentHttpClientTimeout
);
services.AddScoped<WebSearchTool>();
services.AddScoped<WebPageTool>();

var mcpBuilder = services.AddMcpServer();
mcpBuilder
   .WithStdioServerTransport()
   .WithTools<WebSearchTool>()
   .WithTools<WebPageTool>();

var serviceProvider = services.BuildServiceProvider();
await using var scope = serviceProvider.CreateAsyncScope();
var mcpServer = scope.ServiceProvider.GetRequiredService<McpServer>();
await mcpServer.RunAsync();
