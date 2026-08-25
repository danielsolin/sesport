namespace SESport.Core.Tests.Pages.Admin.Entities;

public sealed class EntityRowsViewModelTests
{
   [Fact]
   public void ResolvedSearchUrlBaseFallsBackToGoogle()
   {
      var viewModel = new SESport.Web.Pages.Admin.Entities.EntityRowsViewModel(
         [],
         string.Empty,
         string.Empty,
         [],
         "female",
         "male"
      );

      Assert.Equal(
         "https://www.google.com/search?q=",
         viewModel.ResolvedSearchUrlBase
      );
   }
}
