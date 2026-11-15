using SESport.Core.Domain;

namespace SESport.Data.Members;

internal static class MemberWatchActivityMatcher
{
   public static string GetActivityMatchesCteSql(
      bool filterToMember = false
   )
   {
      var memberFilterSql = filterToMember
         ? "and watch.member_id = @member_id"
         : string.Empty;

      return $$"""
         watch_activity_matches as (
            select watch.member_id, watch.entity_id, link.activity_id
            from member_entity_watches watch
            join entities watched_entity
               on watched_entity.id = watch.entity_id
            join activity_entity_links link
               on link.entity_id = watch.entity_id
               and link.is_active
            where watched_entity.entity_type_id =
               '{{TrackedEntityTypeIds.Person}}'
               {{memberFilterSql}}
            union
            select watch.member_id, watch.entity_id, activity.id
            from member_entity_watches watch
            join entities watched_entity
               on watched_entity.id = watch.entity_id
            join activities activity
               on activity.organization_entity_id = watch.entity_id
            where watched_entity.entity_type_id in (
               '{{TrackedEntityTypeIds.Team}}',
               '{{TrackedEntityTypeIds.NationalTeam}}'
            )
               {{memberFilterSql}}
            union
            select watch.member_id, watch.entity_id, link.activity_id
            from member_entity_watches watch
            join entities watched_entity
               on watched_entity.id = watch.entity_id
            join activity_entity_links link
               on link.organization_entity_id = watch.entity_id
               and link.is_active
            where watched_entity.entity_type_id in (
               '{{TrackedEntityTypeIds.Team}}',
               '{{TrackedEntityTypeIds.NationalTeam}}'
            )
               {{memberFilterSql}}
            union
            select watch.member_id, watch.entity_id, link.activity_id
            from member_entity_watches watch
            join entities watched_entity
               on watched_entity.id = watch.entity_id
            join activity_entity_links link
               on link.represented_entity_id = watch.entity_id
               and link.is_active
            where watched_entity.entity_type_id in (
               '{{TrackedEntityTypeIds.Team}}',
               '{{TrackedEntityTypeIds.NationalTeam}}'
            )
               {{memberFilterSql}}
         )
         """;
   }
}
