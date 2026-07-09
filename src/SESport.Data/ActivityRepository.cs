using Npgsql;
using SESport.Core.Broadcast;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SESport.Data;

public sealed class ActivityRepository(NpgsqlDataSource dataSource)
{
   private const string TimedOrderClause = """
      order by
         a.starts_at nulls last,
         a.activity_date,
         a.local_start_time nulls last,
         a.title
      """;

   private const string DefaultOrderClause = """
      order by
         a.activity_date,
         a.local_start_time nulls last,
         a.title
      """;

   public async Task<IReadOnlyList<ActivityListItem>> GetActivitiesAsync(
      DateOnly date,
      string? status,
      IReadOnlyCollection<string> sportIds,
      CancellationToken cancellationToken
   )
   {
      var normalizedSports = NormalizeSelectedSports(sportIds);
      // Timed rows follow sport day; untimed rows keep their stored date.
      var window = SportDay.ForDate(date);
      var start = ToUtc(window.StartDate, window.Cutoff);
      var end = ToUtc(window.EndDateExclusive, window.Cutoff);
      var whereClause = new StringBuilder()
         .AppendLine("where (")
         .AppendLine("   (a.starts_at >= @start")
         .AppendLine("      and a.starts_at < @end)")
         .AppendLine("   or (")
         .AppendLine("      a.starts_at is null")
         .AppendLine("      and a.activity_date = @date")
         .AppendLine("   )")
         .AppendLine(")");

      if(!string.Equals(
         status,
         ActivityListStatusIds.All,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         whereClause.AppendLine("   and a.publication_status_id = @status");
      }

      if(normalizedSports.Count > 0)
      {
         whereClause.AppendLine("   and a.sport_id = any(@sport_ids)");
      }

      return await QueryActivityListAsync(
         whereClause.ToString(),
         TimedOrderClause,
         "s.name",
         command =>
         {
            command.Parameters.AddWithValue("start", start);
            command.Parameters.AddWithValue("end", end);
            command.Parameters.AddWithValue("date", date);

            if(!string.Equals(
               status,
               ActivityListStatusIds.All,
               StringComparison.OrdinalIgnoreCase
            ))
            {
               command.Parameters.AddWithValue(
                  "status",
                  status ?? ActivityListStatusIds.All
               );
            }

            if(normalizedSports.Count > 0)
            {
               command.Parameters.AddWithValue(
                  "sport_ids",
                  normalizedSports.ToArray()
               );
            }
         },
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<ActivityListItem>> GetPublishedForDateAsync(
      DateOnly date,
      CancellationToken cancellationToken
   )
   {
      return await GetPublishedActivitiesAsync(
         SportDay.ForDate(date),
         cancellationToken
      );
   }

   private async Task<IReadOnlyList<ActivityListItem>>
      GetPublishedActivitiesAsync(
         SportDayWindow window,
         CancellationToken cancellationToken
      )
   {
      return await QueryActivityListAsync(
         $$"""
            where a.publication_status_id =
               '{{ActivityPublicationStatusIds.Published}}'
               and a.starts_at >= @start
               and a.starts_at < @end
         """,
         DefaultOrderClause,
         """
         coalesce(s.display_name, s.name)
         """,
         command =>
         {
            command.Parameters.AddWithValue(
               "start",
               ToUtc(window.StartDate, window.Cutoff)
            );
            command.Parameters.AddWithValue(
               "end",
               ToUtc(window.EndDateExclusive, window.Cutoff)
            );
         },
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<EntityOption>> GetEntityOptionsAsync(
      CancellationToken cancellationToken
   )
   {
      var sql = $$"""
         select
            e.id,
            e.canonical_name,
            e.entity_type_id,
            s.name,
            coalesce(org.organization_names, ''),
            p.sort_order,
            e.person_gender_id,
            e.alias_name
         from entities e
         join sports s
            on s.id = e.sport_id
            and e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
         join entity_watch_priorities p
            on p.id = e.watch_priority_id
         {{EntityLinkSql.GetLinkedOrganizationNamesLateralSql("e")}}
         order by
            p.sort_order,
            e.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var entities = new List<EntityOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         entities.Add(
            new EntityOption(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.GetInt32(5),
               reader.IsDBNull(6) ? null : reader.GetString(6),
               reader.IsDBNull(7) ? null : reader.GetString(7)
            )
         );
      }

      return entities;
   }

   public async Task<IReadOnlyList<EntityOption>>
      GetPersonEntitiesForOrganizationAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      )
   {
      var sql = $$"""
         select
            e.id,
            e.canonical_name,
            e.entity_type_id,
            s.name,
            coalesce(org.alias_name, org.canonical_name) as organization_names,
            p.sort_order,
            e.person_gender_id,
            e.alias_name
         from entities e
         join entities org
            on org.id = @organization_entity_id
         join sports s
            on s.id = e.sport_id
         join entity_watch_priorities p
            on p.id = e.watch_priority_id
         where e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
            and exists (
               select 1
               from entity_to_entity_links l
               where (l.source_entity_id = @organization_entity_id
                     and l.target_entity_id = e.id)
                  or (l.target_entity_id = @organization_entity_id
                     and l.source_entity_id = e.id)
            )
         order by sort_order, canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationEntityId
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var entities = new List<EntityOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         entities.Add(
            new EntityOption(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.GetInt32(5),
               reader.IsDBNull(6) ? null : reader.GetString(6),
               reader.IsDBNull(7) ? null : reader.GetString(7)
            )
         );
      }

      return entities;
   }

   public async Task<IReadOnlyList<EntityOption>>
      GetPersonEntitiesForPromptCandidatesAsync(
         Guid organizationEntityId,
         CancellationToken cancellationToken
      )
   {
      var sql = $$"""
         with candidate_rows as (
            select distinct
               e.id,
               e.canonical_name,
               e.entity_type_id,
               s.name,
               coalesce(
                  org.alias_name,
                  org.canonical_name
               ) as organization_names,
               p.sort_order,
               e.person_gender_id,
               e.alias_name
            from entities e
            join entities org
               on org.id = @organization_entity_id
            join sports s
               on s.id = e.sport_id
            join entity_watch_priorities p
               on p.id = e.watch_priority_id
            where e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
               and exists (
                  select 1
                  from entity_to_entity_links l
                  where (l.source_entity_id = @organization_entity_id
                        and l.target_entity_id = e.id)
                     or (l.target_entity_id = @organization_entity_id
                        and l.source_entity_id = e.id)
               )
         )
         select *
         from candidate_rows
         order by sort_order, random()
         limit 5
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationEntityId
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var entities = new List<EntityOption>();

      while(await reader.ReadAsync(cancellationToken))
      {
         entities.Add(
            new EntityOption(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.GetInt32(5),
               reader.IsDBNull(6) ? null : reader.GetString(6),
               reader.IsDBNull(7) ? null : reader.GetString(7)
            )
         );
      }

      return entities;
   }

   public async Task<IReadOnlyList<LookupOption>> GetActivityTypeOptionsAsync(
      CancellationToken cancellationToken
   )
   {
      return await GetLookupOptionsAsync(
         "select id, label from activity_types order by sort_order, label",
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<LookupOption>> GetSportOptionsAsync(
      CancellationToken cancellationToken
   )
   {
      return await GetLookupOptionsAsync(
         "select id, name from sports order by name",
         cancellationToken
      );
   }

   public async Task<ActivityEditModel?> GetForEditAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            a.id,
            a.title,
            a.description,
            a.teaser,
            a.activity_type_id,
            a.sport_id,
            a.activity_date,
            a.local_start_time,
            a.time_zone_id,
            a.publication_status_id,
            a.tv_channel_name,
            e.uri,
            e.title,
            e.comment
         from activities a
         left join lateral (
            select uri, title, comment
            from activity_evidence
            where activity_id = a.id
            order by created_at desc
            limit 1
         ) e on true
         where a.id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      var model = new ActivityEditModel
      {
         Id = reader.GetGuid(0),
         Title = reader.GetString(1),
         Description = ReadString(reader, 2),
         Teaser = ReadString(reader, 3),
         ActivityType = reader.GetString(4),
         SportId = reader.GetString(5),
         ActivityDate = reader.GetFieldValue<DateOnly>(6),
         LocalStartTime = ReadTimeOnly(reader, 7),
         TimeZoneId = reader.GetString(8),
         IsPublished =
            reader.GetString(9) == ActivityPublicationStatusIds.Published,
         TvChannelName = ReadString(reader, 10),
         EvidenceUri = ReadString(reader, 11),
         EvidenceTitle = ReadString(reader, 12),
         EvidenceComment = ReadString(reader, 13)
      };

      await reader.DisposeAsync();

      const string linkSql = """
         select entity_id, organization_entity_id
         from activity_entity_links
         where activity_id = @id
         order by id
         """;

      await using var linkCommand = dataSource.CreateCommand(linkSql);
      linkCommand.Parameters.AddWithValue("id", id);
      await using var linkReader = await linkCommand.ExecuteReaderAsync(
         cancellationToken
      );

      while(await linkReader.ReadAsync(cancellationToken))
      {
         model.LinkedEntityIds.Add(linkReader.GetGuid(0));

         if(linkReader.IsDBNull(1))
         {
            continue;
         }

         var organizationEntityId = linkReader.GetGuid(1);

         if(model.OrganizationEntityId is null)
         {
            model.OrganizationEntityId = organizationEntityId;
         }
         else if(model.OrganizationEntityId != organizationEntityId)
         {
            model.OrganizationEntityId = null;
         }
      }

      return model;
   }

   public async Task<IReadOnlyList<ActivityParticipantListItem>>
      GetParticipantsForEditAsync(
         Guid? activityId,
         IReadOnlyCollection<Guid> entityIds,
         CancellationToken cancellationToken
      )
   {
      if(activityId is null && entityIds.Count == 0)
      {
         return [];
      }

      var activityLinkJoin = activityId is null
         ? string.Empty
         : "join activity_entity_links al on al.entity_id = e.id";
      var whereClause = activityId is null
         ? "where e.id = any(@entity_ids)"
         : "where al.activity_id = @activity_id";
      var sql = $$"""
         select distinct
            e.id,
            e.canonical_name,
            coalesce(org.organization_names, ''),
            wp.label,
            case e.person_gender_id
               when '{{PersonGenderIds.Female}}' then 'Female'
               when '{{PersonGenderIds.Male}}' then 'Male'
               when '{{PersonGenderIds.NonBinary}}' then 'Non-binary'
               else ''
            end,
            coalesce(e.alias_name, ''),
            wp.sort_order
         from entities e
         join entity_watch_priorities wp on wp.id = e.watch_priority_id
         {{activityLinkJoin}}
         {{EntityLinkSql.GetLinkedOrganizationNamesLateralSql("e")}}
         {{whereClause}}
         order by wp.sort_order, e.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);

      if(activityId is null)
      {
         command.Parameters.AddWithValue("entity_ids", entityIds.ToArray());
      }
      else
      {
         command.Parameters.AddWithValue("activity_id", activityId.Value);
      }

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var participants = new List<ActivityParticipantListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         participants.Add(
            new ActivityParticipantListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.GetString(5)
            )
         );
      }

      return participants;
   }

   public async Task DeleteParticipantAsync(
      Guid activityId,
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from activity_entity_links
         where activity_id = @activity_id
            and entity_id = @entity_id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", entityId);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task AddParticipantAsync(
      Guid activityId,
      Guid entityId,
      Guid organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = $$"""
         insert into activity_entity_links (
            id,
            activity_id,
            entity_id,
            organization_entity_id
         )
         select
            @id,
            @activity_id,
            e.id,
            @organization_entity_id
         from entities e
         where e.id = @entity_id
            and e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
            and exists (
               select 1
               from entity_to_entity_links l
               where (l.source_entity_id = @organization_entity_id
                     and l.target_entity_id = e.id)
                  or (l.target_entity_id = @organization_entity_id
                     and l.source_entity_id = e.id)
            )
            and not exists (
               select 1
               from activity_entity_links existing
               where existing.activity_id = @activity_id
                  and existing.entity_id = e.id
            )
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", entityId);
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationEntityId
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<IReadOnlyList<ActivityParticipantListItem>>
      SearchParticipantCandidatesAsync(
         Guid organizationEntityId,
         string term,
         IReadOnlyCollection<Guid> excludedEntityIds,
         CancellationToken cancellationToken
      )
   {
      term = term.Trim();
      var applyTermFilter = term != string.Empty;
      var escapedTerm = applyTermFilter
         ? term
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
         : string.Empty;
      var excludedIds = excludedEntityIds
         .Where(entityId => entityId != Guid.Empty)
         .Distinct()
         .ToArray();
      var termFilterSql = applyTermFilter
         ? """
            and (
               e.canonical_name ilike @term escape '\'
               or coalesce(e.alias_name, '') ilike @term escape '\'
            )
            """
         : string.Empty;
      var excludedSql = excludedIds.Length == 0
         ? string.Empty
         : "and e.id <> all(@excluded_entity_ids)";
      var limitSql = applyTermFilter ? "limit 20" : string.Empty;
      var sql = $$"""
         select
            e.id,
            e.canonical_name,
            coalesce(org.organization_names, ''),
            wp.label,
            case e.person_gender_id
               when '{{PersonGenderIds.Female}}' then 'Female'
               when '{{PersonGenderIds.Male}}' then 'Male'
               when '{{PersonGenderIds.NonBinary}}' then 'Non-binary'
               else ''
            end,
            coalesce(e.alias_name, ''),
            wp.sort_order
         from entities e
         join entity_watch_priorities wp on wp.id = e.watch_priority_id
         {{EntityLinkSql.GetLinkedOrganizationNamesLateralSql("e")}}
         where e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
            {{termFilterSql}}
            and exists (
               select 1
               from entity_to_entity_links l
               where (l.source_entity_id = @organization_entity_id
                     and l.target_entity_id = e.id)
                  or (l.target_entity_id = @organization_entity_id
                     and l.source_entity_id = e.id)
            )
            {{excludedSql}}
         order by wp.sort_order, e.canonical_name
         {{limitSql}}
         """;

      await using var command = dataSource.CreateCommand(sql);
      if(applyTermFilter)
      {
         command.Parameters.AddWithValue("term", $"%{escapedTerm}%");
      }
      command.Parameters.AddWithValue(
         "organization_entity_id",
         organizationEntityId
      );

      if(excludedIds.Length > 0)
      {
         command.Parameters.AddWithValue("excluded_entity_ids", excludedIds);
      }

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var participants = new List<ActivityParticipantListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         participants.Add(
            new ActivityParticipantListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4),
               reader.GetString(5)
            )
         );
      }

      return participants;
   }

   public async Task<Guid> SaveAsync(
      ActivityEditModel model,
      CancellationToken cancellationToken
   )
   {
      var id = model.Id ?? Guid.NewGuid();
      var status = model.IsPublished
         ? ActivityPublicationStatusIds.Published
         : ActivityPublicationStatusIds.Draft;
      var startsAt = GetStartsAt(model);

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      var slug = await CreateSlugAsync(
         connection,
         transaction,
         model,
         id,
         cancellationToken
      );

      if(model.Id is null)
      {
         await InsertActivityAsync(
            connection,
            transaction,
            id,
            model,
            startsAt,
            status,
            slug,
            cancellationToken
         );
      }
      else
      {
         await UpdateActivityAsync(
            connection,
            transaction,
            id,
            model,
            startsAt,
            status,
            slug,
            cancellationToken
         );
      }

      await ReplaceEntityLinkAsync(
         connection,
         transaction,
         id,
         model.LinkedEntityIds,
         model.OrganizationEntityId,
         cancellationToken
      );
      await ReplaceEvidenceAsync(
         connection,
         transaction,
         id,
         model,
         cancellationToken
      );
      await transaction.CommitAsync(cancellationToken);
      return id;
   }

   public async Task DeleteAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      await using(var proposalCommand = new NpgsqlCommand(
         """
         update activity_proposals
         set
            status_id = 'Pending',
            activity_id = null,
            updated_at = now()
         where activity_id = @activity_id
         """,
         connection,
         transaction
      ))
      {
         proposalCommand.Parameters.AddWithValue("activity_id", id);
         await proposalCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      await using(var evidenceCommand = new NpgsqlCommand(
         "delete from activity_evidence where activity_id = @activity_id",
         connection,
         transaction
      ))
      {
         evidenceCommand.Parameters.AddWithValue("activity_id", id);
         await evidenceCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      await using(var linkCommand = new NpgsqlCommand(
         "delete from activity_entity_links where activity_id = @activity_id",
         connection,
         transaction
      ))
      {
         linkCommand.Parameters.AddWithValue("activity_id", id);
         await linkCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      await using(var activityCommand = new NpgsqlCommand(
         "delete from activities where id = @id",
         connection,
         transaction
      ))
      {
         activityCommand.Parameters.AddWithValue("id", id);
         await activityCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      await transaction.CommitAsync(cancellationToken);
   }

   private async Task<IReadOnlyList<ActivityListItem>> GetActivityListAsync(
      string whereClause,
      CancellationToken cancellationToken
   )
   {
      return await QueryActivityListAsync(
         whereClause,
         DefaultOrderClause,
         "s.name",
         null,
         cancellationToken
      );
   }

   private async Task<IReadOnlyList<ActivityListItem>> QueryActivityListAsync(
      string whereClause,
      string orderClause,
      string sportNameExpression,
      Action<NpgsqlCommand>? configureCommand,
      CancellationToken cancellationToken
   )
   {
      var sql = CreateActivityListSql(
         whereClause,
         orderClause,
         sportNameExpression
      );

      await using var command = dataSource.CreateCommand(sql);
      configureCommand?.Invoke(command);
      return await ReadActivityListAsync(command, cancellationToken);
   }

   private static string CreateActivityListSql(
      string whereClause,
      string orderClause,
      string sportNameExpression
   )
   {
      var builder = new StringBuilder()
         .AppendLine("select")
         .AppendLine("   a.id,")
         .AppendLine("   a.title,")
         .AppendLine("   a.description,")
         .AppendLine("   a.teaser,")
         .AppendLine("   at.label,")
         .AppendLine("   s.id,")
         .AppendLine($"   {sportNameExpression},")
         .AppendLine("   s.icon_id,")
         .AppendLine("   a.activity_date,")
         .AppendLine("   a.local_start_time,")
         .AppendLine("   a.starts_at,")
         .AppendLine("   a.publication_status_id,")
         .AppendLine("   a.tv_channel_name,")
         .AppendLine("   coalesce(")
         .AppendLine("      string_agg(")
         .AppendLine("         te.canonical_name,")
         .AppendLine("         ', ' order by te.canonical_name")
         .AppendLine("      ),")
         .AppendLine("      ''")
         .AppendLine("   ) as entities,")
         .AppendLine(
            "   coalesce(rp.related_person_entities, '') as " +
            "related_person_entities,"
         )
         .AppendLine(
            "   coalesce(rp.related_person_entity_ids, '{}'::uuid[]) " +
            "as related_person_entity_ids,"
         )
         .AppendLine(
            "   coalesce(ro.related_organization_entities, '') " +
            "as related_organization_entities"
         )
         .AppendLine("from activities a")
         .AppendLine("join sports s on s.id = a.sport_id")
         .AppendLine("join activity_types at on at.id = a.activity_type_id")
         .AppendLine(
            "left join activity_entity_links l on l.activity_id = a.id"
         )
         .AppendLine("left join entities te on te.id = l.entity_id")
         .AppendLine("left join lateral (")
         .AppendLine("   select")
         .AppendLine("      string_agg(")
         .AppendLine("         person_name,")
         .AppendLine("         ', ' order by sort_order, person_name")
         .AppendLine("      ) as related_person_entities,")
         .AppendLine("      coalesce(")
         .AppendLine("         array_agg(")
         .AppendLine("            person_id order by sort_order, person_name")
         .AppendLine("         ),")
         .AppendLine("         '{}'::uuid[]")
         .AppendLine("      ) as related_person_entity_ids")
         .AppendLine("   from (")
         .AppendLine("      select distinct")
         .AppendLine("         p.id as person_id,")
         .AppendLine("         p.canonical_name as person_name,")
         .AppendLine("         wp.sort_order")
         .AppendLine("      from activity_entity_links al")
         .AppendLine("      join entities p on p.id = al.entity_id")
         .AppendLine("      join entity_watch_priorities wp")
         .AppendLine("         on wp.id = p.watch_priority_id")
         .AppendLine("      where al.activity_id = a.id")
         .AppendLine(
            $$"""
               and p.entity_type_id in (
                  '{{TrackedEntityTypeIds.Person}}',
                  '{{TrackedEntityTypeIds.NationalTeam}}',
                  '{{TrackedEntityTypeIds.Pair}}'
               )
            """
         )
         .AppendLine("   ) persons")
         .AppendLine(") rp on true")
         .AppendLine("left join lateral (")
         .AppendLine("   select string_agg(")
         .AppendLine("      distinct organization_name,")
         .AppendLine("      ', ' order by organization_name")
         .AppendLine("   ) as related_organization_entities")
         .AppendLine("   from (")
         .AppendLine("      select distinct")
         .AppendLine("         coalesce(context.alias_name,")
         .AppendLine("            context.canonical_name) as organization_name")
         .AppendLine("      from activity_entity_links al")
         .AppendLine("      join entities p on p.id = al.entity_id")
         .AppendLine("      join entities context")
         .AppendLine("         on context.id = al.organization_entity_id")
         .AppendLine("      where al.activity_id = a.id")
         .AppendLine(
            $$"""
               and p.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
            """
         )
         .AppendLine(
            "         and " +
            BroadcastEntityFilter.GetNonOrganizationEntityTypePredicateSql(
               "context.entity_type_id"
            )
         )
         .AppendLine("   ) organizations")
         .AppendLine(") ro on true")
         .AppendLine(whereClause)
         .AppendLine(
            "group by a.id, at.label, s.id, " +
            $"{sportNameExpression}, s.icon_id,"
         )
         .AppendLine(
            "         a.tv_channel_name, rp.related_person_entities,"
         )
         .AppendLine(
            "         rp.related_person_entity_ids,"
         )
         .AppendLine("         ro.related_organization_entities")
         .AppendLine(orderClause);

      return builder.ToString();
   }

   private static async Task InsertActivityAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid id,
      ActivityEditModel model,
      DateTimeOffset? startsAt,
      string status,
      string slug,
      CancellationToken cancellationToken
   )
   {
      const string sql = $$"""
         insert into activities (
            id,
            title,
            description,
            teaser,
            activity_type_id,
            sport_id,
            activity_date,
            local_start_time,
            starts_at,
            time_zone_id,
            publication_status_id,
            tv_channel_name,
            slug,
            published_at
         )
         values (
            @id,
            @title,
            @description,
            @teaser,
            @activity_type_id,
            @sport_id,
            @activity_date,
            @local_start_time,
            @starts_at,
            @time_zone_id,
            @publication_status_id,
            @tv_channel_name,
            @slug,
            case
               when @publication_status_id =
                  '{{ActivityPublicationStatusIds.Published}}' then now()
               else null
            end
         )
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      AddActivityParameters(command, id, model, startsAt, status, slug);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task UpdateActivityAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid id,
      ActivityEditModel model,
      DateTimeOffset? startsAt,
      string status,
      string slug,
      CancellationToken cancellationToken
   )
   {
      const string sql = $$"""
         update activities
         set
            title = @title,
            description = @description,
            teaser = @teaser,
            activity_type_id = @activity_type_id,
            sport_id = @sport_id,
            activity_date = @activity_date,
            local_start_time = @local_start_time,
            starts_at = @starts_at,
            time_zone_id = @time_zone_id,
            publication_status_id = @publication_status_id,
            tv_channel_name = @tv_channel_name,
            slug = @slug,
            published_at = case
               when @publication_status_id =
                  '{{ActivityPublicationStatusIds.Published}}' then coalesce(
                  published_at,
                  now()
               )
               else null
            end,
            updated_at = now()
         where id = @id
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      AddActivityParameters(command, id, model, startsAt, status, slug);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task ReplaceEntityLinkAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      IEnumerable<Guid> entityIds,
      Guid? organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      await using var deleteCommand = new NpgsqlCommand(
         "delete from activity_entity_links where activity_id = @activity_id",
         connection,
         transaction
      );
      deleteCommand.Parameters.AddWithValue("activity_id", activityId);
      await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

      var distinctEntityIds = entityIds
         .Where(entityId => entityId != Guid.Empty)
         .Distinct()
         .ToList();

      if(distinctEntityIds.Count == 0)
      {
         return;
      }

      const string sql = $$"""
         insert into activity_entity_links (
            id,
            activity_id,
            entity_id,
            organization_entity_id
         )
         values (
            @id,
            @activity_id,
            @entity_id,
            case
               when exists (
                  select 1
                  from entities e
                  where e.id = @entity_id
                     and e.entity_type_id =
                        '{{TrackedEntityTypeIds.Person}}'
               )
                  then @organization_entity_id
               else null
            end
         )
         """;

      foreach(var entityId in distinctEntityIds)
      {
         await using var command = new NpgsqlCommand(
            sql,
            connection,
            transaction
         );
         command.Parameters.AddWithValue("id", Guid.NewGuid());
         command.Parameters.AddWithValue("activity_id", activityId);
         command.Parameters.AddWithValue("entity_id", entityId);
         command.Parameters.AddWithValue(
            "organization_entity_id",
            organizationEntityId ?? (object)DBNull.Value
         );
         await command.ExecuteNonQueryAsync(cancellationToken);
      }
   }

   private static async Task ReplaceEvidenceAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      ActivityEditModel model,
      CancellationToken cancellationToken
   )
   {
      await using var deleteCommand = new NpgsqlCommand(
         "delete from activity_evidence where activity_id = @activity_id",
         connection,
         transaction
      );
      deleteCommand.Parameters.AddWithValue("activity_id", activityId);
      await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

      if(
         string.IsNullOrWhiteSpace(model.EvidenceUri) &&
         string.IsNullOrWhiteSpace(model.EvidenceTitle) &&
         string.IsNullOrWhiteSpace(model.EvidenceComment)
      )
      {
         return;
      }

      const string sql = """
         insert into activity_evidence (
            id,
            activity_id,
            source_id,
            uri,
            title,
            observed_at,
            comment
         )
         values (
            @id,
            @activity_id,
            @source_id,
            @uri,
            @title,
            now(),
            @comment
         )
         """;

      var source = GetSource(model.EvidenceUri);
      await EnsureSourceAsync(
         connection,
         transaction,
         source.Id,
         source.Name,
         cancellationToken
      );

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("source_id", source.Id);
      command.Parameters.AddWithValue("uri", BlankToDbNull(model.EvidenceUri));
      command.Parameters.AddWithValue(
         "title",
         BlankToDbNull(model.EvidenceTitle)
      );
      command.Parameters.AddWithValue(
         "comment",
         BlankToDbNull(model.EvidenceComment)
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task EnsureSourceAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      string sourceId,
      string sourceName,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into sources (id, name)
         values (@id, @name)
         on conflict (id) do update
         set name = excluded.name
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", sourceId);
      command.Parameters.AddWithValue("name", sourceName);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static void AddActivityParameters(
      NpgsqlCommand command,
      Guid id,
      ActivityEditModel model,
      DateTimeOffset? startsAt,
      string status,
      string slug
   )
   {
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("title", model.Title.Trim());
      command.Parameters.AddWithValue(
         "description",
         BlankToDbNull(model.Description)
      );
      command.Parameters.AddWithValue(
         "teaser",
         BlankToDbNull(model.Teaser)
      );
      command.Parameters.AddWithValue("activity_type_id", model.ActivityType);
      command.Parameters.AddWithValue("sport_id", model.SportId.Trim());
      command.Parameters.AddWithValue(
         "activity_date",
         model.ActivityDate!.Value
      );
      command.Parameters.AddWithValue(
         "local_start_time",
         model.LocalStartTime ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue(
         "starts_at",
         startsAt?.ToUniversalTime() ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue("time_zone_id", model.TimeZoneId.Trim());
      command.Parameters.AddWithValue("publication_status_id", status);
      command.Parameters.AddWithValue(
         "tv_channel_name",
         BlankToDbNull(model.TvChannelName)
      );
      command.Parameters.AddWithValue("slug", slug);
   }

   private static DateTimeOffset? GetStartsAt(ActivityEditModel model)
   {
      if(model.ActivityDate is null || model.LocalStartTime is null)
      {
         return null;
      }

      return TimeZoneHelper.ToUtc(
         model.ActivityDate.Value,
         model.LocalStartTime.Value,
         model.TimeZoneId
      );
   }

   private async Task<IReadOnlyList<LookupOption>> GetLookupOptionsAsync(
      string sql,
      CancellationToken cancellationToken
   )
   {
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

   private static (string Id, string Name) GetSource(string? uri)
   {
      if(
         !string.IsNullOrWhiteSpace(uri) &&
         Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri)
      )
      {
         var host = parsedUri.Host.ToLowerInvariant();
         return ($"source:{host}", host);
      }

      return ("source:manual", "Manual");
   }

   private static async Task<string> CreateSlugAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      ActivityEditModel model,
      Guid id,
      CancellationToken cancellationToken
   )
   {
      var baseSlug = NormalizeSlug(
         model.Title,
         model.ActivityDate,
         model.ActivityType
      );

      var existingSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      const string sql = """
         select slug
         from activities
         where id <> @id
            and slug is not null
            and (slug = @base_slug or slug like @prefix)
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("base_slug", baseSlug);
      command.Parameters.AddWithValue("prefix", baseSlug + "-%");

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      while(await reader.ReadAsync(cancellationToken))
      {
         existingSlugs.Add(reader.GetString(0));
      }

      if(!existingSlugs.Contains(baseSlug))
      {
         return baseSlug;
      }

      for(var suffix = 2; ; suffix++)
      {
         var candidate = $"{baseSlug}-{suffix}";
         if(!existingSlugs.Contains(candidate))
         {
            return candidate;
         }
      }
   }

   private static string NormalizeSlug(
      string title,
      DateOnly? activityDate,
      string activityType
   )
   {
      var datePart = DateDisplay.Format(activityDate) ?? "undated";
      var slug = Slugify($"{datePart}-{title}-{activityType}");
      return string.IsNullOrWhiteSpace(slug) ? "activity" : slug;
   }

   private static string Slugify(string value)
   {
      var normalized = value.Normalize(NormalizationForm.FormD);
      var builder = new StringBuilder();

      foreach(var character in normalized)
      {
         var category = CharUnicodeInfo.GetUnicodeCategory(character);
         if(category != UnicodeCategory.NonSpacingMark)
         {
            builder.Append(character);
         }
      }

      return Regex.Replace(
            builder
               .ToString()
               .Normalize(NormalizationForm.FormC)
               .ToLowerInvariant(),
            "[^a-z0-9]+",
            "-"
         )
         .Trim('-');
   }

   private static string FormatTime(NpgsqlDataReader reader)
   {
      var activityDate = reader.GetFieldValue<DateOnly>(8);
      var localStartTime = ReadTimeOnly(reader, 9);

      return localStartTime is null
         ? DateDisplay.Format(activityDate)
         : $"{DateDisplay.Format(activityDate)} {localStartTime:HH:mm}";
   }

   private static string? ReadString(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static async Task<IReadOnlyList<ActivityListItem>>
      ReadActivityListAsync(
         NpgsqlCommand command,
         CancellationToken cancellationToken
      )
   {
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var activities = new List<ActivityListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         activities.Add(
            new ActivityListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               ReadString(reader, 2),
               ReadString(reader, 3),
               reader.GetString(4),
               reader.GetString(5),
               reader.GetString(6),
               GetSportIconPath(ReadString(reader, 7)),
               FormatTime(reader),
               ReadDateTimeOffset(reader, 10),
               ReadString(reader, 12),
               reader.GetString(11),
               reader.GetString(14),
               ReadGuidArray(reader, 15),
               reader.GetString(16)
            )
         );
      }

      return activities;
   }

   private static TimeOnly? ReadTimeOnly(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetFieldValue<TimeOnly>(ordinal);
   }

   private static DateTimeOffset? ReadDateTimeOffset(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetFieldValue<DateTimeOffset>(ordinal);
   }

   private static Guid[] ReadGuidArray(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal)
         ? []
         : reader.GetFieldValue<Guid[]>(ordinal);
   }

   private static List<string> NormalizeSelectedSports(
      IEnumerable<string> values
   )
   {
      return values
         .Where(value => !string.IsNullOrWhiteSpace(value))
         .Select(value => value.Trim())
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   private static object BlankToDbNull(string? value)
   {
      return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
   }

   private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
   {
      return TimeZoneHelper.ToUtc(date, time, SportDay.TimeZoneId);
   }

   private static string? GetSportIconPath(string? iconId)
   {
      if(string.IsNullOrWhiteSpace(iconId))
      {
         return null;
      }

      var fileName = Regex.Replace(
            iconId.Trim().ToLowerInvariant(),
            "[^a-z0-9_-]+",
            "-"
         )
         .Trim('-');

      return $"/icons/sports/{fileName}.svg";
   }
}
