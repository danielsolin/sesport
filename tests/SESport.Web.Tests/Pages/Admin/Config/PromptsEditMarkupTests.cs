namespace SESport.Core.Tests.Pages.Admin.Config;

public sealed class PromptsEditMarkupTests
{
   [Fact]
   public async Task EditPageShowsCodexReasoningSelectorAndProviderMetadata()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Config/Ai/Prompts/Edit.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains("data-provider-kind", html);
      Assert.Contains("data-codex-reasoning-field", html);
      Assert.Contains("Prompt.CodexReasoningEffort", html);
      Assert.Contains("CodexReasoningEffortOptions", html);
      Assert.Contains("ai-prompt-edit.js", html);
   }

   [Fact]
   public async Task EditPageScriptTogglesReasoningBySelectedProvider()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var scriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/ai-prompt-edit.js"
      );
      var script = await File.ReadAllTextAsync(scriptPath);

      Assert.Contains("selectedOption?.dataset.providerKind", script);
      Assert.Contains("field.hidden = !isCodex", script);
      Assert.Contains("reasoningSelect.disabled = !isCodex", script);
   }

   [Fact]
   public async Task ConfigLayoutRendersPageScriptsSection()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var layoutPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Config/_Layout.cshtml"
      );
      var layout = await File.ReadAllTextAsync(layoutPath);

      Assert.Contains(
         "RenderSectionAsync(\"Scripts\", required: false)",
         layout
      );
   }
}
