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
      var partialPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Entities/_EntityLinkedEntitiesGrid.cshtml"
      );
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/site.css"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);
      var partial = await File.ReadAllTextAsync(partialPath);
      var css = await File.ReadAllTextAsync(cssPath);

      Assert.Contains("data-entity-linked-entities-picker", html);
      Assert.Contains("data-entity-linked-entities-search-url", html);
      Assert.Contains("data-entity-linked-entities-update-url", html);
      Assert.Contains("data-entity-linked-entities-picker", html);
      Assert.Contains("data-entity-linked-entities-grid", partial);
      Assert.Contains(
         "/Pages/Admin/Entities/_EntityLinkedEntitiesGrid.cshtml",
         html
      );
      Assert.Contains("data-entity-linked-entities-rows", partial);
      Assert.Contains("data-entity-linked-entities-remove", partial);
      Assert.Contains("entity-linked-entities-table", partial);
      Assert.Contains("Entity.LinkedEntityIds", partial);
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
      Assert.Contains("class=\"entity-image-source-control\"", html);
      Assert.Contains("<span>Image source URL</span>", html);
      Assert.Contains("ReplaceImage", html);
      Assert.Contains("Replace\n", html);
      Assert.DoesNotContain("Replace image", html);
      Assert.DoesNotContain("readonly", html);
      Assert.DoesNotContain("Double-click to edit", html);
      Assert.DoesNotContain("asp-items=\"Model.EntityLinkOptions\"", html);
      Assert.DoesNotContain("data-multi-select", html);
      Assert.DoesNotContain("data-entity-linked-entities-chip", html);
      Assert.Contains(
         "src=\"~/Admin/js/entity-linked-entities.js\"",
         html
      );
      Assert.Contains("initializeEntityLinkedEntitiesPicker", script);
      Assert.Contains("data-entity-linked-entities-row", script);
      Assert.Contains("data-entity-linked-entities-remove", script);
      Assert.Contains("linked-entity-suggestions", script);
      Assert.Contains("linked-entity-grid", script);
      Assert.Contains("replaceContentsWithPartialHtml", script);
      Assert.DoesNotContain("createElement", script);
      Assert.DoesNotContain("innerHTML", script);
      Assert.DoesNotContain("event.key === \"Backspace\"", script);
      Assert.DoesNotContain("chip", script);
      Assert.Contains("broadcast-org-entity-option", script);
      Assert.Contains(
         ".entity-image-source-control {\n" +
         "   display: flex;\n" +
         "   align-items: flex-end;",
         css
      );
      Assert.Contains("align-self: flex-start;", css);
      Assert.Contains(
         "[hidden] {\n" +
         "   display: none !important;\n" +
         "}",
         css
      );
   }
}
