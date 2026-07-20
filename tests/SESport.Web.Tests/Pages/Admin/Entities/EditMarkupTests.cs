namespace SESport.Core.Tests.Pages.Admin.Entities;

public sealed class EditMarkupTests
{
   [Fact]
   public async Task EditPageUsesRemoteLinkedEntityPicker()
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
         "src/SESport.Web/wwwroot/js/entity-linked-entities.js"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);

      Assert.Contains("data-entity-linked-entities-picker", html);
      Assert.Contains("data-entity-linked-entities-search-url", html);
      Assert.Contains("data-entity-linked-entities-update-url", html);
      Assert.Contains("data-entity-linked-entities-selected", html);
      Assert.Contains("data-entity-linked-entities-suggestions", html);
      Assert.Contains("data-entity-linked-entities-chip-remove", html);
      Assert.Contains("Entity.LinkedEntityIds", html);
      Assert.Contains("data-organization-only=\"false\"", html);
      Assert.Contains("data-person-birthdate-field", html);
      Assert.Contains("Entity.Birthdate", html);
      Assert.DoesNotContain("asp-items=\"Model.EntityLinkOptions\"", html);
      Assert.DoesNotContain("data-multi-select", html);
      Assert.Contains("entity-linked-entities.js", html);
      Assert.Contains("initializeEntityLinkedEntitiesPicker", script);
      Assert.Contains("data-entity-linked-entities-chip", script);
      Assert.Contains("data-entity-linked-entities-chip-remove", script);
      Assert.Contains("broadcast-org-entity-option", script);
   }
}
