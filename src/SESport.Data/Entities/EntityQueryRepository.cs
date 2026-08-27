using Npgsql;

using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Data.Activities;
using SESport.Data.Models;

namespace SESport.Data.Entities;

public sealed class EntityQueryRepository(NpgsqlDataSource dataSource)
{
   public async Task<IReadOnlyList<EntityListItem>> SearchEntitiesAsync(
      string? term,
      CancellationToken cancellationToken,
      bool broadcastOrganizationOnly = false,
      IReadOnlyCollection<string>? entityTypeIds = null,
      Guid? excludeEntityId = null,
      int? maxResults = null,
      DateOnly? activityDate = null,
      bool includeRelatedEntityNames = true,
      IReadOnlyCollection<string>? sportIds = null
   )
   {
      return await QueryEntitiesAsync(
         term,
         true,
         cancellationToken,
         broadcastOrganizationOnly,
         entityTypeIds,
         excludeEntityId,
         maxResults,
         activityDate,
         includeRelatedEntityNames,
         sportIds
      );
   }

   public async Task<IReadOnlyList<EntityListItem>> GetEntitiesAsync(
      CancellationToken cancellationToken,
      bool broadcastOrganizationOnly = false,
      IReadOnlyCollection<string>? entityTypeIds = null,
      Guid? excludeEntityId = null,
      int? maxResults = null,
      DateOnly? activityDate = null,
      IReadOnlyCollection<string>? sportIds = null
   )
   {
      return await QueryEntitiesAsync(
         null,
         false,
         cancellationToken,
         broadcastOrganizationOnly,
         entityTypeIds,
         excludeEntityId,
         maxResults,
         activityDate,
         true,
         sportIds
      );
   }

