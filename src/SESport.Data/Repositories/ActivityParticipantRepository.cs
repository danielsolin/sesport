using Npgsql;

using NpgsqlTypes;

using SESport.Core.Domain;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class ActivityParticipantRepository(NpgsqlDataSource dataSource)
{
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
      var activeExpression = activityId is null ? "true" : "al.is_active";
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
               else ''
            end,
            coalesce(e.alias_name, ''),
            wp.sort_order as sort_order,
            {{activeExpression}}
         from entities e
         join entity_watch_priorities wp on wp.id = e.watch_priority_id
         {{activityLinkJoin}}
         {{ActivityQueryRepository.GetLinkedOrganizationNamesLateralSql("e")}}
         {{whereClause}}
         order by sort_order, e.canonical_name
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
               reader.GetString(5),
               reader.GetBoolean(7)
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

   public async Task SetParticipantActiveAsync(
      Guid activityId,
      Guid entityId,
      bool isActive,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         with selected_activity as (
            select activity_group_id
            from activities
            where id = @activity_id
         )
         update activity_entity_links link
         set is_active = @is_active
         from activities activity
         where link.activity_id = activity.id
            and link.entity_id = @entity_id
            and (
               link.activity_id = @activity_id
               or (
                  activity.activity_group_id = (
                     select activity_group_id
                     from selected_activity
                  )
                  and coalesce(
                     activity.starts_at,
                     (
                        activity.activity_date
                        + coalesce(
                           activity.local_start_time,
                           time '23:59:59'
                        )
                     ) at time zone activity.time_zone_id
                  ) > now()
               )
            )
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("activity_id", activityId);
      command.Parameters.AddWithValue("entity_id", entityId);
      command.Parameters.AddWithValue("is_active", isActive);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task AddParticipantAsync(
      Guid activityId,
      Guid entityId,
      Guid organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var transaction = await connection.BeginTransactionAsync(
         cancellationToken
      );
      var representedEntityId =
         await ResolveRepresentedEntityIdAsync(
            connection,
            transaction,
            entityId,
            organizationEntityId,
            cancellationToken
         );
      const string sql = $$"""
         insert into activity_entity_links (
            id,
            activity_id,
            entity_id,
            organization_entity_id,
            represented_entity_id
         )
         select
            @id,
            @activity_id,
            e.id,
            @organization_entity_id,
            @represented_entity_id
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
         organizationEntityId
      );
      command.Parameters.Add(
         "represented_entity_id",
         NpgsqlDbType.Uuid
      ).Value = representedEntityId ?? (object)DBNull.Value;
      await command.ExecuteNonQueryAsync(cancellationToken);
      await transaction.CommitAsync(cancellationToken);
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
               else ''
            end,
            coalesce(e.alias_name, '')
         from entities e
         join entity_watch_priorities wp on wp.id = e.watch_priority_id
         {{ActivityQueryRepository.GetLinkedOrganizationNamesLateralSql("e")}}
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
               reader.GetString(5),
               true
            )
         );
      }

      return participants;
   }

   internal static async Task<Guid?> ResolveRepresentedEntityIdAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid personEntityId,
      Guid? organizationEntityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = $$"""
         with direct_context as (
            select context.id
            from entities context
            where context.id = @organization_entity_id
               and context.entity_type_id =
                  '{{TrackedEntityTypeIds.NationalTeam}}'
               and exists (
                  select 1
                  from entity_to_entity_links link
                  where (link.source_entity_id = @person_entity_id
                        and link.target_entity_id = context.id)
                     or (link.target_entity_id = @person_entity_id
                        and link.source_entity_id = context.id)
               )
         ),
         context_teams as (
            select distinct team.id
            from entity_to_entity_links person_team_link
            join entities team
               on team.id = case
                  when person_team_link.source_entity_id =
                     @person_entity_id
                  then person_team_link.target_entity_id
                  else person_team_link.source_entity_id
               end
            where (
               person_team_link.source_entity_id = @person_entity_id
               or person_team_link.target_entity_id = @person_entity_id
            )
               and team.entity_type_id = '{{TrackedEntityTypeIds.Team}}'
               and @organization_entity_id is not null
               and exists (
                  select 1
                  from entity_to_entity_links context_link
                  where (context_link.source_entity_id = team.id
                        and context_link.target_entity_id =
                           @organization_entity_id)
                     or (context_link.target_entity_id = team.id
                        and context_link.source_entity_id =
                           @organization_entity_id)
               )
         ),
         all_teams as (
            select distinct team.id
            from entity_to_entity_links person_team_link
            join entities team
               on team.id = case
                  when person_team_link.source_entity_id =
                     @person_entity_id
                  then person_team_link.target_entity_id
                  else person_team_link.source_entity_id
               end
            where (
               person_team_link.source_entity_id = @person_entity_id
               or person_team_link.target_entity_id = @person_entity_id
            )
               and team.entity_type_id = '{{TrackedEntityTypeIds.Team}}'
         )
         select case
            when exists (select 1 from direct_context)
            then (select id from direct_context)
            when exists (
               select 1
               from entities context
               where context.id = @organization_entity_id
                  and context.entity_type_id =
                     '{{TrackedEntityTypeIds.NationalTeam}}'
            )
            then null
            when (select count(*) from context_teams) = 1
            then (select id from context_teams)
            when (select count(*) from context_teams) > 1
            then null
            when (select count(*) from all_teams) = 1
            then (select id from all_teams)
            else null
         end as represented_entity_id
         """;

      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("person_entity_id", personEntityId);
      command.Parameters.Add(
         "organization_entity_id",
         NpgsqlDbType.Uuid
      ).Value = organizationEntityId ?? (object)DBNull.Value;

      var value = await command.ExecuteScalarAsync(cancellationToken);
      return value is Guid representedEntityId
         ? representedEntityId
         : null;
   }

}
