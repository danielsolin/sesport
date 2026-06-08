using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using Npgsql;

namespace SESport.Data;

public sealed class ActivityRepository(NpgsqlDataSource dataSource)
{
   public async Task<IReadOnlyList<ActivityListItem>> GetActivitiesAsync(
      DateOnly date,
      string? status,
      IReadOnlyCollection<string> sportIds,
      CancellationToken cancellationToken
   )
   {
      var normalizedSports = NormalizeSelectedSports(sportIds);
      var sql = new StringBuilder()
         .AppendLine("select")
         .AppendLine("   a.id,")
         .AppendLine("   a.title,")
         .AppendLine("   a.description,")
         .AppendLine("   a.teaser,")
         .AppendLine("   at.label,")
         .AppendLine("   s.id,")
         .AppendLine("   s.name,")
         .AppendLine("   s.icon_id,")
         .AppendLine("   a.activity_date,")
         .AppendLine("   a.local_start_time,")
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
            "   coalesce(rp.related_person_entities, '') as related_person_entities,"
         )
         .AppendLine(
            "   coalesce(ro.related_organization_entities, '') as related_organization_entities"
         )
         .AppendLine("from activities a")
         .AppendLine("join sports s on s.id = a.sport_id")
         .AppendLine("join activity_types at on at.id = a.activity_type_id")
         .AppendLine(
            "left join activity_entity_links l on l.activity_id = a.id"
         )
         .AppendLine("left join entities te on te.id = l.entity_id")
         .AppendLine("left join lateral (")
         .AppendLine("   select string_agg(")
         .AppendLine("      distinct p.canonical_name,")
         .AppendLine("      ', ' order by p.canonical_name")
         .AppendLine("   ) as related_person_entities")
         .AppendLine("   from activity_entity_links al")
         .AppendLine("   join entities p on p.id = al.entity_id")
         .AppendLine("   where al.activity_id = a.id")
         .AppendLine(
            "      and p.entity_type_id in ('Person', 'NationalTeam', 'Pair')"
         )
         .AppendLine(") rp on true")
         .AppendLine("left join lateral (")
         .AppendLine("   select string_agg(")
         .AppendLine("      distinct entity.canonical_name,")
         .AppendLine("      ', ' order by entity.canonical_name")
         .AppendLine("   ) as related_organization_entities")
         .AppendLine("   from activity_entity_links al")
         .AppendLine("   join entities p on p.id = al.entity_id")
         .AppendLine("   join entity_to_entity_links el")
         .AppendLine("      on el.source_entity_id = p.id")
         .AppendLine("      or el.target_entity_id = p.id")
         .AppendLine("   join entities entity")
         .AppendLine("      on entity.id = case")
         .AppendLine("         when el.source_entity_id = p.id")
         .AppendLine("            then el.target_entity_id")
         .AppendLine("         else el.source_entity_id")
         .AppendLine("      end")
         .AppendLine("   where al.activity_id = a.id")
         .AppendLine("      and p.entity_type_id = 'Person'")
         .AppendLine(
            "      and entity.entity_type_id in ('Organization', 'Tour', 'League')"
         )
         .AppendLine(") ro on true")
         .AppendLine("where a.activity_date = @date");

