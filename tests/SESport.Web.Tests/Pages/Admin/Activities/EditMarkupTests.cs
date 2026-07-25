namespace SESport.Core.Tests.Pages.Admin.Activities;

public sealed class EditMarkupTests
{
   [Fact]
   public async Task EditPageShowsParticipantRemoveButtons()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Activities/Edit.cshtml"
      );
      var scriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/activity-participants.js"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);

      Assert.Contains("data-activity-participant-remove", html);
      Assert.Contains("data-activity-participant-remove", script);
      Assert.Contains("removeParticipantRow", script);
      Assert.Contains("renderEmptyParticipantsNotice", script);
      Assert.Contains("Delete", html);
      Assert.Contains("SetParticipantActive", html);
      Assert.Contains("Deactivate", html);
      Assert.Contains("Reactivate", html);
   }

   [Fact]
   public async Task EditPagePostsUnsavedPrefilledSources()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Activities/Edit.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains("if(source.Id is null)", html);
      Assert.Contains(
         "asp-for=\"Activity.Sources[index].Kind\"",
         html
      );
      Assert.Contains(
         "asp-for=\"Activity.Sources[index].Url\"",
         html
      );
      Assert.Contains(
         "asp-for=\"Activity.Sources[index].Title\"",
         html
      );
      Assert.Contains(
         "asp-for=\"Activity.Sources[index].Excerpt\"",
         html
      );
      Assert.DoesNotContain("<th>Excerpt</th>", html);
      Assert.DoesNotContain(
         "SourceDisplay.FormatExcerpt(source.Excerpt)",
         html
      );
   }

   [Fact]
   public async Task EditPageShowsFactsAsSubgrid()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Activities/Edit.cshtml"
      );
      var scriptPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/js/site.js"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);

      Assert.Contains("data-activity-facts-grid", html);
      Assert.Contains("@foreach(var fact in Model.Facts)", html);
      Assert.Contains("<th>Date</th>", html);
      Assert.Contains("<th>Fact</th>", html);
      Assert.Contains("<th>Source</th>", html);
      Assert.Contains("activity-facts-date-column", html);
      Assert.Contains(
         "fact.CreatedAt.ToString(\"yyyy-MM-dd\")",
         html
      );
      Assert.Contains("<td>@fact.Text</td>", html);
      Assert.Contains("activity-facts-table", html);
      Assert.Contains("@sourceUrl", html);
      Assert.Contains("target=\"_blank\"", html);
      Assert.DoesNotContain("data-find-facts", html);
      Assert.DoesNotContain("Find Facts", html);
      Assert.DoesNotContain("data-facts-output", html);
      Assert.Contains("async function findFactsAsync(button)", script);
      Assert.Contains("if(!form || !url)", script);
   }
}
