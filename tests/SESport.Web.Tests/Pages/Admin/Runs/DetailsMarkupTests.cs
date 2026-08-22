namespace SESport.Core.Tests.Pages.Admin.Runs;

public sealed class DetailsMarkupTests
{
   [Fact]
   public async Task DetailsPageDoesNotShowToolsDescription()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Runs/Details.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var toolTracePath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Ajax/Poll/RunToolTrace.cshtml"
      );
      var toolTraceHtml = await File.ReadAllTextAsync(toolTracePath);
      var toolTraceScriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/Admin/js/run-tool-trace.js"
      );
      var toolTraceScript = await File.ReadAllTextAsync(
         toolTraceScriptPath
      );
      var siteCssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/site.css"
      );
      var siteCss = await File.ReadAllTextAsync(siteCssPath);

      AssertOrder(
         html,
         "Execution Environment",
         "run-execution-environment-form",
         "Save",
         "data-run-tool-trace",
         "summary>Conversation history summary</summary>",
         "summary>Output</summary>",
         "summary>Raw final request</summary>",
         "summary>Raw final response</summary>",
         "summary>System prompt</summary>",
         "summary>Rendered prompt</summary>",
         "summary>Input payload</summary>",
         "summary>User prompt template</summary>"
      );
      Assert.Contains("<dt>Temperature</dt>", html);
      Assert.Contains("<h2>Token usage</h2>", html);
      Assert.Contains("Input tokens (total)", html);
      Assert.Contains("Cached input tokens", html);
      Assert.Contains("Uncached input tokens", html);
      Assert.Contains("Final payload chars", html);
      Assert.Contains("Max payload chars", html);
      Assert.Contains("DetailsModel.FormatJson(Model.Run.OutputText)", html);
      Assert.Contains("asp-for=\"ExecutionEnvironment\"", html);
      Assert.Contains(
         "src=\"~/Admin/js/run-tool-trace.js\"",
         html
      );
      Assert.DoesNotContain("tool-trace-turn-header-main", html);
      Assert.Contains("tool-trace-turn-header-main", toolTraceHtml);
      Assert.Contains("data-run-tool-trace-partial", toolTraceHtml);
      Assert.Contains("Action details", toolTraceHtml);
      Assert.Contains("tool-trace-turn-action-summary", toolTraceHtml);
      Assert.Contains("Raw event JSON", toolTraceHtml);
      Assert.Contains("tool-trace-action-status", toolTraceHtml);
      Assert.Contains("tool-trace-output-count", toolTraceHtml);
      Assert.Contains("summary>Command</summary>", toolTraceHtml);
      Assert.Contains("summary>Command output</summary>", toolTraceHtml);
      Assert.Contains("Output characters", toolTraceHtml);
      Assert.Contains(
         ".tool-trace-action {\n"
         + "   display: grid;\n"
         + "   gap: 0;\n"
         + "   min-width: 0;",
         siteCss
      );
      Assert.Contains(
         ".tool-trace-action-content {\n"
         + "   display: grid;\n"
         + "   gap: 8px;\n"
         + "   min-width: 0;",
         siteCss
      );
      Assert.Contains(
         ".tool-trace-action-details {\n"
         + "   display: grid;\n"
         + "   gap: 6px;\n"
         + "   min-width: 0;",
         siteCss
      );
      Assert.Contains("summary>Raw tool trace JSON</summary>", toolTraceHtml);
      Assert.Contains("FormatJsonOrRetentionNotice", html);
      Assert.Contains("FormatJsonOrRetentionNotice", toolTraceHtml);
      Assert.Contains(
         "Detailed tool trace was removed by retention",
         toolTraceHtml
      );
      Assert.DoesNotContain("Full trace", toolTraceHtml);
      Assert.Contains("Round", toolTraceHtml);
      Assert.Contains("pollIntervalMilliseconds = 10000", toolTraceScript);
      Assert.Contains("updateToolTrace()", toolTraceScript);
      Assert.Contains(
         "host.dataset.runStatus !== \"running\"",
         toolTraceScript
      );
      Assert.Contains("data-run-status", toolTraceHtml);
      Assert.DoesNotContain("Tools description", html);
   }

   private static void AssertOrder(string html, params string[] parts)
   {
      var lastIndex = -1;

      foreach(var part in parts)
      {
         var index = html.IndexOf(part, StringComparison.Ordinal);

         Assert.True(index >= 0, $"Missing expected text: {part}");
         Assert.True(
            index > lastIndex,
            $"Expected '{part}' after previous section."
         );

         lastIndex = index;
      }
   }
}
