using SESport.AI.Models;
using SESport.AI.Rendering;

namespace SESport.Core.Tests.AI;

public class TemplatePromptRendererTests
{
   [Fact]
   public void RenderReturnsSeparatedSystemAndUserPrompt()
   {
      var renderer = new TemplatePromptRenderer();
      var prompt = new AiPromptDefinition(
         Guid.Parse("11111111-1111-1111-1111-111111111111"),
         "job",
         1,
         " System prompt ",
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

      Assert.Equal("System prompt", rendered.SystemPrompt);
      Assert.Equal("User: Tre Kronor", rendered.UserPrompt);
      Assert.Equal(
         "System prompt" + Environment.NewLine + Environment.NewLine +
         "User: Tre Kronor",
         rendered.ToPromptText()
      );
   }
}
