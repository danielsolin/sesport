namespace SESport.Core.Tests.Pages.Admin.Config;

public sealed class AiJobsIndexMarkupTests
{
   [Fact]
   public async Task IndexPageShowsQueuePriorityInsteadOfLabelAndOutput()
   {
      var repoRoot = Path.GetFullPath(
         Path.Combine(AppContext.BaseDirectory, "../../../../..")
      );
      var htmlPath = Path.Combine(
         repoRoot,
         "src/SESport.Web/Pages/Admin/Config/Ai/Jobs/Index.cshtml"
      );
      var html = await File.ReadAllTextAsync(htmlPath);

      Assert.Contains("<th>QP</th>", html);
      Assert.Contains("@job.QueuePriority", html);
      Assert.DoesNotContain("<th>Label</th>", html);
      Assert.DoesNotContain("<th>Output</th>", html);
   }
}
