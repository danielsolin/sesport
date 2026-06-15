using SESport.AI.Models;
using SESport.AI.Rendering;
using SESport.Web.Pages.Admin.Config.Ai.Runs;

namespace SESport.Core.Tests.Pages.Admin.Config.Ai.Runs;

public class DetailsModelTests
{
   [Fact]
   public void BuildSystemPromptTextAppendsToolsDescriptionForLlamaServer()
   {
      var text = DetailsModel.BuildSystemPromptText(
         "System prompt",
         "llama-server",
         true,
         "Use web search."
      );

      Assert.Equal(
         "System prompt" + Environment.NewLine + Environment.NewLine +
         "Use web search.",
         text
      );
   }

   [Fact]
   public void BuildRenderedPromptTextReturnsOnlyUserContent()
   {
      var renderer = new TemplatePromptRenderer();
      var prompt = new AiPromptDefinition(
         Guid.Parse("11111111-1111-1111-1111-111111111111"),
         "job",
         1,
         "System prompt",
         "User: {{input.title}}",
         null,
         "{}",
         null,
         null,
         null,
         true
      );

      var rendered = renderer.Render(
         prompt,
         """{"input":{"title":"Tre Kronor"}}"""
      );

      Assert.Equal(
         "User: Tre Kronor",
         DetailsModel.BuildRenderedPromptText(rendered)
      );
   }
}
