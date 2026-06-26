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

      Assert.Contains("index-participants-pill", html);
      Assert.Contains("TotalParticipantsCount", html);
      Assert.Contains("Deltagare:", html);
   }
}
