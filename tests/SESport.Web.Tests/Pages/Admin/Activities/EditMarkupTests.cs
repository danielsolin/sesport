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
   }
}
