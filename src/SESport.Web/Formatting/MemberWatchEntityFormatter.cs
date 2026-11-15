using SESport.Core.Domain;

namespace SESport.Web.Formatting;

public static class MemberWatchEntityFormatter
{
   public static string GetTypeLabel(string entityTypeId)
   {
      return entityTypeId switch
      {
         TrackedEntityTypeIds.Person => "Person",
         TrackedEntityTypeIds.Team => "Lag",
         TrackedEntityTypeIds.NationalTeam => "Landslag",
         _ => "Entitet"
      };
   }
}
