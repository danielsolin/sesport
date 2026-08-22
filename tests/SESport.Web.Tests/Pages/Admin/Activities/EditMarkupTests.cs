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
         "src/SESport.Web/wwwroot/Admin/js/activity-participants.js"
      );
      var partialPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Activities/_ActivityParticipantSelection.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);
      var partial = await File.ReadAllTextAsync(partialPath);

      Assert.Contains("data-activity-participant-selection", partial);
      Assert.Contains("data-activity-participant-remove", partial);
      Assert.Contains("data-activity-participant-remove", script);
      Assert.Contains("participant-selection", script);
      Assert.Contains("replaceElementWithPartialHtml", script);
      Assert.DoesNotContain("createElement", script);
      Assert.DoesNotContain("innerHTML", script);
      Assert.Contains("Delete", partial);
      Assert.Contains("SetParticipantActive", html);
      Assert.Contains("Deactivate", partial);
      Assert.Contains("Reactivate", partial);
   }

   [Fact]
   public async Task EditPageShowsOrganizationAndGroupSelectors()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Activities/Edit.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var suggestions = await File.ReadAllTextAsync(
         Path.Combine(
            repoRoot,
            "src/SESport.Web/Pages/Admin/Ajax/Search/"
               + "_BroadcastActivityGroupSuggestions.cshtml"
         )
      );

      Assert.Contains("<span>Organization</span>", html);
      Assert.DoesNotContain("Related organization", html);
      Assert.Contains(
         "asp-for=\"Activity.OrganizationEntityId\"",
         html
      );
      Assert.Contains("<span>Group</span>", html);
      Assert.Contains("asp-for=\"Activity.ActivityGroupId\"", html);
      Assert.Contains("data-activity-group-picker", html);
      Assert.Contains("data-activity-group-search-url", html);
      Assert.Contains("data-activity-group-suggestions", html);
      Assert.Contains(
         "src=\"~/Admin/js/activity-group-autocomplete.js\"",
         html
      );
      Assert.Contains(
         "src=\"~/Admin/js/activity-participants.js\"",
         html
      );
      Assert.Contains("Create new group", suggestions);
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

   [Fact]
   public async Task EditPageOffersActivityAiJobRunner()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Activities/Edit.cshtml"
      );
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/site.css"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var css = await File.ReadAllTextAsync(cssPath);
      var model = new SESport.Web.Pages.Admin.Activities.EditModel(
         null!,
         null!,
         null!,
         null!
      );

      Assert.Equal(
         [
            AiJobIds.FindActivityGroupFacts,
            AiJobIds.FindParticipantsResult,
            AiJobIds.FindParticipantsStart
         ],
         model.ActivityAiJobOptions.Select(option => option.Value)
      );
      Assert.Equal(
         AiJobIds.FindParticipantsStart,
         model.SelectedAiJobId
      );
      Assert.Contains("class=\"activity-ai-job-form\"", html);
      Assert.Contains("asp-page-handler=\"RunAiJob\"", html);
      Assert.Contains(
         "asp-route-id=\"@Model.Activity.Id\"",
         html
      );
      Assert.Contains("asp-for=\"SelectedAiJobId\"", html);
      Assert.Contains(
         "asp-items=\"Model.ActivityAiJobOptions\"",
         html
      );
      Assert.Contains("aria-label=\"AI job\"", html);
      Assert.Contains(">\n            Run\n", html);
      Assert.Contains("OnPostRunAiJobAsync", await File.ReadAllTextAsync(
         Path.Combine(
            repoRoot,
            "src/SESport.Web/Pages/Admin/Activities/Edit.cshtml.cs"
         )
      ));
      Assert.Contains(".activity-ai-job-form", css);
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
         "src/SESport.Web/wwwroot/Admin/js/site.js"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);

      Assert.Contains("data-activity-facts-grid", html);
      Assert.DoesNotContain(
         "These facts belong to the ActivityGroup",
         html
      );
      Assert.Contains("@foreach(var fact in Model.Facts)", html);
      Assert.Contains("<th>Date</th>", html);
      Assert.Contains("<th>Fact</th>", html);
      Assert.Contains("<th>Source</th>", html);
      Assert.Contains("activity-facts-date-column", html);
      Assert.Contains("activity-facts-actions-column", html);
      Assert.Contains(
         "DateDisplay.DateOnlyFormat",
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

   [Fact]
   public async Task EditPageShowsOtherGroupDescriptions()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Activities/Edit.cshtml"
      );
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/site.css"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var css = await File.ReadAllTextAsync(cssPath);

      Assert.Contains(
         "if(Model.OtherGroupDescriptions.Count > 0)",
         html
      );
      Assert.Contains(
         "class=\"activity-group-description-options\"",
         html
      );
      Assert.Contains(
         "@string.Join(\", \", Model.OtherGroupDescriptions)",
         html
      );
      Assert.DoesNotContain(
         "Other descriptions in this ActivityGroup",
         html
      );
      Assert.Contains(".activity-group-description-options", css);
      Assert.Contains("color: var(--muted)", css);
      Assert.Contains("font-size: 11px", css);
   }

   [Fact]
   public async Task EditPageShowsAiResultsSection()
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
         "src/SESport.Web/wwwroot/Admin/js/site.js"
      );
      var endpointPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Ajax/Update/" +
         "ActivityParticipantAiResultValue.cshtml.cs"
      );
      var cssPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/wwwroot/css/site.css"
      );
      var html = await File.ReadAllTextAsync(htmlPath);
      var script = await File.ReadAllTextAsync(scriptPath);
      var endpoint = await File.ReadAllTextAsync(endpointPath);
      var css = await File.ReadAllTextAsync(cssPath);

      Assert.Contains("data-activity-ai-results", html);
      Assert.Contains("AI results", html);
      Assert.Contains("data-activity-ai-result-set", html);
      Assert.Contains("data-activity-ai-result-job-id", html);
      Assert.Contains("data-activity-ai-result-run-id", html);
      Assert.Contains("Run details", html);
      Assert.Contains("activity-ai-result-table", html);
      Assert.Contains(
         "activity-ai-result-value-column",
         html
      );
      Assert.Contains("activity-ai-result-summary-meta", html);
      Assert.Contains(
         "data-ai-result-edit-url",
         html
      );
      Assert.Contains(
         "data-ai-result-edit-field=",
         html
      );
      Assert.Contains("data-ai-result-value-id=", html);
      Assert.Contains(
         "data-ai-result-edit-display",
         html
      );
      Assert.Contains("data-ai-result-placeholder", html);
      Assert.Contains("valuePlaceholder", html);
      Assert.Contains(
         "data-ai-result-edit-input",
         html
      );
      Assert.Contains(
         "initializeActivityAiResultInlineEditing",
         script
      );
      Assert.Contains(
         "postActivityAiResultInlineEditAsync",
         script
      );
      Assert.Contains(
         "openActivityAiResultInlineEditCell",
         script
      );
      Assert.Contains("UpdateValueAsync", endpoint);
      Assert.Contains(
         ".activity-ai-result-inline-edit-input {\n" +
         "   width: 80px;\n" +
         "   max-width: 100%;",
         css
      );
      Assert.Contains(
         ".activity-ai-result-table {\n" +
         "   table-layout: fixed;",
         css
      );
      Assert.Contains(
         ".activity-ai-result-field-column {\n" +
         "   width: 30%;",
         css
      );
      Assert.Contains(
         ".activity-ai-result-value-column {\n" +
         "   width: 36%;",
         css
      );
      Assert.DoesNotContain("Checked sources", html);
      Assert.DoesNotContain("<th>Sources</th>", html);
      Assert.DoesNotContain("Raw JSON", html);
      Assert.DoesNotContain("activity-ai-result-source-list", html);
   }
}
