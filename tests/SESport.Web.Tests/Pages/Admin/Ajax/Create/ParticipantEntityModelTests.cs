using SESport.Data.Models;
using SESport.Web.Pages.Admin.Ajax.Create;

namespace SESport.Core.Tests.Pages.Admin.Ajax.Create;

public sealed class ParticipantEntityModelTests
{
   [Fact]
   public void ClearPersonalDataClearsFieldsCopiedFromTemplate()
   {
      var entity = new EntityEditModel
      {
         AliasName = "Source alias",
         Birthdate = new DateOnly(2000, 1, 2),
         Height = 180,
         Weight = 75,
         FormativeClub = "Source club"
      };

      ParticipantEntityModel.ClearPersonalData(entity);

      Assert.Null(entity.AliasName);
      Assert.Null(entity.Birthdate);
      Assert.Null(entity.Height);
      Assert.Null(entity.Weight);
      Assert.Null(entity.FormativeClub);
   }
}