      if(!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
      {
         sql.AppendLine("   and a.publication_status_id = @status");
      }

      if(normalizedSports.Count > 0)
      {
         sql.AppendLine("   and a.sport_id = any(@sport_ids)");
      }

      sql.AppendLine("group by a.id, at.label, s.id, s.name, s.icon_id,")
         .AppendLine(
            "         a.tv_channel_name, rp.related_person_entities,"
         )
         .AppendLine("         ro.related_organization_entities")
         .AppendLine("order by")
         .AppendLine("   a.activity_date,")
         .AppendLine("   a.local_start_time nulls last,")
         .AppendLine("   a.title");

      await using var command = dataSource.CreateCommand(sql.ToString());
      command.Parameters.AddWithValue("date", date);

      if(!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
      {
         command.Parameters.AddWithValue("status", status ?? "All");
      }

      if(normalizedSports.Count > 0)
      {
         command.Parameters.AddWithValue(
            "sport_ids",
            normalizedSports.ToArray()
         );
      }

      return await ReadActivityListAsync(command, cancellationToken);
   }

   public async Task<IReadOnlyList<ActivityListItem>> GetPublishedAsync(
      CancellationToken cancellationToken
   )
   {
      return await GetActivityListAsync(
         "where a.publication_status_id = 'Published'",
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

   public async Task<IReadOnlyList<ActivityListItem>> GetTodaysAsync(
      CancellationToken cancellationToken
   )
   {
      return await GetPublishedActivitiesAsync(
         SportDay.Today(DateTimeOffset.UtcNow),
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<ActivityListItem>> GetTomorrowsAsync(
      CancellationToken cancellationToken
   )
   {
      return await GetPublishedActivitiesAsync(
         SportDay.Tomorrow(DateTimeOffset.UtcNow),
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<ActivityListItem>> GetDraftsAsync(
      CancellationToken cancellationToken
   )
   {
      return await GetActivityListAsync(
         "where a.publication_status_id = 'Draft'",
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<ActivityListItem>> GetAllAsync(
      CancellationToken cancellationToken
   )
   {
      return await GetActivityListAsync(string.Empty, cancellationToken);
   }

   private async Task<IReadOnlyList<ActivityListItem>>
      GetPublishedActivitiesAsync(
         SportDayWindow window,
         CancellationToken cancellationToken
      )
   {
      var sql = $$"""
         select
            a.id,
            a.title,
            a.description,
            a.teaser,
            at.label,
            s.id,
            s.name,
            s.icon_id,
            a.activity_date,
            a.local_start_time,
            a.publication_status_id,
            a.tv_channel_name,
            coalesce(
               string_agg(
                  te.canonical_name,
                  ', ' order by te.canonical_name
               ),
               ''
            ) as entities,
            coalesce(rp.related_person_entities, '') as related_person_entities,
            coalesce(ro.related_organization_entities, '')
               as related_organization_entities
         from activities a
         join sports s on s.id = a.sport_id
         join activity_types at on at.id = a.activity_type_id
         left join activity_entity_links l on l.activity_id = a.id
         left join entities te on te.id = l.entity_id
         left join lateral (
            select string_agg(
               distinct p.canonical_name,
               ', ' order by p.canonical_name
            ) as related_person_entities
            from activity_entity_links al
            join entities p on p.id = al.entity_id
            where al.activity_id = a.id
               and p.entity_type_id in ('Person', 'NationalTeam', 'Pair')
         ) rp on true
         left join lateral (
            select string_agg(
               distinct entity.canonical_name,
               ', ' order by entity.canonical_name
            ) as related_organization_entities
            from activity_entity_links al
            join entities p on p.id = al.entity_id
            join entity_to_entity_links el
               on el.source_entity_id = p.id
               or el.target_entity_id = p.id
            join entities entity
               on entity.id = case
                  when el.source_entity_id = p.id
                     then el.target_entity_id
                  else el.source_entity_id
               end
            where al.activity_id = a.id
               and p.entity_type_id in ('Person', 'NationalTeam', 'Pair')
               and entity.entity_type_id in ('Organization', 'Tour', 'League')
         ) ro on true
         where a.publication_status_id = 'Published'
            and a.starts_at >= @start
            and a.starts_at < @end
         group by a.id, at.label, s.id, s.name, s.icon_id,
                  a.tv_channel_name, rp.related_person_entities,
                  ro.related_organization_entities
         order by
            a.activity_date,
            a.local_start_time nulls last,
            a.title
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "start",
         ToUtc(window.StartDate, window.Cutoff)
      );
      command.Parameters.AddWithValue(
         "end",
         ToUtc(window.EndDateExclusive, window.Cutoff)
      );

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
               ReadString(reader, 11),
               reader.GetString(10),
               reader.GetString(13),
               reader.GetString(14)
            )
         );
      }

      return activities;
   }

   public async Task<IReadOnlyList<EntityOption>> GetEntityOptionsAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            e.id,
            e.canonical_name,
            et.label,
            s.name,
            coalesce(org.organization_names, '')
         from entities e
         join entity_types et on et.id = e.entity_type_id
         join sports s on s.id = e.sport_id
         left join lateral (
            select string_agg(
               distinct linked.canonical_name,
               ', ' order by linked.canonical_name
            ) as organization_names
            from entity_to_entity_links l
            join entities linked on linked.id = case
               when l.source_entity_id = e.id then l.target_entity_id
               else l.source_entity_id
            end
            where (l.source_entity_id = e.id or l.target_entity_id = e.id)
               and e.entity_type_id = 'Person'
               and linked.entity_type_id = 'Organization'
         ) org on true
         order by e.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var entities = new List<EntityOption>();

      while (await reader.ReadAsync(cancellationToken))
      {
         entities.Add(
            new EntityOption(
               reader.GetGuid(0),
               reader.GetString(1),
               reader.GetString(2),
               reader.GetString(3),
               reader.GetString(4)
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

      if (!await reader.ReadAsync(cancellationToken))
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
         IsPublished = reader.GetString(9) == "Published",
         TvChannelName = ReadString(reader, 10),
         EvidenceUri = ReadString(reader, 11),
         EvidenceTitle = ReadString(reader, 12),
         EvidenceComment = ReadString(reader, 13)
      };

      await reader.DisposeAsync();

      const string linkSql = """
         select entity_id
         from activity_entity_links
         where activity_id = @id
         order by id
         """;

      await using var linkCommand = dataSource.CreateCommand(linkSql);
      linkCommand.Parameters.AddWithValue("id", id);
      await using var linkReader = await linkCommand.ExecuteReaderAsync(
         cancellationToken
      );

      while (await linkReader.ReadAsync(cancellationToken))
      {
         model.LinkedEntityIds.Add(linkReader.GetGuid(0));
      }

      return model;
   }

   public async Task<Guid> SaveAsync(
      ActivityEditModel model,
      CancellationToken cancellationToken
   )
   {
      var id = model.Id ?? Guid.NewGuid();
      var status = model.IsPublished ? "Published" : "Draft";
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

      if (model.Id is null)
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
      var sql = $$"""
         select
            a.id,
            a.title,
            a.description,
            a.teaser,
            at.label,
            s.id,
            s.name,
            s.icon_id,
            a.activity_date,
            a.local_start_time,
            a.publication_status_id,
            a.tv_channel_name,
            coalesce(
               string_agg(
                  te.canonical_name,
                  ', ' order by te.canonical_name
               ),
               ''
            ) as entities,
            coalesce(rp.related_person_entities, '') as related_person_entities,
            coalesce(ro.related_organization_entities, '')
               as related_organization_entities
         from activities a
         join sports s on s.id = a.sport_id
         join activity_types at on at.id = a.activity_type_id
         left join activity_entity_links l on l.activity_id = a.id
         left join entities te on te.id = l.entity_id
         left join lateral (
            select string_agg(
               distinct p.canonical_name,
               ', ' order by p.canonical_name
            ) as related_person_entities
            from activity_entity_links al
            join entities p on p.id = al.entity_id
            where al.activity_id = a.id
               and p.entity_type_id = 'Person'
         ) rp on true
         left join lateral (
            select string_agg(
               distinct entity.canonical_name,
               ', ' order by entity.canonical_name
            ) as related_organization_entities
            from activity_entity_links al
            join entities p on p.id = al.entity_id
            join entity_to_entity_links el
               on el.source_entity_id = p.id
               or el.target_entity_id = p.id
            join entities entity
               on entity.id = case
                  when el.source_entity_id = p.id
                     then el.target_entity_id
                  else el.source_entity_id
               end
            where al.activity_id = a.id
               and p.entity_type_id = 'Person'
               and entity.entity_type_id in ('Organization', 'Tour', 'League')
         ) ro on true
         {{whereClause}}
         group by a.id, at.label, s.id, s.name, s.icon_id,
                  a.tv_channel_name, rp.related_person_entities,
                  ro.related_organization_entities
         order by
            a.activity_date,
            a.local_start_time nulls last,
            a.title
         """;

      await using var command = dataSource.CreateCommand(sql);
      return await ReadActivityListAsync(command, cancellationToken);
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
      const string sql = """
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
               when @publication_status_id = 'Published' then now()
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
      const string sql = """
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
               when @publication_status_id = 'Published' then coalesce(
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

      if (distinctEntityIds.Count == 0)
      {
         return;
      }

      const string sql = """
         insert into activity_entity_links (id, activity_id, entity_id)
         values (@id, @activity_id, @entity_id)
         """;

      foreach (var entityId in distinctEntityIds)
      {
         await using var command = new NpgsqlCommand(
            sql,
            connection,
            transaction
         );
         command.Parameters.AddWithValue("id", Guid.NewGuid());
         command.Parameters.AddWithValue("activity_id", activityId);
         command.Parameters.AddWithValue("entity_id", entityId);
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

      if (
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
      if (model.ActivityDate is null || model.LocalStartTime is null)
      {
         return null;
      }

      var local = model.ActivityDate.Value.ToDateTime(
         model.LocalStartTime.Value
      );
      return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
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

      while (await reader.ReadAsync(cancellationToken))
      {
         options.Add(
            new LookupOption(reader.GetString(0), reader.GetString(1))
         );
      }

      return options;
   }

   private static (string Id, string Name) GetSource(string? uri)
   {
      if (
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

      for(var suffix = 2;; suffix++)
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

      foreach (var character in normalized)
      {
         var category = CharUnicodeInfo.GetUnicodeCategory(character);
         if (category != UnicodeCategory.NonSpacingMark)
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
               ReadString(reader, 11),
               reader.GetString(10),
               reader.GetString(13),
               reader.GetString(14)
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
      var local = date.ToDateTime(time);
      return new DateTimeOffset(
         local,
         TimeZoneInfo.Local.GetUtcOffset(local)
      ).ToUniversalTime();
   }

   private static string? GetSportIconPath(string? iconId)
   {
      if (string.IsNullOrWhiteSpace(iconId))
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
