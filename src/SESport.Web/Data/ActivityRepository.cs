using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;

namespace SESport.Web.Data;

public sealed class ActivityRepository(NpgsqlDataSource dataSource)
{
   public async Task<IReadOnlyList<ActivityListItem>> GetPublishedAsync(
      CancellationToken cancellationToken
   )
   {
      return await GetActivityListAsync(
         "where a.publication_status_id = 'Published'",
         cancellationToken
      );
   }

   public async Task<IReadOnlyList<ActivityListItem>> GetAdminListAsync(
      CancellationToken cancellationToken
   )
   {
      return await GetActivityListAsync(string.Empty, cancellationToken);
   }

   public async Task<IReadOnlyList<EntityOption>> GetEntityOptionsAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select id, canonical_name, entity_type_id
         from tracked_entities
         order by canonical_name
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
               reader.GetString(2)
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

   public async Task<IReadOnlyList<LookupOption>> GetEntityRoleOptionsAsync(
      CancellationToken cancellationToken
   )
   {
      return await GetLookupOptionsAsync(
         "select id, label from activity_entity_link_roles order by sort_order, label",
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
            a.activity_type_id,
            a.sport_id,
            s.name as sport_name,
            a.context,
            a.time_kind_id,
            a.activity_date,
            a.local_start_time,
            a.time_zone_id,
            a.time_description,
            a.country_relevance_explanation,
            a.publication_status_id,
            a.slug,
            l.entity_id,
            l.role_id,
            l.explanation,
            l.context_name,
            e.source_name,
            e.uri,
            e.title,
            e.summary
         from activities a
         join sports s on s.id = a.sport_id
         left join lateral (
            select entity_id, role_id, explanation, context_name
            from activity_entity_links
            where activity_id = a.id
            order by id
            limit 1
         ) l on true
         left join lateral (
            select src.name as source_name, e.uri, e.title, e.summary
            from activity_evidence e
            join sources src on src.id = e.source_id
            where e.activity_id = a.id
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

      return new ActivityEditModel
      {
         Id = reader.GetGuid(0),
         Title = reader.GetString(1),
         Description = ReadString(reader, 2),
         ActivityType = reader.GetString(3),
         SportId = reader.GetString(4),
         SportName = reader.GetString(5),
         Context = ReadString(reader, 6),
         TimeKind = reader.GetString(7),
         ActivityDate = reader.GetFieldValue<DateOnly>(8),
         LocalStartTime = ReadTimeOnly(reader, 9),
         TimeZoneId = reader.GetString(10),
         TimeDescription = ReadString(reader, 11),
         CountryRelevanceExplanation = reader.GetString(12),
         IsPublished = reader.GetString(13) == "Published",
         Slug = ReadString(reader, 14),
         EntityId = reader.IsDBNull(15) ? null : reader.GetGuid(15),
         EntityRole = ReadString(reader, 16) ?? "CompetesIn",
         EntityExplanation = ReadString(reader, 17),
         EntityContextName = ReadString(reader, 18),
         EvidenceSourceName = ReadString(reader, 19),
         EvidenceUri = ReadString(reader, 20),
         EvidenceTitle = ReadString(reader, 21),
         EvidenceSummary = ReadString(reader, 22)
      };
   }

   public async Task<Guid> SaveAsync(
      ActivityEditModel model,
      CancellationToken cancellationToken
   )
   {
      var id = model.Id ?? Guid.NewGuid();
      var status = model.IsPublished ? "Published" : "Draft";
      var startsAt = GetStartsAt(model);
      var slug = NormalizeSlug(model.Slug, model.Title);

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      await EnsureSportAsync(connection, transaction, model, cancellationToken);

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
         model,
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
            at.label,
            s.name,
            a.context,
            a.time_kind_id,
            a.activity_date,
            a.local_start_time,
            a.time_description,
            a.country_relevance_explanation,
            a.publication_status_id,
            a.slug,
            coalesce(string_agg(te.canonical_name, ', '), '') as entities
         from activities a
         join sports s on s.id = a.sport_id
         join activity_types at on at.id = a.activity_type_id
         left join activity_entity_links l on l.activity_id = a.id
         left join tracked_entities te on te.id = l.entity_id
         {{whereClause}}
         group by a.id, at.label, s.name
         order by
            a.activity_date,
            a.local_start_time nulls last,
            a.title
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var activities = new List<ActivityListItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         activities.Add(
            new ActivityListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               ReadString(reader, 2),
               reader.GetString(3),
               reader.GetString(4),
               ReadString(reader, 5),
               FormatTime(reader),
               reader.GetString(10),
               reader.GetString(11),
               ReadString(reader, 12),
               reader.GetString(13)
            )
         );
      }

      return activities;
   }

