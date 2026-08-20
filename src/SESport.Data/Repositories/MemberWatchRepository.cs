using Npgsql;

using SESport.Core.Domain;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class MemberWatchRepository(NpgsqlDataSource dataSource)
{
   private const string TestActivityTitle = "Test Activity";
   private const string TestActivitySlugPattern = "test-activity-%";

   public async Task<IReadOnlyList<MemberPersonListItem>>
      GetWatchedEntitiesAsync(
      Guid memberId,
      DateTimeOffset now,
      CancellationToken cancellationToken
   )
   {
      var sql = BuildPersonListSql(
         """
         join member_entity_watches watch
            on watch.entity_id = e.id
         """,
         """
         and watch.member_id = @member_id
         """,
         includeLimit: false,
         includeNextActivity: true
      );

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("member_id", memberId);
      AddNextActivityParameters(command, now);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var entities = new List<MemberPersonListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         entities.Add(ReadPersonListItem(reader, includesNextActivity: true));
      }

      return entities;
   }

   public async Task<IReadOnlyList<MemberPersonListItem>> SearchPeopleAsync(
      string query,
      Guid memberId,
      int maxResults,
      CancellationToken cancellationToken
   )
   {
      var escapedQuery = query
         .Trim()
         .Replace("\\", "\\\\", StringComparison.Ordinal)
         .Replace("%", "\\%", StringComparison.Ordinal)
         .Replace("_", "\\_", StringComparison.Ordinal);
      var sql = BuildPersonListSql(
         string.Empty,
         """
         and (
            e.canonical_name ilike @term escape '\'
            or coalesce(e.alias_name, '') ilike @term escape '\'
         )
         and not exists (
            select 1
            from member_entity_watches existing_watch
            where existing_watch.member_id = @member_id
               and existing_watch.entity_id = e.id
         )
         """,
         includeLimit: true,
         includeNextActivity: false
      );

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("term", $"%{escapedQuery}%");
      command.Parameters.AddWithValue("member_id", memberId);
      command.Parameters.AddWithValue("max_results", maxResults);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var entities = new List<MemberPersonListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         entities.Add(ReadPersonListItem(reader, includesNextActivity: false));
      }

      return entities;
   }

   public async Task<bool> TryAddEntityWatchAsync(
      Guid memberId,
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = $$"""
         insert into member_entity_watches (
            member_id,
            entity_id
         )
         select
            @member_id,
            e.id
         from entities e
         where e.id = @entity_id
            and e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
         on conflict (member_id, entity_id) do nothing
         returning entity_id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("member_id", memberId);
      command.Parameters.AddWithValue("entity_id", entityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      return await reader.ReadAsync(cancellationToken);
   }

   public async Task<bool> RemoveEntityWatchAsync(
      Guid memberId,
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from member_entity_watches
         where member_id = @member_id
            and entity_id = @entity_id
         returning entity_id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("member_id", memberId);
      command.Parameters.AddWithValue("entity_id", entityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      return await reader.ReadAsync(cancellationToken);
   }

   private static MemberPersonListItem ReadPersonListItem(
      NpgsqlDataReader reader,
      bool includesNextActivity
   )
   {
      MemberNextActivity? nextActivity = null;
      if(includesNextActivity && !reader.IsDBNull(4))
      {
         nextActivity = new MemberNextActivity(
            reader.GetFieldValue<DateTimeOffset>(4),
            reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6)
         );
      }

      return new MemberPersonListItem(
         reader.GetGuid(0),
         reader.GetString(1),
         reader.GetString(2),
         reader.GetString(3),
         nextActivity
      );
   }

   private static string BuildPersonListSql(
      string additionalFromSql,
      string additionalWhereSql,
      bool includeLimit,
      bool includeNextActivity
   )
   {
      var limitSql = includeLimit
         ? "limit @max_results"
         : string.Empty;
      var nextActivitySelect = includeNextActivity
         ? """
            , next_activity.starts_at
            , next_activity.title
            , next_activity.organization_name
         """
         : string.Empty;
      var nextActivityJoin = includeNextActivity
         ? $$"""
         left join lateral (
            select
               activity.starts_at,
               activity.title,
               coalesce(
                  nullif(btrim(organization.alias_name), ''),
                  organization.canonical_name
               ) as organization_name
            from activities activity
            join activity_entity_links activity_link
               on activity_link.activity_id = activity.id
               and activity_link.entity_id = e.id
               and activity_link.is_active
            left join entities organization
               on organization.id =
                  {{ActivityRepository.GetActivityOrganizationEntityIdSql(
                     "activity"
                  )}}
            where activity.publication_status_id =
               '{{ActivityPublicationStatusIds.Published}}'
               and activity.starts_at > @now
               and not (
                  (
                     activity.title = @test_activity_title
                     or coalesce(activity.slug, '') like
                        @test_activity_slug_pattern
                  )
                  and activity.published_at is null
               )
            order by activity.starts_at, activity.id
            limit 1
         ) next_activity on true
         """
         : string.Empty;

      return $$"""
         select
            e.id,
            e.canonical_name,
            coalesce(s.display_name, s.name) as sport_name,
            coalesce(context.related_names, '') as related_names
            {{nextActivitySelect}}
         from entities e
         join sports s on s.id = e.sport_id
         left join lateral (
            select string_agg(
               coalesce(
                  nullif(btrim(linked.alias_name), ''),
                  linked.canonical_name
               ),
               ', ' order by
                  case linked.entity_type_id
                     when '{{TrackedEntityTypeIds.Discipline}}' then 1
                     when '{{TrackedEntityTypeIds.Team}}' then 2
                     when '{{TrackedEntityTypeIds.Club}}' then 3
                     else 4
                  end,
                  linked.canonical_name
            ) as related_names
            from entity_to_entity_links entity_link
            join entities linked on linked.id = case
               when entity_link.source_entity_id = e.id
                  then entity_link.target_entity_id
               else entity_link.source_entity_id
            end
            where (
               entity_link.source_entity_id = e.id
               or entity_link.target_entity_id = e.id
            )
               and linked.entity_type_id in (
                  '{{TrackedEntityTypeIds.Discipline}}',
                  '{{TrackedEntityTypeIds.Team}}',
                  '{{TrackedEntityTypeIds.Club}}'
               )
         ) context on true
         {{nextActivityJoin}}
         {{additionalFromSql}}
         where e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
            {{additionalWhereSql}}
         order by e.canonical_name
         {{limitSql}}
         """;
   }

   private static void AddNextActivityParameters(
      NpgsqlCommand command,
      DateTimeOffset now
   )
   {
      command.Parameters.AddWithValue("now", now);
      command.Parameters.AddWithValue(
         "test_activity_title",
         TestActivityTitle
      );
      command.Parameters.AddWithValue(
         "test_activity_slug_pattern",
         TestActivitySlugPattern
      );
   }
}
