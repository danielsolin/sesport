using SESport.Web.Pages.Admin.Broadcasts;

namespace SESport.Core.Tests.Pages.Admin.Broadcasts;

public sealed class BroadcastParticipationRunsViewModelTests
{
   [Fact]
   public void PendingViewModelExposesPendingStatusForServerMarkup()
   {
      var model = new BroadcastParticipationRunsViewModel(
         Guid.NewGuid(),
         null,
         null,
         null,
         null,
         null,
         string.Empty,
         [],
         false,
         true,
         false
      );

      Assert.Equal("pending", model.ParticipationStatusId);
      Assert.False(model.IsFinal);
   }
}
