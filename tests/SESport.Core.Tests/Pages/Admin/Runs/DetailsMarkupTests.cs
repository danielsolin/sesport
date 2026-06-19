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

      AssertOrder(
         html,
         "Execution Environment",
         "run-execution-environment-form",
         "Save",
         "tool-trace-summary-content",
         "summary>Conversation history summary</summary>",
         "summary>Output</summary>",
         "summary>Raw final request</summary>",
         "summary>Raw final response</summary>",
         "summary>Raw tool trace JSON</summary>",
         "summary>System prompt</summary>",
         "summary>Rendered prompt</summary>",
         "summary>Input payload</summary>",
         "summary>User prompt template</summary>"
      );
      Assert.Contains("<dt>Temperature</dt>", html);
      Assert.Contains("Final conversation chars", html);
      Assert.Contains("Max conversation chars", html);
      Assert.Contains("asp-for=\"ExecutionEnvironment\"", html);
      Assert.Contains("tool-trace-turn-header-main", html);
      Assert.DoesNotContain("Full trace", html);
      Assert.Contains("Round", html);
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
