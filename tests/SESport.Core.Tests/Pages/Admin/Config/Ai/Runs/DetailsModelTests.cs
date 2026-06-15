using SESport.AI.Rendering;
using SESport.Web.Pages.Admin.Config.Ai.Runs;

namespace SESport.Core.Tests.Pages.Admin.Config.Ai.Runs;

public sealed class DetailsModelTests
{
   [Fact]
   public void BuildRenderedPromptTextReturnsOnlyUserContent()
   {
      var renderer = new TemplatePromptRenderer();

      var text = DetailsModel.BuildRenderedPromptText(
         renderer,
         "System prompt",
         "User: {{input.title}}",
         """{"input":{"title":"Tre Kronor"}}"""
      );

      Assert.Equal("User: Tre Kronor", text);
   }
}
