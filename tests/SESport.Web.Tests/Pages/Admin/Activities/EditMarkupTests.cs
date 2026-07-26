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
   public async Task EditPageCanAddSourceFromUrl()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Activities/Edit.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains("asp-page-handler=\"AddSource\"", html);
      Assert.Contains("id=\"add-activity-source-form\"", html);
      Assert.Contains("form=\"add-activity-source-form\"", html);
      Assert.Contains("name=\"sourceUrl\"", html);
      Assert.Contains("type=\"url\"", html);
      Assert.Contains("required", html);
      Assert.Contains("Add source", html);
   }

   [Theory]
   [InlineData("https://example.com/source", true)]
   [InlineData(" http://example.com/source ", true)]
   [InlineData("ftp://example.com/source", false)]
   [InlineData("not a url", false)]
   [InlineData("", false)]
   public void SourceUrlRequiresAbsoluteHttpUrl(
      string sourceUrl,
      bool expected
   )
   {
      var isValid = SESport.Web.Pages.Admin.Activities.EditModel
         .TryNormalizeSourceUrl(sourceUrl, out _);

      Assert.Equal(expected, isValid);
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
      Assert.Contains("activity-facts-actions-column", html);
      Assert.Contains(
         "fact.CreatedAt.ToString(\"yyyy-MM-dd\")",
         html
      );
      Assert.Contains("<td>@fact.Text</td>", html);
      Assert.Contains("activity-facts-table", html);
      Assert.Contains("@sourceUrl", html);
      Assert.Contains("target=\"_blank\"", html);
      Assert.Contains("asp-page-handler=\"DeleteFact\"", html);
      Assert.Contains("asp-route-factId=\"@fact.Id\"", html);
      Assert.Contains(
         "onsubmit=\"return confirm('Are you sure?');\"",
         html
      );
      Assert.DoesNotContain("data-find-facts", html);
      Assert.DoesNotContain("Find Facts", html);
      Assert.DoesNotContain("data-facts-output", html);
      Assert.Contains("async function findFactsAsync(button)", script);
      Assert.Contains("if(!form || !url)", script);
   }
}
