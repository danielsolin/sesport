using SESport.Core.Broadcast;

namespace SESport.Data;

public static class EntityLinkSql
{
   public static string GetOtherSideEntityIdSql(string entityIdSql)
   {
      return $"""
         case
            when source_entity_id = {entityIdSql}
               then target_entity_id
            else source_entity_id
         end
         """;
   }

   public static string GetLinkedOrganizationNamesLateralSql(
      string entityAlias
   )
   {
      var entityIdSql = $"{entityAlias}.id";

      return $"""
         left join lateral (
            select string_agg(
               distinct organization_name,
               ', ' order by organization_name
            ) as organization_names
            from (
               select distinct
                  coalesce(entity.alias_name,
                     entity.canonical_name) as organization_name
               from entity_to_entity_links l
               join entities entity
                  on entity.id =
                     {GetOtherSideEntityIdSql(entityIdSql)}
               where (l.source_entity_id = {entityIdSql}
                     or l.target_entity_id = {entityIdSql})
                  and {BroadcastEntityFilter
                     .GetNonOrganizationEntityTypePredicateSql(
                        "entity.entity_type_id"
                     )}
            ) organizations
         ) org on true
         """;
   }
}
