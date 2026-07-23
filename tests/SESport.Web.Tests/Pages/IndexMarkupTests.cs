namespace SESport.Core.Tests.Pages;

public sealed class IndexMarkupTests
{
   [Fact]
   public async Task IndexPageIncludesParticipantsCount()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Index.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains("index-participants-info", html);
      Assert.Contains("aria-label=\"Visa alla sporter\"", html);
      Assert.Contains("TotalParticipantsCount", html);
      Assert.Contains("Svenskar:", html);
      Assert.Contains("SportParticipantCounts", html);
      Assert.Contains(
         "aria-label=\"Visa endast @sport.SportName\"",
         html
      );
      Assert.Contains("index-participants-filter", html);
      Assert.Contains("is-selected", html);
      Assert.Contains("asp-route-sport=\"@sport.SportId\"", html);
   }
}
