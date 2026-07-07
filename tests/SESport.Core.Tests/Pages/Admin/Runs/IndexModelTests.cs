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

   private static IndexModel CreateModel()
   {
      return new IndexModel(null!, null!, new RunDatePreferenceStore());
   }
}
