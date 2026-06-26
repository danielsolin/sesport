using SESport.Web.Pages.Admin.Runs;

namespace SESport.Core.Tests.Pages.Admin.Runs;

public sealed class IndexModelTests
{
   [Fact]
   public void GetDetailsRouteValuesIncludesRunIdAndFilters()
   {
      var model = new IndexModel(null!, null!)
      {
         Date = new DateOnly(2026, 6, 26),
         JobId = "decide-swedish-participation",
         StatusIds = ["running", "pending"]
      };
      var runId = Guid.Parse("11111111-2222-3333-4444-555555555555");

      var routeValues = model.GetDetailsRouteValues(runId);

      Assert.Equal("2026-06-26", routeValues["date"]);
      Assert.Equal("decide-swedish-participation", routeValues["jobId"]);
      Assert.Equal(runId.ToString(), routeValues["id"]);
      Assert.Equal("running", routeValues["status[0]"]);
      Assert.Equal("pending", routeValues["status[1]"]);
   }
}
