using Npgsql;

using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class EntityMergeRepository(NpgsqlDataSource dataSource)
{
   public async Task<EntityMergePreview?> GetEntityMergePreviewAsync(
      Guid sourceEntityId,
      Guid targetEntityId,
      CancellationToken cancellationToken
   )
   {
      if(sourceEntityId == targetEntityId)
      {
         return null;
      }

      var source = await GetEntityMergeSummaryAsync(
         sourceEntityId,
         cancellationToken
      );
      var target = await GetEntityMergeSummaryAsync(
         targetEntityId,
         cancellationToken
      );

      if(source is null || target is null)
      {
         return null;
      }

      var counts = await GetEntityMergeReferenceCountsAsync(
         sourceEntityId,
         cancellationToken
      );
      var links = await GetEntityMergeLinkPreviewsAsync(
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );

      return new EntityMergePreview(source, target, counts, links);
   }

   public async Task<EntityMergeResult> MergeEntityAsync(
      Guid sourceEntityId,
      Guid targetEntityId,
      CancellationToken cancellationToken
   )
   {
      if(sourceEntityId == targetEntityId)
      {
         throw new InvalidOperationException(
            "Source and target entity must be different."
         );
      }

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );

      var source = await GetEntityMergeSummaryAsync(
         connection,
         transaction,
         sourceEntityId,
         true,
         cancellationToken
      ) ?? throw new InvalidOperationException("Source entity was not found.");
      var target = await GetEntityMergeSummaryAsync(
         connection,
         transaction,
         targetEntityId,
         true,
         cancellationToken
      ) ?? throw new InvalidOperationException("Target entity was not found.");

      if(!string.Equals(
         source.EntityTypeId,
         target.EntityTypeId,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         throw new InvalidOperationException(
            "Source and target entity must have the same entity type."
         );
      }

      var activityLinksMoved = await ExecuteMergeCommandAsync(
         connection,
         transaction,
         """
         update activity_entity_links
         set entity_id = @target_entity_id
         where entity_id = @source_entity_id
         """,
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );
      var activityOrganizationLinksMoved =
         await ExecuteMergeCommandAsync(
            connection,
            transaction,
            """
            update activities
            set organization_entity_id = @target_entity_id
            where organization_entity_id = @source_entity_id
            """,
            sourceEntityId,
            targetEntityId,
            cancellationToken
         );
      activityOrganizationLinksMoved +=
         await ExecuteMergeCommandAsync(
            connection,
            transaction,
            """
            update activity_entity_links
            set organization_entity_id = @target_entity_id
            where organization_entity_id = @source_entity_id
            """,
            sourceEntityId,
            targetEntityId,
            cancellationToken
         );
      var broadcastsMoved = await ExecuteMergeCommandAsync(
         connection,
         transaction,
         """
         update broadcasts
         set entity_id = @target_entity_id,
             updated_at = now()
         where entity_id = @source_entity_id
         """,
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );
      var sourceReferenceSql = $"""
         update sources
         set correlation_id = @target_entity_id::text
         where correlation_type = '{SourceCorrelationTypes.Entity}'
            and correlation_id = @source_entity_id::text
         """;
      await ExecuteMergeCommandAsync(
         connection,
         transaction,
         sourceReferenceSql,
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );
      var duplicateEntityLinksDeleted =
         await DeleteDuplicateEntityLinksAsync(
            connection,
            transaction,
            sourceEntityId,
            targetEntityId,
            cancellationToken
         );
      var entityLinksMoved = await MoveEntityLinksAsync(
         connection,
         transaction,
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );
      var duplicateActivityLinksDeleted =
         await DeleteDuplicateActivityEntityLinksAsync(
            connection,
            transaction,
            targetEntityId,
            cancellationToken
         );

      await ExecuteMergeCommandAsync(
         connection,
         transaction,
         """
         delete from entities
         where id = @source_entity_id
         """,
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );

      await transaction.CommitAsync(cancellationToken);

      return new EntityMergeResult(
         activityLinksMoved,
         activityOrganizationLinksMoved,
         broadcastsMoved,
         duplicateActivityLinksDeleted,
         duplicateEntityLinksDeleted,
         entityLinksMoved
      );
   }

   private async Task<EntityMergeEntitySummary?> GetEntityMergeSummaryAsync(
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );

      return await GetEntityMergeSummaryAsync(
         connection,
         null,
         entityId,
         false,
         cancellationToken
      );
   }

   private static async Task<EntityMergeEntitySummary?>
      GetEntityMergeSummaryAsync(
         NpgsqlConnection connection,
         NpgsqlTransaction? transaction,
         Guid entityId,
         bool lockRow,
         CancellationToken cancellationToken
      )
   {
      var sql = """
         select
            id,
            canonical_name,
            entity_type_id,
            sport_id,
            country_id,
            watch_priority_id,
            expected_stability_id,
            person_gender_id,
            alias_name
         from entities
         where id = @id
         """ + (lockRow ? "\nfor update" : "");

      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("id", entityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return new EntityMergeEntitySummary(
         reader.GetGuid(0),
         reader.GetString(1),
         reader.GetString(2),
         reader.GetString(3),
         reader.GetString(4),
         reader.GetString(5),
         reader.GetString(6),
         reader.IsDBNull(7) ? null : reader.GetString(7),
         reader.IsDBNull(8) ? null : reader.GetString(8)
      );
   }

   private async Task<IReadOnlyList<EntityMergeReferenceCount>>
      GetEntityMergeReferenceCountsAsync(
         Guid sourceEntityId,
         CancellationToken cancellationToken
      )
   {
      var sql = $$"""
         select label, count
         from (
            select
               'Activity participants' as label,
               count(*)::int as count
            from activity_entity_links
            where entity_id = @source_entity_id
            union all
            select
               'Activity organizations',
               count(*)::int
            from activities a
            where {{ActivityRepository
               .GetActivityOrganizationEntityIdSql("a")}} =
               @source_entity_id
            union all
            select
               'Broadcasts',
               count(*)::int
            from broadcasts
            where entity_id = @source_entity_id
            union all
            select
               'Linked entities',
               count(*)::int
            from entity_to_entity_links
            where source_entity_id = @source_entity_id
               or target_entity_id = @source_entity_id
         ) counts
         order by label
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("source_entity_id", sourceEntityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var counts = new List<EntityMergeReferenceCount>();

      while(await reader.ReadAsync(cancellationToken))
      {
         counts.Add(
            new EntityMergeReferenceCount(
               reader.GetString(0),
               reader.GetInt32(1)
            )
         );
      }

      return counts;
   }

   private async Task<IReadOnlyList<EntityMergeLinkPreview>>
      GetEntityMergeLinkPreviewsAsync(
         Guid sourceEntityId,
         Guid targetEntityId,
         CancellationToken cancellationToken
      )
   {
      var sql = $$"""
         select
            related.canonical_name,
            related.entity_type_id,
            case
               when exists (
                  select 1
                  from entity_to_entity_links kept_link
                  where (
                     kept_link.source_entity_id = @target_entity_id
                     and kept_link.target_entity_id = related.id
                  ) or (
                     kept_link.target_entity_id = @target_entity_id
                     and kept_link.source_entity_id = related.id
                  )
               )
                  then 'Drop duplicate'
               else 'Move'
            end as action
         from entity_to_entity_links source_link
         join entities related
            on related.id =
               {{EntityQueryRepository.GetOtherSideEntityIdSql("@source_entity_id")}}
         where source_link.source_entity_id = @source_entity_id
            or source_link.target_entity_id = @source_entity_id
         order by related.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("source_entity_id", sourceEntityId);
      command.Parameters.AddWithValue("target_entity_id", targetEntityId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var links = new List<EntityMergeLinkPreview>();

      while(await reader.ReadAsync(cancellationToken))
      {
         links.Add(
            new EntityMergeLinkPreview(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2)
            )
         );
      }

      return links;
   }

   private static async Task<int> DeleteDuplicateEntityLinksAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid sourceEntityId,
      Guid targetEntityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from entity_to_entity_links old_link
         where (
               old_link.source_entity_id = @source_entity_id
               or old_link.target_entity_id = @source_entity_id
            )
            and exists (
               select 1
               from entity_to_entity_links kept_link
               where kept_link.id <> old_link.id
                  and (
                     (
                        kept_link.source_entity_id = @target_entity_id
                        and kept_link.target_entity_id = case
                           when old_link.source_entity_id = @source_entity_id
                              then old_link.target_entity_id
                           else old_link.source_entity_id
                        end
                     )
                     or (
                        kept_link.target_entity_id = @target_entity_id
                        and kept_link.source_entity_id = case
                           when old_link.source_entity_id = @source_entity_id
                              then old_link.target_entity_id
                           else old_link.source_entity_id
                        end
                     )
                  )
            )
         """;

      return await ExecuteMergeCommandAsync(
         connection,
         transaction,
         sql,
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );
   }

   private static async Task<int> MoveEntityLinksAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid sourceEntityId,
      Guid targetEntityId,
      CancellationToken cancellationToken
   )
   {
      var movedSourceLinks = await ExecuteMergeCommandAsync(
         connection,
         transaction,
         """
         update entity_to_entity_links
         set source_entity_id = @target_entity_id,
             updated_at = now()
         where source_entity_id = @source_entity_id
         """,
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );
      var movedTargetLinks = await ExecuteMergeCommandAsync(
         connection,
         transaction,
         """
         update entity_to_entity_links
         set target_entity_id = @target_entity_id,
             updated_at = now()
         where target_entity_id = @source_entity_id
         """,
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );
      await ExecuteMergeCommandAsync(
         connection,
         transaction,
         """
         delete from entity_to_entity_links
         where source_entity_id = target_entity_id
         """,
         sourceEntityId,
         targetEntityId,
         cancellationToken
      );

      return movedSourceLinks + movedTargetLinks;
   }

   private static async Task<int> DeleteDuplicateActivityEntityLinksAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid targetEntityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from activity_entity_links deleted_link
         using (
            select id
            from (
               select
                  id,
                  row_number() over (
                     partition by
                        activity_id,
                        entity_id,
                        organization_entity_id
                     order by id
                  ) as duplicate_index
               from activity_entity_links
               where entity_id = @target_entity_id
                  or organization_entity_id = @target_entity_id
            ) duplicates
            where duplicate_index > 1
         ) duplicate_links
         where deleted_link.id = duplicate_links.id
         """;

      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("target_entity_id", targetEntityId);
      return await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task<int> ExecuteMergeCommandAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      string sql,
      Guid sourceEntityId,
      Guid targetEntityId,
      CancellationToken cancellationToken
   )
   {
      await using var command = new NpgsqlCommand(sql, connection, transaction);
      command.Parameters.AddWithValue("source_entity_id", sourceEntityId);
      command.Parameters.AddWithValue("target_entity_id", targetEntityId);
      return await command.ExecuteNonQueryAsync(cancellationToken);
   }

}
