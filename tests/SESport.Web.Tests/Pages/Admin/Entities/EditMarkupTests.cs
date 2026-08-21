namespace SESport.Core.Tests.Pages.Admin.Entities;

public sealed class EditMarkupTests
{
   [Fact]
   public async Task EditPageUsesLinkedEntitySubgridPicker()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Entities/Edit.cshtml"
      );
      var scriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/Admin/js/entity-linked-entities.js"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);

      Assert.Contains("data-entity-linked-entities-picker", html);
      Assert.Contains("data-entity-linked-entities-search-url", html);
      Assert.Contains("data-entity-linked-entities-update-url", html);
      Assert.Contains("data-entity-linked-entities-grid", html);
      Assert.Contains("data-entity-linked-entities-rows", html);
      Assert.Contains("data-entity-linked-entities-suggestions", html);
      Assert.Contains("data-entity-linked-entities-remove", html);
      Assert.Contains("entity-linked-entities-table", html);
      Assert.Contains("Entity.LinkedEntityIds", html);
      Assert.Contains("data-organization-only=\"false\"", html);
      Assert.Contains("data-person-birthdate-field", html);
      Assert.Contains("Entity.Birthdate", html);
      Assert.Contains("data-person-formative-club-field", html);
      Assert.Contains("Entity.FormativeClub", html);
      Assert.Contains("asp-page-handler=\"AddSource\"", html);
      Assert.Contains("id=\"add-entity-source-form\"", html);
      Assert.Contains("name=\"sourceUrl\"", html);
      Assert.Contains("type=\"url\"", html);
      Assert.Contains("Add source", html);
      Assert.DoesNotContain("asp-items=\"Model.EntityLinkOptions\"", html);
      Assert.DoesNotContain("data-multi-select", html);
      Assert.DoesNotContain("data-entity-linked-entities-chip", html);
      Assert.Contains(
         "src=\"~/Admin/js/entity-linked-entities.js\"",
         html
      );
      Assert.Contains("initializeEntityLinkedEntitiesPicker", script);
      Assert.Contains("data-entity-linked-entities-grid", script);
      Assert.Contains("data-entity-linked-entities-rows", script);
      Assert.Contains("data-entity-linked-entities-row", script);
      Assert.Contains("data-entity-linked-entities-remove", script);
      Assert.DoesNotContain("event.key === \"Backspace\"", script);
      Assert.DoesNotContain("chip", script);
      Assert.Contains("broadcast-org-entity-option", script);
   }
}
