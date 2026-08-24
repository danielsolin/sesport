using Npgsql;

using SESport.Core.Broadcast;
using SESport.Core.Configuration;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class EntityMutationRepository(NpgsqlDataSource dataSource)
{
   private static string? NormalizeNullable(string? value)
   {
      return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
   }

   private static void AddNullableParameter(
      NpgsqlCommand command,
      string name,
      string? value
   )
   {
      command.Parameters.AddWithValue(
         name,
         (object?)NormalizeNullable(value) ?? DBNull.Value
      );
   }
   public async Task ReplacePrimaryEntityImageAsync(
      Guid entityId,
      EntityImageReplacement replacement,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      await using var demoteCommand = new NpgsqlCommand(
         """
         update entity_images
         set is_primary = false,
             updated_at = now()
         where entity_id = @entity_id
            and is_primary
         """,
         connection,
         transaction
      );
      demoteCommand.Parameters.AddWithValue("entity_id", entityId);
      await demoteCommand.ExecuteNonQueryAsync(cancellationToken);

      await using var insertCommand = new NpgsqlCommand(
         """
         insert into entity_images (
            id,
            entity_id,
            image_data,
            mime_type,
            pixel_width,
            pixel_height,
            content_sha256,
            thumbnail_data,
            thumbnail_mime_type,
            thumbnail_pixel_width,
            thumbnail_pixel_height,
            thumbnail_content_sha256,
            thumbnail_source_media_url,
            source_kind,
            source_asset_id,
            source_url,
            source_media_url,
            source_title,
            creator_name,
            creator_url,
            license_name,
            license_url,
            copyright_notice,
            attribution_text,
            modification_description,
            review_status,
            review_note,
            reviewed_at,
            is_primary
         )
         values (
            @id,
            @entity_id,
            @image_data,
            @mime_type,
            @pixel_width,
            @pixel_height,
            @content_sha256,
            @thumbnail_data,
            @thumbnail_mime_type,
            @thumbnail_pixel_width,
            @thumbnail_pixel_height,
            @thumbnail_content_sha256,
            @thumbnail_source_media_url,
            @source_kind,
            @source_asset_id,
            @source_url,
            @source_media_url,
            @source_title,
            @creator_name,
            @creator_url,
            @license_name,
            @license_url,
            @copyright_notice,
            @attribution_text,
            @modification_description,
            @review_status,
            @review_note,
            now(),
            true
         )
         """,
         connection,
         transaction
      );
      insertCommand.Parameters.AddWithValue("id", Guid.NewGuid());
      insertCommand.Parameters.AddWithValue("entity_id", entityId);
      insertCommand.Parameters.AddWithValue(
         "image_data",
         replacement.ImageData
      );
      insertCommand.Parameters.AddWithValue(
         "mime_type",
         replacement.MimeType
      );
      insertCommand.Parameters.AddWithValue(
         "pixel_width",
         replacement.PixelWidth
      );
      insertCommand.Parameters.AddWithValue(
         "pixel_height",
         replacement.PixelHeight
      );
      insertCommand.Parameters.AddWithValue(
         "content_sha256",
         replacement.ContentSha256
      );
      insertCommand.Parameters.AddWithValue(
         "thumbnail_data",
         replacement.ThumbnailData
      );
      insertCommand.Parameters.AddWithValue(
         "thumbnail_mime_type",
         replacement.ThumbnailMimeType
      );
      insertCommand.Parameters.AddWithValue(
         "thumbnail_pixel_width",
         replacement.ThumbnailPixelWidth
      );
      insertCommand.Parameters.AddWithValue(
         "thumbnail_pixel_height",
         replacement.ThumbnailPixelHeight
      );
      insertCommand.Parameters.AddWithValue(
         "thumbnail_content_sha256",
         replacement.ThumbnailContentSha256
      );
      insertCommand.Parameters.AddWithValue(
         "thumbnail_source_media_url",
         replacement.ThumbnailSourceMediaUrl
      );
      insertCommand.Parameters.AddWithValue(
         "source_kind",
         EntityImageSourceKindIds.WikimediaCommons
      );
      insertCommand.Parameters.AddWithValue(
         "source_asset_id",
         replacement.SourceAssetId
      );
      insertCommand.Parameters.AddWithValue(
         "source_url",
         replacement.SourceUrl
      );
      insertCommand.Parameters.AddWithValue(
         "source_media_url",
         replacement.SourceMediaUrl
      );
      insertCommand.Parameters.AddWithValue(
         "source_title",
         replacement.SourceTitle
      );
      AddNullableParameter(
         insertCommand,
         "creator_name",
         replacement.CreatorName
      );
      AddNullableParameter(
         insertCommand,
         "creator_url",
         replacement.CreatorUrl
      );
      insertCommand.Parameters.AddWithValue(
         "license_name",
         replacement.LicenseName
      );
      AddNullableParameter(
         insertCommand,
         "license_url",
         replacement.LicenseUrl
      );
      AddNullableParameter(
         insertCommand,
         "copyright_notice",
         replacement.CopyrightNotice
      );
      insertCommand.Parameters.AddWithValue(
         "attribution_text",
         replacement.AttributionText
      );
      insertCommand.Parameters.AddWithValue(
         "modification_description",
         replacement.ModificationDescription
      );
      insertCommand.Parameters.AddWithValue(
         "review_status",
         EntityImageReviewStatusIds.Approved
      );
      insertCommand.Parameters.AddWithValue(
         "review_note",
         replacement.ReviewNote
      );

      await insertCommand.ExecuteNonQueryAsync(cancellationToken);
      await transaction.CommitAsync(cancellationToken);
   }

   public async Task DeletePrimaryEntityImageAsync(
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      await using var command = dataSource.CreateCommand(
         """
         delete from entity_images
         where entity_id = @entity_id
            and is_primary
         """
      );
      command.Parameters.AddWithValue("entity_id", entityId);

      await command.ExecuteNonQueryAsync(cancellationToken);
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

   private static string BuildEntityInsertSql(bool includePersonGender)
   {
      return includePersonGender
         ? """
            insert into entities (
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
               person_gender_id
            )
            values (
               @id,
               @canonical_name,
               @entity_type_id,
               @sport_id,
               @country_id,
               @country_relevance_kind_id,
               @country_relevance_reason,
               @watch_priority_id,
               @expected_stability_id,
               @alias_name,
               @bio,
               @birthdate,
               @height,
               @weight,
               @formative_club,
               @person_gender_id
            )
            """
         : """
            insert into entities (
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
               formative_club
            )
            values (
               @id,
               @canonical_name,
               @entity_type_id,
               @sport_id,
               @country_id,
               @country_relevance_kind_id,
               @country_relevance_reason,
               @watch_priority_id,
               @expected_stability_id,
               @alias_name,
               @bio,
               @birthdate,
               @height,
               @weight,
               @formative_club
            )
            """;
   }

   private static string BuildEntityUpdateSql(bool includePersonGender)
   {
      return includePersonGender
         ? """
            update entities
            set
               canonical_name = @canonical_name,
               entity_type_id = @entity_type_id,
               sport_id = @sport_id,
               country_id = @country_id,
               country_relevance_kind_id = @country_relevance_kind_id,
               country_relevance_reason = @country_relevance_reason,
               watch_priority_id = @watch_priority_id,
               expected_stability_id = @expected_stability_id,
               alias_name = @alias_name,
               bio = @bio,
               birthdate = @birthdate,
               height = @height,
               weight = @weight,
               formative_club = @formative_club,
               person_gender_id = @person_gender_id,
               updated_at = now()
            where id = @id
            """
         : """
            update entities
            set
               canonical_name = @canonical_name,
               entity_type_id = @entity_type_id,
               sport_id = @sport_id,
               country_id = @country_id,
               country_relevance_kind_id = @country_relevance_kind_id,
               country_relevance_reason = @country_relevance_reason,
               watch_priority_id = @watch_priority_id,
               expected_stability_id = @expected_stability_id,
               alias_name = @alias_name,
               bio = @bio,
               birthdate = @birthdate,
               height = @height,
               weight = @weight,
               formative_club = @formative_club,
               updated_at = now()
            where id = @id
            """;
   }

   public async Task SaveEntityAsync(
      EntityEditModel model,
      CancellationToken cancellationToken
   )
   {
      var isNew = model.Id is null;
      var id = model.Id ?? Guid.NewGuid();
      var includePersonGender = await HasPersonGenderColumnAsync(
         cancellationToken
      );
      var sql = isNew
         ? BuildEntityInsertSql(includePersonGender)
         : BuildEntityUpdateSql(includePersonGender);

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("id", id);
      AddEntityParameters(command, model);
      await command.ExecuteNonQueryAsync(cancellationToken);

      if(isNew)
      {
         await SaveEntityLinksAsync(
            connection,
            transaction,
            id,
            model.LinkedEntityIds,
            cancellationToken
         );
      }

      await transaction.CommitAsync(cancellationToken);
      model.Id = id;
   }

   public async Task<bool> UpdateEntityBioAsync(
      Guid entityId,
      string bio,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update entities
         set bio = @bio,
             updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue("bio", bio.Trim());

      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }

   public async Task<bool> UpdateEntityPersonFactsAsync(
      Guid entityId,
      DateOnly? birthdate,
      int? height,
      int? weight,
      string? formativeClub,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update entities
         set birthdate = coalesce(birthdate, @birthdate),
             height = coalesce(height, @height),
             weight = coalesce(weight, @weight),
             formative_club = coalesce(
                formative_club,
                @formative_club
             ),
             updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", entityId);
      command.Parameters.AddWithValue(
         "birthdate",
         (object?)birthdate ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "height",
         (object?)height ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "weight",
         (object?)weight ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "formative_club",
         (object?)NormalizeNullable(formativeClub) ?? DBNull.Value
      );

      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }

   public async Task<bool> AddEntityLinkAsync(
      Guid sourceEntityId,
      Guid targetEntityId,
      CancellationToken cancellationToken
   )
   {
      if(sourceEntityId == targetEntityId)
      {
         return false;
      }

      const string sql = """
         insert into entity_to_entity_links (
            id,
            source_entity_id,
            target_entity_id
         )
         values (
            md5(@source_entity_id::text || @target_entity_id::text)::uuid,
            @source_entity_id,
            @target_entity_id
         )
         on conflict do nothing
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("source_entity_id", sourceEntityId);
      command.Parameters.AddWithValue("target_entity_id", targetEntityId);

      try
      {
         return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
      }
      catch(PostgresException exception)
         when(exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
      {
         throw new EntityLinkEntityNotFoundException(
            sourceEntityId,
            targetEntityId,
            exception
         );
      }
   }

   public async Task EnsureEntityLinksAsync(
      IReadOnlyCollection<Guid> sourceEntityIds,
      Guid targetEntityId,
      CancellationToken cancellationToken
   )
   {
      var normalizedSourceEntityIds = sourceEntityIds
         .Where(entityId =>
            entityId != Guid.Empty && entityId != targetEntityId)
         .Distinct()
         .ToArray();

      if(normalizedSourceEntityIds.Length == 0 ||
         targetEntityId == Guid.Empty)
      {
         return;
      }

      const string sql = $$"""
         insert into entity_to_entity_links (
            id,
            source_entity_id,
            target_entity_id
         )
         select
            md5(source_entity.id::text || @target_entity_id::text)::uuid,
            source_entity.id,
            @target_entity_id
         from unnest(@source_entity_ids) as source_entity_id
         join entities source_entity
            on source_entity.id = source_entity_id
         where source_entity.entity_type_id in (
            '{{TrackedEntityTypeIds.Person}}',
            '{{TrackedEntityTypeIds.Pair}}'
         )
         on conflict do nothing
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "source_entity_ids",
         normalizedSourceEntityIds
      );
      command.Parameters.AddWithValue("target_entity_id", targetEntityId);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<bool> RemoveEntityLinkAsync(
      Guid sourceEntityId,
      Guid targetEntityId,
      CancellationToken cancellationToken
   )
   {
      if(sourceEntityId == targetEntityId)
      {
         return false;
      }

      const string sql = """
         delete from entity_to_entity_links
         where (
               source_entity_id = @source_entity_id
               and target_entity_id = @target_entity_id
            )
            or (
               source_entity_id = @target_entity_id
               and target_entity_id = @source_entity_id
            )
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("source_entity_id", sourceEntityId);
      command.Parameters.AddWithValue("target_entity_id", targetEntityId);

      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }

   public async Task DeleteEntityAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from sources
         where correlation_type = @correlation_type
            and correlation_id = @correlation_id
         """;
      await using var sourceCommand = dataSource.CreateCommand(sql);
      sourceCommand.Parameters.AddWithValue(
         "correlation_type",
         SourceCorrelationTypes.Entity
      );
      sourceCommand.Parameters.AddWithValue(
         "correlation_id",
         id.ToString()
      );
      await sourceCommand.ExecuteNonQueryAsync(cancellationToken);

      const string deleteEntitySql =
         "delete from entities where id = @id";
      await using var command = dataSource.CreateCommand(deleteEntitySql);
      command.Parameters.AddWithValue("id", id);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<bool> UpdateEntityWatchPriorityAsync(
      Guid id,
      string watchPriorityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update entities
         set
            watch_priority_id = @watch_priority_id,
            updated_at = now()
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue(
         "watch_priority_id",
         watchPriorityId
      );

      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }

   private static void AddEntityParameters(
      NpgsqlCommand command,
      EntityEditModel model
   )
   {
      command.Parameters.AddWithValue(
         "canonical_name",
         model.CanonicalName.Trim()
      );
      command.Parameters.AddWithValue("entity_type_id", model.EntityTypeId);
      command.Parameters.AddWithValue("sport_id", model.SportId);
      command.Parameters.AddWithValue("country_id", model.CountryId.Trim());
      command.Parameters.AddWithValue(
         "country_relevance_kind_id",
         model.CountryRelevanceKindId
      );
      command.Parameters.AddWithValue(
         "country_relevance_reason",
         model.CountryRelevanceReason.Trim()
      );
      command.Parameters.AddWithValue(
         "watch_priority_id",
         model.WatchPriorityId
      );
      command.Parameters.AddWithValue(
         "expected_stability_id",
         model.ExpectedStabilityId
      );
      command.Parameters.AddWithValue(
         "alias_name",
         (object?)NormalizeAliasName(model.AliasName) ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "bio",
         (object?)NormalizeBio(model.Bio) ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "birthdate",
         (object?)model.Birthdate ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "height",
         (object?)model.Height ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "weight",
         (object?)model.Weight ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "formative_club",
         (object?)NormalizeNullable(model.FormativeClub) ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "person_gender_id",
         (object?)NormalizePersonGenderId(model) ?? DBNull.Value
      );
   }

   private static string? NormalizeAliasName(string? aliasName)
   {
      return string.IsNullOrWhiteSpace(aliasName)
         ? null
         : aliasName.Trim();
   }

   private static string? NormalizeBio(string? bio)
   {
      return string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
   }

   private static string? NormalizePersonGenderId(EntityEditModel model)
   {
      if(!string.Equals(
         model.EntityTypeId,
         TrackedEntityTypeIds.Person,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return null;
      }

      return string.IsNullOrWhiteSpace(model.PersonGenderId)
         ? null
         : model.PersonGenderId.Trim();
   }

   private static async Task SaveEntityLinksAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid sourceEntityId,
      IEnumerable<Guid>? targetEntityIds,
      CancellationToken cancellationToken
   )
   {
      const string deleteSql = """
         delete from entity_to_entity_links
         where source_entity_id = @source_entity_id
            or target_entity_id = @source_entity_id
         """;

      await using(var deleteCommand = new NpgsqlCommand(
         deleteSql,
         connection,
         transaction
      ))
      {
         deleteCommand.Parameters.AddWithValue(
            "source_entity_id",
            sourceEntityId
         );
         await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
      }

      const string insertSql = """
         insert into entity_to_entity_links (
            id,
            source_entity_id,
            target_entity_id
         )
         values (
            md5(@source_entity_id::text || @target_entity_id::text)::uuid,
            @source_entity_id,
            @target_entity_id
         )
         on conflict do nothing
         """;

      foreach(var targetEntityId in (targetEntityIds ?? []).Distinct())
      {
         if(targetEntityId == sourceEntityId)
         {
            continue;
         }

         await using var insertCommand = new NpgsqlCommand(
            insertSql,
            connection,
            transaction
         );
         insertCommand.Parameters.AddWithValue(
            "source_entity_id",
            sourceEntityId
         );
         insertCommand.Parameters.AddWithValue(
            "target_entity_id",
            targetEntityId
         );
         await insertCommand.ExecuteNonQueryAsync(cancellationToken);
      }
   }

}