   private static async Task EnsureSportAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      ActivityEditModel model,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into sports (id, name)
         values (@id, @name)
         on conflict (id) do update
         set name = excluded.name
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", model.SportId.Trim());
      command.Parameters.AddWithValue("name", model.SportName.Trim());
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task InsertActivityAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid id,
      ActivityEditModel model,
      DateTimeOffset? startsAt,
      string status,
      string? slug,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         insert into activities (
            id,
            title,
            description,
            activity_type_id,
            sport_id,
            context,
            time_kind_id,
            activity_date,
            local_start_time,
            starts_at,
            time_zone_id,
            time_description,
            country_relevance_explanation,
            publication_status_id,
            slug,
            published_at
         )
         values (
            @id,
            @title,
            @description,
            @activity_type_id,
            @sport_id,
            @context,
            @time_kind_id,
            @activity_date,
            @local_start_time,
            @starts_at,
            @time_zone_id,
            @time_description,
            @country_relevance_explanation,
            @publication_status_id,
            @slug,
            case when @publication_status_id = 'Published' then now() else null end
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
      string? slug,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update activities
         set
            title = @title,
            description = @description,
            activity_type_id = @activity_type_id,
            sport_id = @sport_id,
            context = @context,
            time_kind_id = @time_kind_id,
            activity_date = @activity_date,
            local_start_time = @local_start_time,
            starts_at = @starts_at,
            time_zone_id = @time_zone_id,
            time_description = @time_description,
            country_relevance_explanation = @country_relevance_explanation,
            publication_status_id = @publication_status_id,
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
      ActivityEditModel model,
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

      if (model.EntityId is null)
      {
         return;
      }

      const string sql = """
         insert into activity_entity_links (
            id,
            activity_id,
            entity_id,
            role_id,
            explanation,
            context_name
         )
         values (
            @id,
            @activity_id,
            @entity_id,
            @role_id,
            @explanation,
            @context_name
         )
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", model.EntityId.Value);
      command.Parameters.AddWithValue("role_id", model.EntityRole);
      command.Parameters.AddWithValue(
         "explanation",
         BlankToNullable(model.EntityExplanation) ??
         model.CountryRelevanceExplanation
      );
      command.Parameters.AddWithValue(
         "context_name",
         BlankToDbNull(model.EntityContextName)
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
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

      if (string.IsNullOrWhiteSpace(model.EvidenceSourceName))
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
            summary
         )
         values (
            @id,
            @activity_id,
            @source_id,
            @uri,
            @title,
            now(),
            @summary
         )
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      var sourceId = Slugify(model.EvidenceSourceName);
      await EnsureSourceAsync(
         connection,
         transaction,
         sourceId,
         model.EvidenceSourceName,
         cancellationToken
      );
      command.Parameters.AddWithValue("source_id", sourceId);
      command.Parameters.AddWithValue("uri", BlankToDbNull(model.EvidenceUri));
      command.Parameters.AddWithValue(
         "title",
         BlankToDbNull(model.EvidenceTitle)
      );
      command.Parameters.AddWithValue(
         "summary",
         BlankToNullable(model.EvidenceSummary) ?? model.EvidenceSourceName
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
      command.Parameters.AddWithValue("name", sourceName.Trim());
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static void AddActivityParameters(
      NpgsqlCommand command,
      Guid id,
      ActivityEditModel model,
      DateTimeOffset? startsAt,
      string status,
      string? slug
   )
   {
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("title", model.Title.Trim());
      command.Parameters.AddWithValue(
         "description",
         BlankToDbNull(model.Description)
      );
      command.Parameters.AddWithValue("activity_type_id", model.ActivityType);
      command.Parameters.AddWithValue("sport_id", model.SportId.Trim());
      command.Parameters.AddWithValue("context", BlankToDbNull(model.Context));
      command.Parameters.AddWithValue("time_kind_id", model.TimeKind);
      command.Parameters.AddWithValue("activity_date", model.ActivityDate!.Value);
      command.Parameters.AddWithValue(
         "local_start_time",
         model.LocalStartTime ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue(
         "starts_at",
         startsAt?.ToUniversalTime() ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue("time_zone_id", model.TimeZoneId.Trim());
      command.Parameters.AddWithValue(
         "time_description",
         BlankToDbNull(model.TimeDescription)
      );
      command.Parameters.AddWithValue(
         "country_relevance_explanation",
         model.CountryRelevanceExplanation.Trim()
      );
      command.Parameters.AddWithValue("publication_status_id", status);
      command.Parameters.AddWithValue("slug", slug ?? (object)DBNull.Value);
   }

   private static DateTimeOffset? GetStartsAt(ActivityEditModel model)
   {
      if (model.TimeKind != "Scheduled")
      {
         return null;
      }

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
         options.Add(new LookupOption(reader.GetString(0), reader.GetString(1)));
      }

      return options;
   }

   private static string? NormalizeSlug(string? slug, string title)
   {
      var value = string.IsNullOrWhiteSpace(slug) ? title : slug;
      return string.IsNullOrWhiteSpace(value) ? null : Slugify(value);
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
            builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant(),
            "[^a-z0-9]+",
            "-"
         )
         .Trim('-');
   }

   private static string FormatTime(NpgsqlDataReader reader)
   {
      var kind = reader.GetString(6);
      var activityDate = reader.GetFieldValue<DateOnly>(7);
      var localStartTime = ReadTimeOnly(reader, 8);
      var description = ReadString(reader, 9);

      if (kind == "Scheduled" && localStartTime is not null)
      {
         return $"{activityDate:yyyy-MM-dd} {localStartTime:HH:mm}";
      }

      return string.IsNullOrWhiteSpace(description)
         ? $"{activityDate:yyyy-MM-dd}"
         : $"{activityDate:yyyy-MM-dd} · {description}";
   }

   private static string? ReadString(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static TimeOnly? ReadTimeOnly(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetFieldValue<TimeOnly>(ordinal);
   }

   private static object BlankToDbNull(string? value)
   {
      return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
   }

   private static string? BlankToNullable(string? value)
   {
      return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
   }
}
