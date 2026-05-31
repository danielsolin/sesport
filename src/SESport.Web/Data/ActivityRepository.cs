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
         "where a.publication_status = 'Published'",
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
         select id, canonical_name, entity_type
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
            a.activity_type,
            a.sport_id,
            a.sport_name,
            a.context,
            a.time_kind,
            a.starts_at,
            a.starts_on,
            a.ends_on,
            a.time_description,
            a.country_relevance_explanation,
            a.publication_status,
            a.slug,
            l.entity_id,
            l.role,
            l.explanation,
            l.context_name,
            e.source_name,
            e.uri,
            e.title,
            e.summary
         from activities a
         left join lateral (
            select entity_id, role, explanation, context_name
            from activity_entity_links
            where activity_id = a.id
            order by id
            limit 1
         ) l on true
         left join lateral (
            select source_name, uri, title, summary
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

      var startsAt = reader.IsDBNull(8)
         ? (DateTimeOffset?)null
         : reader.GetFieldValue<DateTimeOffset>(8);

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
         StartsAtLocal = startsAt?.LocalDateTime.ToString("yyyy-MM-ddTHH:mm"),
         StartsOn = ReadDateOnly(reader, 9),
         EndsOn = ReadDateOnly(reader, 10),
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
      var startsAt = ParseStartsAt(model);
      var slug = NormalizeSlug(model.Slug, model.Title);

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
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
            a.activity_type,
            a.sport_name,
            a.context,
            a.time_kind,
            a.starts_at,
            a.starts_on,
            a.ends_on,
            a.time_description,
            a.country_relevance_explanation,
            a.publication_status,
            a.slug,
            coalesce(string_agg(te.canonical_name, ', '), '') as entities
         from activities a
         left join activity_entity_links l on l.activity_id = a.id
         left join tracked_entities te on te.id = l.entity_id
         {{whereClause}}
         group by a.id
         order by
            coalesce(a.starts_at, a.starts_on::timestamptz, a.created_at),
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
               reader.GetString(11),
               reader.GetString(12),
               ReadString(reader, 13),
               reader.GetString(14)
            )
         );
      }

      return activities;
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
            activity_type,
            sport_id,
            sport_name,
            context,
            time_kind,
            starts_at,
            starts_on,
            ends_on,
            time_description,
            country_relevance_explanation,
            publication_status,
            slug,
            published_at
         )
         values (
            @id,
            @title,
            @description,
            @activity_type,
            @sport_id,
            @sport_name,
            @context,
            @time_kind,
            @starts_at,
            @starts_on,
            @ends_on,
            @time_description,
            @country_relevance_explanation,
            @publication_status,
            @slug,
            case when @publication_status = 'Published' then now() else null end
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
            activity_type = @activity_type,
            sport_id = @sport_id,
            sport_name = @sport_name,
            context = @context,
            time_kind = @time_kind,
            starts_at = @starts_at,
            starts_on = @starts_on,
            ends_on = @ends_on,
            time_description = @time_description,
            country_relevance_explanation = @country_relevance_explanation,
            publication_status = @publication_status,
            slug = @slug,
            published_at = case
               when @publication_status = 'Published' then coalesce(
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
            role,
            explanation,
            context_name
         )
         values (
            @id,
            @activity_id,
            @entity_id,
            @role,
            @explanation,
            @context_name
         )
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", model.EntityId.Value);
      command.Parameters.AddWithValue("role", model.EntityRole);
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
            source_name,
            uri,
            title,
            observed_at,
            summary
         )
         values (
            @id,
            @activity_id,
            @source_id,
            @source_name,
            @uri,
            @title,
            now(),
            @summary
         )
         """;

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue(
         "source_id",
         Slugify(model.EvidenceSourceName)
      );
      command.Parameters.AddWithValue("source_name", model.EvidenceSourceName);
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
      command.Parameters.AddWithValue("description", BlankToDbNull(model.Description));
      command.Parameters.AddWithValue("activity_type", model.ActivityType);
      command.Parameters.AddWithValue("sport_id", model.SportId.Trim());
      command.Parameters.AddWithValue("sport_name", model.SportName.Trim());
      command.Parameters.AddWithValue("context", BlankToDbNull(model.Context));
      command.Parameters.AddWithValue("time_kind", model.TimeKind);
      command.Parameters.AddWithValue(
         "starts_at",
         startsAt?.ToUniversalTime() ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue(
         "starts_on",
         model.StartsOn ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue(
         "ends_on",
         model.EndsOn ?? (object)DBNull.Value
      );
      command.Parameters.AddWithValue(
         "time_description",
         BlankToDbNull(model.TimeDescription)
      );
      command.Parameters.AddWithValue(
         "country_relevance_explanation",
         model.CountryRelevanceExplanation.Trim()
      );
      command.Parameters.AddWithValue("publication_status", status);
      command.Parameters.AddWithValue("slug", slug ?? (object)DBNull.Value);
   }

   private static DateTimeOffset? ParseStartsAt(ActivityEditModel model)
   {
      if (model.TimeKind != "ExactStart")
      {
         return null;
      }

      if (string.IsNullOrWhiteSpace(model.StartsAtLocal))
      {
         return null;
      }

      var local = DateTime.Parse(model.StartsAtLocal);
      return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
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
      var description = ReadString(reader, 10);

      if (kind == "ExactStart" && !reader.IsDBNull(7))
      {
         return reader.GetFieldValue<DateTimeOffset>(7)
            .LocalDateTime
            .ToString("yyyy-MM-dd HH:mm");
      }

      if (kind == "DateRange" && !reader.IsDBNull(8) && !reader.IsDBNull(9))
      {
         return $"{reader.GetFieldValue<DateOnly>(8):yyyy-MM-dd} - " +
            $"{reader.GetFieldValue<DateOnly>(9):yyyy-MM-dd}";
      }

      return string.IsNullOrWhiteSpace(description) ? "TBD" : description;
   }

   private static string? ReadString(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static DateOnly? ReadDateOnly(NpgsqlDataReader reader, int ordinal)
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetFieldValue<DateOnly>(ordinal);
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
