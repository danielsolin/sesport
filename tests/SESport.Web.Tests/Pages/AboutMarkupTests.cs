namespace SESport.Core.Tests.Pages;

public sealed class AboutMarkupTests
{
   [Fact]
   public async Task AboutPageContainsLetterContent()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var page = await File.ReadAllTextAsync(
         Path.Combine(repoRoot, "src/SESport.Web/Pages/Om.cshtml")
      );
      var publicCss = await File.ReadAllTextAsync(
         Path.Combine(repoRoot, "src/SESport.Web/wwwroot/css/public.css")
      );

      Assert.Contains("@page \"/om\"", page);
      Assert.Contains("<article class=\"about-letter\">", page);
      Assert.Contains("Hej!", page);
      Assert.Contains(
         "Jag heter Daniel och har byggt sesport för att jag ville kunna se",
         page
      );
      Assert.Contains(
         "Du behöver inget konto för att använda sesport.",
         page
      );
      Assert.Contains(
         "Det finns ingen redaktion bakom sesport.",
         page
      );
      Assert.Contains(
         "Frågor, synpunkter eller annat?",
         page
      );
      Assert.Contains(
         "href=\"mailto:info@sesport.se\"",
         page
      );
      Assert.Contains("Maila mig på:", page);
      Assert.Contains("//D", page);
      Assert.Contains(".about-page {", publicCss);
      Assert.Contains(".about-letter {", publicCss);
   }
}
