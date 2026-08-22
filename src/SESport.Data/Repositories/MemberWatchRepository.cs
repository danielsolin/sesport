using Npgsql;

using SESport.Core.Domain;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class MemberWatchRepository(NpgsqlDataSource dataSource)
{
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
         includeNextActivity: true,
         includeSearchRanking: false
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
      var searchTerms = query
         .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
         .Select(term => term
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
         )
         .ToArray();
      if(searchTerms.Length == 0)
      {
         return Array.Empty<MemberPersonListItem>();
      }

      var sql = BuildPersonListSql(
         string.Empty,
         """
         and not exists (
            select 1
            from unnest(@search_terms::text[]) as search_term(term)
            where not (
               e.canonical_name ilike
                  ('%' || search_term.term || '%') escape '\'
               or coalesce(e.alias_name, '') ilike
                  ('%' || search_term.term || '%') escape '\'
               or coalesce(s.display_name, s.name) ilike
                  ('%' || search_term.term || '%') escape '\'
               or coalesce(context.related_search_names, '') ilike
                  ('%' || search_term.term || '%') escape '\'
            )
         )
         """,
         includeLimit: true,
         includeNextActivity: false,
         includeSearchRanking: true
      );

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("search_terms", searchTerms);
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

   public async Task<MemberPrimaryImage?>
      GetPersonPrimaryImageAsync(
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      var sql = $$"""
         select
            coalesce(image.thumbnail_data, image.image_data),
            coalesce(image.thumbnail_mime_type, image.mime_type)
         from entity_images image
         join entities person
            on person.id = image.entity_id
         where person.entity_type_id =
            '{{TrackedEntityTypeIds.Person}}'
            and image.entity_id = @entity_id
            and image.review_status =
               '{{EntityImageReviewStatusIds.Approved}}'
            and image.is_primary
            and coalesce(
               image.thumbnail_data,
               image.image_data
            ) is not null
            and coalesce(
               image.thumbnail_mime_type,
               image.mime_type
            ) is not null
            and coalesce(
               image.thumbnail_mime_type,
               image.mime_type
            ) ilike 'image/%'
         limit 1
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("entity_id", entityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return new MemberPrimaryImage(
         reader.GetFieldValue<byte[]>(0),
         reader.GetString(1)
      );
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
      var isWatched = reader.GetBoolean(4);
      var hasPrimaryImage = reader.GetBoolean(5);
      MemberPrimaryImageSource? primaryImageSource = null;
      if(hasPrimaryImage)
      {
         primaryImageSource = new MemberPrimaryImageSource(
            reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9)
         );
      }

      if(includesNextActivity && !reader.IsDBNull(10))
      {
         nextActivity = new MemberNextActivity(
            reader.GetFieldValue<DateTimeOffset>(10),
            reader.GetString(11),
            reader.IsDBNull(12) ? null : reader.GetString(12)
         );
      }

      return new MemberPersonListItem(
         reader.GetGuid(0),
         reader.GetString(1),
         reader.GetString(2),
         reader.GetString(3),
         nextActivity,
         hasPrimaryImage,
         primaryImageSource,
         isWatched
      );
   }

   private static string BuildPersonListSql(
      string additionalFromSql,
      string additionalWhereSql,
      bool includeLimit,
      bool includeNextActivity,
      bool includeSearchRanking
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
      var orderBySql = includeSearchRanking
         ? """
            order by
               case when e.canonical_name ilike (
                  (@search_terms::text[])[1] || '%'
               ) escape '\'
                  or coalesce(e.alias_name, '') ilike (
                     (@search_terms::text[])[1] || '%'
                  ) escape '\'
                  then 0
                  else 1
               end,
               e.canonical_name,
               e.id
            """
         : """
            order by e.canonical_name, e.id
            """;

      return $$"""
         select
            e.id,
            e.canonical_name,
            coalesce(s.display_name, s.name) as sport_name,
            coalesce(context.related_names, '') as related_names,
            exists (
               select 1
               from member_entity_watches watch_status
               where watch_status.member_id = @member_id
                  and watch_status.entity_id = e.id
            ) as is_watched,
            primary_image.source_url is not null as has_primary_image,
            primary_image.source_url,
            primary_image.creator_name,
            primary_image.license_name,
            primary_image.license_url
            {{nextActivitySelect}}
         from entities e
         join sports s on s.id = e.sport_id
         left join lateral (
            select
               image.source_url,
               image.creator_name,
               image.license_name,
               image.license_url
            from entity_images image
            where image.entity_id = e.id
               and image.review_status =
                  '{{EntityImageReviewStatusIds.Approved}}'
               and image.is_primary
               and image.image_data is not null
               and image.mime_type is not null
               and image.mime_type ilike 'image/%'
            limit 1
         ) primary_image on true
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
                        when '{{TrackedEntityTypeIds.NationalTeam}}' then 3
                        when '{{TrackedEntityTypeIds.Club}}' then 4
                        else 5
                  end,
                  linked.canonical_name
            ) as related_names,
            string_agg(
               concat_ws(
                  ' ',
                  nullif(btrim(linked.alias_name), ''),
                  linked.canonical_name
               ),
               ', ' order by
                     case linked.entity_type_id
                        when '{{TrackedEntityTypeIds.Discipline}}' then 1
                        when '{{TrackedEntityTypeIds.Team}}' then 2
                        when '{{TrackedEntityTypeIds.NationalTeam}}' then 3
                        when '{{TrackedEntityTypeIds.Club}}' then 4
                        else 5
                  end,
                  linked.canonical_name
            ) as related_search_names
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
                  '{{TrackedEntityTypeIds.NationalTeam}}',
                  '{{TrackedEntityTypeIds.Club}}'
               )
         ) context on true
         {{nextActivityJoin}}
         {{additionalFromSql}}
         where e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
            {{additionalWhereSql}}
         {{orderBySql}}
         {{limitSql}}
         """;
   }

   private static void AddNextActivityParameters(
      NpgsqlCommand command,
      DateTimeOffset now
   )
   {
      command.Parameters.AddWithValue("now", now);
      PublicActivityQuerySupport.AddExclusionParameters(command);
   }
}
