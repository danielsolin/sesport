using SESport.Web.Pages.Admin.Runs;
using SESport.Web.Services;

namespace SESport.Core.Tests.Pages.Admin.Runs;

public sealed class IndexModelTests
{
   [Fact]
   public void GetDetailsRouteValuesIncludesRunIdAndFilters()
   {
      var model = CreateModel();
      model.Date = new DateOnly(2026, 6, 26);
      model.JobId = "decide-swedish-participation";
      model.StatusIds = ["running", "pending"];
      var runId = Guid.Parse("11111111-2222-3333-4444-555555555555");

      var routeValues = model.GetDetailsRouteValues(runId);

      Assert.Equal("2026-06-26", routeValues["date"]);
      Assert.Equal("decide-swedish-participation", routeValues["jobId"]);
      Assert.Equal(runId.ToString(), routeValues["id"]);
      Assert.Equal("running", routeValues["status[0]"]);
      Assert.Equal("pending", routeValues["status[1]"]);
   }

   [Fact]
   public void GetDeleteRouteValuesIncludesRunIdAndFilters()
   {
      var model = CreateModel();
      model.Date = new DateOnly(2026, 6, 26);
      model.JobId = "decide-swedish-participation";
      model.StatusIds = ["running", "pending"];
      var runId = Guid.Parse("11111111-2222-3333-4444-555555555555");

      var routeValues = model.GetDeleteRouteValues(runId);

      Assert.Equal("2026-06-26", routeValues["date"]);
      Assert.Equal("decide-swedish-participation", routeValues["jobId"]);
      Assert.Equal(runId.ToString(), routeValues["id"]);
      Assert.Equal("running", routeValues["status[0]"]);
      Assert.Equal("pending", routeValues["status[1]"]);
   }

   [Theory]
   [InlineData(
      "generate-activity-teaser",
      "A 2026-07-12"
   )]
   [InlineData(
      "decide-swedish-participation",
      "B 2026-07-12"
   )]
   public void FormatRunTargetLabelIncludesDate(
      string jobId,
      string expected
   )
   {
      var run = new AiRunListItem(
         Guid.NewGuid(),
         jobId,
         null,
         "Job label",
         "Event",
         new DateOnly(2026, 7, 12),
         "Provider",
         null,
         "completed",
         0,
         0,
         DateTimeOffset.UtcNow,
         null
      );

      Assert.Equal(expected, IndexModel.FormatRunTargetLabel(run));
   }

   private static IndexModel CreateModel()
   {
      return new IndexModel(null!, null!, new RunDatePreferenceStore());
   }
}