   private async Task<IReadOnlyList<EntityListItem>> QueryEntitiesAsync(
      string? term,
      bool applyTermFilter,
      CancellationToken cancellationToken,
      bool broadcastOrganizationOnly,
      IReadOnlyCollection<string>? entityTypeIds,
      Guid? excludeEntityId,
      int? maxResults,
      DateOnly? activityDate,
      bool includeRelatedEntityNames,
      IReadOnlyCollection<string>? sportIds
   )
   {
      term = term?.Trim() ?? string.Empty;
      var normalizedEntityTypeIds = entityTypeIds?
         .Where(entityTypeId => !string.IsNullOrWhiteSpace(entityTypeId))
         .Select(entityTypeId => entityTypeId.Trim())
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToArray() ?? [];
      var normalizedSportIds = sportIds?
         .Where(sportId => !string.IsNullOrWhiteSpace(sportId))
         .Select(sportId => sportId.Trim())
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToArray() ?? [];

      if(applyTermFilter && term == string.Empty)
      {
         return [];
      }

      var whereClauses = new List<string>();

      if(applyTermFilter)
      {
         var escapedTerm = term
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

         whereClauses.Add(
            """
            (
               e.canonical_name ilike @term escape '\'
               or coalesce(e.alias_name, '') ilike @term escape '\'
               or (
                  @include_related_entity_names
                  and coalesce(linked.searchable_entity_names, '') ilike @term
                     escape '\'
               )
            )
            """
         );
         term = $"%{escapedTerm}%";
      }

      if(broadcastOrganizationOnly)
      {
         whereClauses.Add(
            $"""
            {BroadcastEntityFilter
               .GetBroadcastOrganizationEntityTypePredicateSql(
                  "e.entity_type_id"
               )}
            """
         );
      }

      if(excludeEntityId is not null)
      {
         whereClauses.Add("e.id <> @exclude_entity_id");
      }

      if(normalizedEntityTypeIds.Length > 0)
      {
         whereClauses.Add("e.entity_type_id = any(@entity_type_ids)");
      }

      if(normalizedSportIds.Length > 0)
      {
         whereClauses.Add("e.sport_id = any(@sport_ids)");
      }

      DateTimeOffset activityStart = default;
      DateTimeOffset activityEnd = default;

      if(activityDate is not null)
      {
         var window = SportDay.ForDate(activityDate.Value);
         activityStart = TimeZoneHelper.ToUtc(
            window.StartDate,
            window.Cutoff,
            SportDay.TimeZoneId
         );
         activityEnd = TimeZoneHelper.ToUtc(
            window.EndDateExclusive,
            window.Cutoff,
            SportDay.TimeZoneId
         );
         whereClauses.Add(
            $$"""
            exists (
               select 1
               from activity_entity_links activity_link
               join activities linked_activity
                  on linked_activity.id = activity_link.activity_id
               where activity_link.entity_id = e.id
                  and linked_activity.publication_status_id =
                     '{{ActivityPublicationStatusIds.Published}}'
                  and (
                     (
                        linked_activity.starts_at is not null
                        and linked_activity.starts_at >= @activity_start
                        and linked_activity.starts_at < @activity_end
                     )
                     or (
                        linked_activity.starts_at is null
                        and linked_activity.activity_date = @activity_date
                     )
                  )
            )
            """
         );
      }

      var whereSql = whereClauses.Count == 0
         ? string.Empty
         : "where " + string.Join("\n         and ", whereClauses);
      var sql = $"""
         select
            e.id,
            e.canonical_name,
            et.label,
            s.name,
            p.id,
            p.label,
            coalesce(c.name, e.country_id, ''),
            coalesce(linked.related_organization_names, ''),
            coalesce(linked.related_person_count, 0)::integer,
            e.person_gender_id,
            e.birthdate,
            e.height,
            e.weight,
            e.formative_club
         from entities e
         join entity_types et on et.id = e.entity_type_id
         join sports s on s.id = e.sport_id
         join entity_watch_priorities p on p.id = e.watch_priority_id
         left join countries c on c.id = e.country_id
         left join lateral (
            select
               string_agg(linked_name, ', ' order by linked_name)
                  as searchable_entity_names,
               string_agg(
                  linked_name,
                  ', ' order by linked_name
               ) filter (
                  where linked_type not in (
                     '{TrackedEntityTypeIds.Person}',
                     '{TrackedEntityTypeIds.Pair}'
                  )
               ) as related_organization_names,
               count(*) filter (
                  where linked_type = '{TrackedEntityTypeIds.Person}'
               ) as related_person_count
            from (
               select distinct
                  e2.id as linked_id,
                  e2.canonical_name as linked_name,
                  e2.entity_type_id as linked_type
               from entity_to_entity_links l
               join entities e2
                  on e2.id =
                     {GetOtherSideEntityIdSql("e.id")}
               where l.source_entity_id = e.id
                  or l.target_entity_id = e.id
            ) linked_entities
         ) linked on true
         {whereSql}
         order by e.canonical_name
         {GetLimitSql(maxResults)}
         """;

      await using var command = dataSource.CreateCommand(sql);

      if(applyTermFilter)
      {
         command.Parameters.AddWithValue("term", term);
         command.Parameters.AddWithValue(
            "include_related_entity_names",
            includeRelatedEntityNames
         );
      }

      if(normalizedEntityTypeIds.Length > 0)
      {
         command.Parameters.AddWithValue(
            "entity_type_ids",
            normalizedEntityTypeIds
         );
      }

      if(normalizedSportIds.Length > 0)
      {
         command.Parameters.AddWithValue("sport_ids", normalizedSportIds);
      }

      if(excludeEntityId is not null)
      {
         command.Parameters.AddWithValue(
            "exclude_entity_id",
            excludeEntityId.Value
         );
      }

      if(maxResults is > 0)
      {
         command.Parameters.AddWithValue("max_results", maxResults.Value);
      }

      if(activityDate is not null)
      {
         command.Parameters.AddWithValue("activity_date", activityDate);
         command.Parameters.AddWithValue(
            "activity_start",
            activityStart
         );
         command.Parameters.AddWithValue(
            "activity_end",
            activityEnd
         );
      }

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var entities = new List<EntityListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         entities.Add(
            new EntityListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.GetString(5),
               reader.GetString(6),
               reader.GetString(7),
               reader.GetInt32(8),
               reader.IsDBNull(9) ? null : reader.GetString(9),
               reader.IsDBNull(10) ? null : reader.GetFieldValue<DateOnly>(10),
               reader.IsDBNull(11) ? null : reader.GetInt32(11),
               reader.IsDBNull(12) ? null : reader.GetInt32(12),
               reader.IsDBNull(13) ? null : reader.GetString(13)
            )
         );
      }

      return entities;
   }

   public async Task<IReadOnlyList<EntityLinkOption>>
      GetEntityLinkOptionsByIdsAsync(
         IReadOnlyCollection<Guid> ids,
         Guid? excludeEntityId,
         CancellationToken cancellationToken
      )
   {
      if(ids.Count == 0)
      {
         return [];
      }

      var sql = $"""
         select
            e.id,
            e.canonical_name,
            et.label,
            s.name
         from entities e
         join entity_types et on et.id = e.entity_type_id
         join sports s on s.id = e.sport_id
         where e.id = any(@ids)
            {GetExcludeEntitySql(excludeEntityId)}
         order by e.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("ids", ids.ToArray());

      if(excludeEntityId is not null)
      {
         command.Parameters.AddWithValue(
            "exclude_entity_id",
            excludeEntityId.Value
         );
      }

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<EntityLinkOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         options.Add(
            new EntityLinkOption(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3)
            )
         );
      }

      return options;
   }

   public async Task<EntityEditModel?> GetEntityForEditAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      var entitySql = await BuildEntitySqlAsync(cancellationToken);

      await using var command = dataSource.CreateCommand(entitySql);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      var model = new EntityEditModel
      {
         Id = reader.GetGuid(0),
         CanonicalName = reader.GetString(1),
         EntityTypeId = reader.GetString(2),
         SportId = reader.GetString(3),
         CountryId = reader.GetString(4),
         CountryRelevanceKindId = reader.GetString(5),
         CountryRelevanceReason = reader.GetString(6),
         WatchPriorityId = reader.GetString(7),
         ExpectedStabilityId = reader.GetString(8),
         AliasName = reader.IsDBNull(9) ? null : reader.GetString(9),
         Bio = reader.IsDBNull(10) ? null : reader.GetString(10),
         Birthdate = reader.IsDBNull(11)
            ? null
            : reader.GetFieldValue<DateOnly>(11),
         Height = reader.IsDBNull(12) ? null : reader.GetInt32(12),
         Weight = reader.IsDBNull(13) ? null : reader.GetInt32(13),
         FormativeClub = reader.IsDBNull(14)
            ? null
            : reader.GetString(14),
         PersonGenderId = reader.IsDBNull(15) ? null : reader.GetString(15),
         HasPrimaryThumbnail = reader.GetBoolean(16),
         PrimaryImageSourceUrl = reader.IsDBNull(17)
            ? null
            : reader.GetString(17)
      };

      await reader.DisposeAsync();

      var linkSql = $$"""
         select
            {{GetOtherSideEntityIdSql("@id")}}
               as linked_entity_id
         from entity_to_entity_links
         where source_entity_id = @id or target_entity_id = @id
         order by linked_entity_id
         """;

      await using var linkCommand = dataSource.CreateCommand(linkSql);
      linkCommand.Parameters.AddWithValue("id", id);
      await using var linkReader = await linkCommand.ExecuteReaderAsync(
         cancellationToken
      );

      while(await linkReader.ReadAsync(cancellationToken))
      {
         model.LinkedEntityIds.Add(linkReader.GetGuid(0));
      }

      return model;
   }

   public async Task<EntityPrimaryThumbnail?>
      GetEntityPrimaryThumbnailAsync(
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      var sql = $$"""
         select
            image.thumbnail_data,
            image.thumbnail_mime_type
         from entity_images image
         where image.entity_id = @entity_id
            and image.review_status =
               '{{EntityImageReviewStatusIds.Approved}}'
            and image.is_primary
            and image.thumbnail_data is not null
            and image.thumbnail_mime_type is not null
            and image.thumbnail_mime_type ilike 'image/%'
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

      return new EntityPrimaryThumbnail(
         reader.GetFieldValue<byte[]>(0),
         reader.GetString(1)
      );
   }

   public async Task<IReadOnlyList<EntityActivityListItem>>
      GetEntityActivitiesAsync(
         Guid entityId,
         CancellationToken cancellationToken
      )
   {
      var sql = $$"""
         select
            a.id,
            a.activity_date,
            a.local_start_time,
            coalesce(org.organization_name, '') as organization_name,
            a.title,
            s.name,
            at.label,
            a.publication_status_id
         from activities a
         join sports s on s.id = a.sport_id
         join activity_types at on at.id = a.activity_type_id
         left join lateral (
            select org_entity.canonical_name as organization_name
            from entities org_entity
            where org_entity.id =
               {{ActivityRepository
                  .GetActivityOrganizationEntityIdSql("a")}}
         ) org on true
         where (
            exists (
               select 1
               from activity_entity_links participant_link
               where participant_link.activity_id = a.id
                  and participant_link.entity_id = @entity_id
            )
            or {{ActivityRepository
               .GetActivityOrganizationEntityIdSql("a")}} = @entity_id
         )
         order by
            a.activity_date desc,
            a.local_start_time desc nulls last,
            a.title
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("entity_id", entityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var activities = new List<EntityActivityListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         activities.Add(
            new EntityActivityListItem(
               reader.GetGuid(0),
               reader.GetFieldValue<DateOnly>(1),
               reader.IsDBNull(2)
                  ? null
                  : reader.GetFieldValue<TimeOnly>(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.GetString(5),
               reader.GetString(6),
               reader.GetString(7)
            )
         );
      }

      return activities;
   }

   public async Task<EntityEditModel?> GetEntityCloneTemplateAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      var entitySql = await BuildEntitySqlAsync(cancellationToken);

      await using var command = dataSource.CreateCommand(entitySql);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      var model = new EntityEditModel
      {
         Id = null,
         CanonicalName = reader.GetString(1),
         EntityTypeId = TrackedEntityTypeIds.Person,
         SportId = reader.GetString(3),
         CountryId = reader.GetString(4),
         CountryRelevanceKindId = reader.GetString(5),
         CountryRelevanceReason = reader.GetString(6),
         WatchPriorityId = reader.GetString(7),
         ExpectedStabilityId = reader.GetString(8),
         AliasName = reader.IsDBNull(9) ? null : reader.GetString(9),
         Bio = reader.IsDBNull(10) ? null : reader.GetString(10),
         Birthdate = reader.IsDBNull(11)
            ? null
            : reader.GetFieldValue<DateOnly>(11),
         Height = reader.IsDBNull(12) ? null : reader.GetInt32(12),
         Weight = reader.IsDBNull(13) ? null : reader.GetInt32(13),
         FormativeClub = reader.IsDBNull(14)
            ? null
            : reader.GetString(14),
         PersonGenderId = reader.IsDBNull(15) ? null : reader.GetString(15)
      };

      await reader.DisposeAsync();

      var linkSql = $"""
         select
            {GetOtherSideEntityIdSql("@id")}
               as linked_entity_id
         from entity_to_entity_links l
         join entities linked
            on linked.id =
               {GetOtherSideEntityIdSql("@id")}
            where (
               source_entity_id = @id
               or target_entity_id = @id
            )
            and {BroadcastEntityFilter.GetNonOrganizationEntityTypePredicateSql(
               "linked.entity_type_id"
            )}
         order by linked_entity_id
         """;

      await using var linkCommand = dataSource.CreateCommand(linkSql);
      linkCommand.Parameters.AddWithValue("id", id);
      await using var linkReader = await linkCommand.ExecuteReaderAsync(
         cancellationToken
      );

      while(await linkReader.ReadAsync(cancellationToken))
      {
         model.LinkedEntityIds.Add(linkReader.GetGuid(0));
      }

      return model;
   }

   public async Task<IReadOnlyList<EntityLinkOption>> GetEntityLinkOptionsAsync(
      Guid? excludeEntityId,
      CancellationToken cancellationToken
   )
   {
      var sql = excludeEntityId is null
         ? """
            select
               e.id,
               e.canonical_name,
               et.label,
               s.name
            from entities e
            join entity_types et on et.id = e.entity_type_id
            join sports s on s.id = e.sport_id
            order by e.canonical_name
            """
         : """
            select
               e.id,
               e.canonical_name,
               et.label,
               s.name
            from entities e
            join entity_types et on et.id = e.entity_type_id
            join sports s on s.id = e.sport_id
            where e.id <> @exclude_entity_id
            order by e.canonical_name
            """;

      await using var command = dataSource.CreateCommand(sql);

      if(excludeEntityId is not null)
      {
         command.Parameters.AddWithValue("exclude_entity_id", excludeEntityId);
      }

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<EntityLinkOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         options.Add(
            new EntityLinkOption(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3)
            )
         );
      }

      return options;
   }

   private static string GetExcludeEntitySql(Guid? excludeEntityId) =>
      excludeEntityId is null
         ? string.Empty
         : """
            and e.id <> @exclude_entity_id
            """;

   private static string GetLimitSql(int? maxResults) =>
      maxResults is > 0
         ? """
            limit @max_results
            """
         : string.Empty;

   internal static string GetOtherSideEntityIdSql(string entityIdSql)
   {
      return $"""
         case
            when source_entity_id = {entityIdSql}
               then target_entity_id
            else source_entity_id
         end
         """;
   }

   public async Task<IReadOnlyList<EntityLinkOption>>
      GetOrganizationEntityOptionsAsync(
         CancellationToken cancellationToken,
         string? sportId = null
      )
   {
      var sportFilter = string.IsNullOrWhiteSpace(sportId)
         ? string.Empty
         : "and e.sport_id = @sport_id";
      var sql = $"""
         select
            e.id,
            e.canonical_name,
            et.label,
            s.name
         from entities e
         join entity_types et on et.id = e.entity_type_id
         join sports s on s.id = e.sport_id
         where {BroadcastEntityFilter.GetNonOrganizationEntityTypePredicateSql(
            "e.entity_type_id"
         )}
            {sportFilter}
         order by e.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);

      if(!string.IsNullOrWhiteSpace(sportId))
      {
         command.Parameters.AddWithValue("sport_id", sportId);
      }

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<EntityLinkOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         options.Add(
            new EntityLinkOption(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3)
            )
         );
      }

      return options;
   }

   public async Task<IReadOnlyList<EntityLinkOption>>
      GetBroadcastOrganizationLinkOptionsAsync(
         CancellationToken cancellationToken
      )
   {
      var sql = $"""
         select
            e.id,
            e.canonical_name,
            et.label,
            s.name
         from entities e
         join entity_types et on et.id = e.entity_type_id
         join sports s on s.id = e.sport_id
         where {BroadcastEntityFilter.GetNonOrganizationEntityTypePredicateSql(
            "e.entity_type_id"
         )}
         order by e.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<EntityLinkOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         options.Add(
            new EntityLinkOption(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3)
            )
         );
      }

      return options;
   }

   public async Task<IReadOnlyList<EntityLinkOption>>
      SearchBroadcastOrganizationLinkOptionsAsync(
         string term,
         CancellationToken cancellationToken
      )
   {
      term = term.Trim();

      if(term == string.Empty)
      {
         return [];
      }

      var sql = $"""
         select
            e.id,
            e.canonical_name,
            et.label,
            s.name
         from entities e
         join entity_types et on et.id = e.entity_type_id
         join sports s on s.id = e.sport_id
         where {BroadcastEntityFilter.GetNonOrganizationEntityTypePredicateSql(
            "e.entity_type_id"
         )}
            and (
               e.canonical_name ilike '%' || @term || '%'
               or coalesce(e.alias_name, '') ilike '%' || @term || '%'
            )
         order by e.canonical_name
         limit 20
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("term", term);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<EntityLinkOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         options.Add(
            new EntityLinkOption(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3)
            )
         );
      }

      return options;
   }

   public Task<IReadOnlyList<EntityNameOption>>
      GetPersonEntityNameOptionsAsync(
         CancellationToken cancellationToken
      )
   {
      return GetParticipantEntityNameOptionsAsync(cancellationToken);
   }

   public Task<IReadOnlyList<EntityNameOption>>
      GetPersonEntityNameOptionsAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      )
   {
      return GetParticipantEntityNameOptionsAsync(
         organizationEntityId,
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<EntityNameOption>>
      GetParticipantEntityNameOptionsAsync(
         CancellationToken cancellationToken
      )
   {
      const string sql = $$"""
         select id, name
         from (
            select
               e.id,
               e.canonical_name as name
            from entities e
            where e.entity_type_id in (
               '{{TrackedEntityTypeIds.Person}}',
               '{{TrackedEntityTypeIds.Pair}}'
            )
            union all
            select
               e.id,
               e.alias_name as name
            from entities e
            where e.entity_type_id in (
               '{{TrackedEntityTypeIds.Person}}',
               '{{TrackedEntityTypeIds.Pair}}'
            )
               and e.alias_name is not null
         ) names
         order by name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<EntityNameOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         options.Add(
            new EntityNameOption(
               reader.GetGuid(0),
               reader.GetString(1)
            )
         );
      }

      return options;
   }

   public async Task<IReadOnlyList<EntityNameOption>>
      GetParticipantEntityNameOptionsAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      )
   {
      const string sql = $$"""
         with linked_persons as (
            select distinct
               e.id,
               e.canonical_name,
               e.alias_name
            from entities e
            where e.entity_type_id in (
               '{{TrackedEntityTypeIds.Person}}',
               '{{TrackedEntityTypeIds.Pair}}'
            )
               and exists (
                  select 1
                  from entity_to_entity_links l
                  where (l.source_entity_id = @organization_entity_id
                        and l.target_entity_id = e.id)
                     or (l.target_entity_id = @organization_entity_id
                        and l.source_entity_id = e.id)
               )
         )
         select id, name
         from (
            select
               id,
               canonical_name as name
            from linked_persons
            union all
            select
               id,
               alias_name as name
            from linked_persons
            where alias_name is not null
         ) names
         order by name
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationEntityId
      );

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<EntityNameOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         options.Add(
            new EntityNameOption(
               reader.GetGuid(0),
               reader.GetString(1)
            )
         );
      }

      return options;
   }

   public async Task<IReadOnlyList<EntityNameOption>>
      GetBroadcastParticipantEntityNameOptionsAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      )
   {
      const string sql = $$"""
         with linked_participants as (
            select
               e.id,
               e.canonical_name,
               e.alias_name
            from entities e
            where e.entity_type_id in (
               '{{TrackedEntityTypeIds.Person}}',
               '{{TrackedEntityTypeIds.Pair}}'
            )
               and exists (
                  select 1
                  from entity_to_entity_links l
                  where (l.source_entity_id = @organization_entity_id
                        and l.target_entity_id = e.id)
                     or (l.target_entity_id = @organization_entity_id
                        and l.source_entity_id = e.id)
               )
            union
            select distinct
               person.id,
               person.canonical_name,
               person.alias_name
            from entities person
            join entity_to_entity_links person_team_link
               on person_team_link.source_entity_id = person.id
                  or person_team_link.target_entity_id = person.id
            join entities team
               on team.entity_type_id = '{{TrackedEntityTypeIds.Team}}'
                  and (
                     team.id = person_team_link.source_entity_id
                     or team.id = person_team_link.target_entity_id
                  )
            join entity_to_entity_links team_organization_link
               on (
                  team_organization_link.source_entity_id = team.id
                  and team_organization_link.target_entity_id =
                     @organization_entity_id
               )
                  or (
                     team_organization_link.target_entity_id = team.id
                     and team_organization_link.source_entity_id =
                        @organization_entity_id
                  )
            where person.entity_type_id =
               '{{TrackedEntityTypeIds.Person}}'
         )
         select id, name
         from (
            select
               id,
               canonical_name as name
            from linked_participants
            union all
            select
               id,
               alias_name as name
            from linked_participants
            where alias_name is not null
         ) names
         order by name
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationEntityId
      );

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<EntityNameOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         options.Add(
            new EntityNameOption(
               reader.GetGuid(0),
               reader.GetString(1)
            )
         );
      }

      return options;
   }

   public async Task<IReadOnlyList<LookupOption>> GetCountryOptionsAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select id, name
         from countries
         order by name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<LookupOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         options.Add(
            new LookupOption(reader.GetString(0), reader.GetString(1))
         );
      }

      return options;
   }

   public async Task<IReadOnlyList<LookupOption>>
      GetPersonGenderOptionsAsync(
         CancellationToken cancellationToken
      )
   {
      await Task.CompletedTask.WaitAsync(cancellationToken);

      return
      [
         new LookupOption(PersonGenderIds.Female, "Female"),
         new LookupOption(PersonGenderIds.Male, "Male")
      ];
   }

   private async Task<bool> HasPersonGenderColumnAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select exists (
            select 1
            from information_schema.columns
            where table_schema = current_schema()
               and table_name = 'entities'
               and column_name = 'person_gender_id'
         )
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      await reader.ReadAsync(cancellationToken);
      return reader.GetBoolean(0);
   }

   private static string BuildEntitySql(bool includePersonGender)
   {
      var personGenderColumn = includePersonGender
         ? "person_gender_id"
         : "null::text as person_gender_id";

      return $$"""
         select
            id,
            canonical_name,
            entity_type_id,
            sport_id,
            country_id,
            country_relevance_kind_id,
            country_relevance_reason,
            watch_priority_id,
            expected_stability_id,
            alias_name,
            bio,
            birthdate,
            height,
            weight,
            formative_club,
            {{personGenderColumn}},
            exists (
               select 1
               from entity_images image
               where image.entity_id = entities.id
                  and image.review_status =
                     '{{EntityImageReviewStatusIds.Approved}}'
                  and image.is_primary
                  and image.thumbnail_data is not null
                  and image.thumbnail_mime_type is not null
                  and image.thumbnail_mime_type ilike 'image/%'
            ) as has_primary_thumbnail,
            (
               select image.source_url
               from entity_images image
               where image.entity_id = entities.id
                  and image.review_status =
                     '{{EntityImageReviewStatusIds.Approved}}'
                  and image.is_primary
               limit 1
            ) as primary_image_source_url
         from entities
         where id = @id
         """;
   }

   private async Task<string> BuildEntitySqlAsync(
      CancellationToken cancellationToken
   )
   {
      return BuildEntitySql(
         await HasPersonGenderColumnAsync(cancellationToken)
      );
   }

}
