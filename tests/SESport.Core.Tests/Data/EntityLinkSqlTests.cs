using SESport.Data;

namespace SESport.Core.Tests.Data;

public sealed class EntityLinkSqlTests
{
   [Fact]
   public void GetOtherSideEntityIdSqlUsesSourceAndTargetColumns()
   {
      var sql = EntityLinkSql.GetOtherSideEntityIdSql("e.id");

      Assert.Contains("when source_entity_id = e.id", sql);
      Assert.Contains("then target_entity_id", sql);
      Assert.Contains("else source_entity_id", sql);
   }

   [Fact]
   public void GetLinkedOrganizationNamesLateralSqlUsesNonOrganizationTypes()
   {
      var sql = EntityLinkSql.GetLinkedOrganizationNamesLateralSql("e");

      Assert.Contains("left join lateral", sql);
      Assert.Contains("organization_names", sql);
      Assert.Contains("entity.id =", sql);
      Assert.Contains("not in", sql);
      Assert.Contains("'Person', 'Pair'", sql);
   }
}
