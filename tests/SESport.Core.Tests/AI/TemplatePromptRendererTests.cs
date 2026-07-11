using SESport.Core.AI;
using SESport.Core.Domain;
using SESport.AI.Prompts;

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

   [Fact]
   public void RenderReplacesCandidatesMarker()
   {
      var renderer = new TemplatePromptRenderer();
      var prompt = new AiPromptDefinition(
         Guid.Parse("22222222-2222-2222-2222-222222222222"),
         "job",
         1,
         "System",
         "Possible participants:\n{{candidates}}",
         null,
         "{}",
         null,
         null,
         null,
         true
      );

      var rendered = renderer.Render(
         prompt,
         """
         {"candidates":"- Jenny Rissveds"}
         """
      );

      Assert.Equal(
         "Possible participants:\n- Jenny Rissveds",
         rendered.UserPrompt
      );
   }

   [Fact]
   public void RenderReplacesDescriptionMarker()
   {
      var renderer = new TemplatePromptRenderer();
      var prompt = new AiPromptDefinition(
         Guid.Parse("33333333-3333-3333-3333-333333333333"),
         "job",
         1,
         "System",
         "Description: {{description}}",
         null,
         "{}",
         null,
         null,
         null,
         true
      );

      var rendered = renderer.Render(
         prompt,
         """{"description":"Olympic qualifier"}"""
      );

      Assert.Equal(
         "Description: Olympic qualifier",
         rendered.UserPrompt
      );
   }

   [Fact]
   public void RenderReplacesPrimaryCountryMarkersInSystemPrompt()
   {
      var renderer = new TemplatePromptRenderer();
      var prompt = new AiPromptDefinition(
         Guid.Parse("44444444-4444-4444-4444-444444444444"),
         "job",
         1,
         "Find {{LanguageName}} participants from {{CountryName}}.",
         "Use code {{ThreeLetterCode}}.",
         null,
         "{}",
         null,
         null,
         null,
         true
      );

      var rendered = renderer.Render(prompt, "{}");

      Assert.Equal(
         $"Find {PrimaryCountry.LanguageName} participants from " +
         $"{PrimaryCountry.CountryName}.",
         rendered.SystemPrompt
      );
      Assert.Equal(
         $"Use code {PrimaryCountry.ThreeLetterCode}.",
         rendered.UserPrompt
      );
   }

   [Fact]
   public void RenderReplacesNamespacedPrimaryCountryMarkers()
   {
      var renderer = new TemplatePromptRenderer();
      var prompt = new AiPromptDefinition(
         Guid.Parse("55555555-5555-5555-5555-555555555555"),
         "job",
         1,
         "Country: {{PrimaryCountry.CountryName}}",
         "Local: {{PrimaryCountry.LocalDisplayName}}, " +
         "{{PrimaryCountry.TwoLetterCode}}",
         null,
         "{}",
         null,
         null,
         null,
         true
      );

      var rendered = renderer.Render(prompt, "{}");

      Assert.Equal(
         $"Country: {PrimaryCountry.CountryName}",
         rendered.SystemPrompt
      );
      Assert.Equal(
         $"Local: {PrimaryCountry.LocalDisplayName}, " +
         PrimaryCountry.TwoLetterCode,
         rendered.UserPrompt
      );
   }
}
