using Npgsql;

using NpgsqlTypes;

using SESport.Core.Domain;
using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Data.Activities;

public sealed class ActivityGroupQueryRepository(
   NpgsqlDataSource dataSource
)
{
   public async Task<IReadOnlyList<LookupOption>>
      SearchActivityGroupOptionsAsync(
         string? term,
         string? sportId,
         CancellationToken cancellationToken,
         Guid? organizationEntityId = null
      )
   {
      term = term?.Trim() ?? string.Empty;
      var applyTermFilter = term != string.Empty;
      var escapedTerm = applyTermFilter
         ? term
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
         : string.Empty;
      var termFilterSql = applyTermFilter
         ? "and title ilike @term escape '\\'"
         : string.Empty;
      var sql = $$"""
         select id::text, title
         from activity_groups
         where (@sport_id is null or sport_id = @sport_id)
            and (@organization_entity_id is null or exists (
               select 1
               from activities a
               where a.activity_group_id = activity_groups.id
                  and {{ActivityQueryRepository
                     .GetActivityOrganizationEntityIdSql("a")}} =
                     @organization_entity_id
            ))
            {{termFilterSql}}
         order by start_date desc, end_date desc, title, id
         limit 20
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.Add(
         "sport_id",
         NpgsqlDbType.Text
      ).Value = string.IsNullOrWhiteSpace(sportId)
         ? DBNull.Value
         : sportId;
      command.Parameters.Add(
         "organization_entity_id",
         NpgsqlDbType.Uuid
      ).Value = (object?)organizationEntityId ?? DBNull.Value;
      if(applyTermFilter)
      {
         command.Parameters.AddWithValue("term", $"%{escapedTerm}%");
      }

      return await ActivityQueryRepository.ReadLookupOptionsAsync(
         command,
         cancellationToken
      );
   }

   internal const string FindMatchingActivityGroupSql = """
      select id
      from activity_groups
      where sport_id = @sport_id
         and title = @title
         and start_date <= @activity_date
         and end_date >= @activity_date
      order by
         (end_date - start_date),
         start_date desc,
         id
      limit 1
      """;

   public async Task<Guid?> FindMatchingActivityGroupIdAsync(
      string title,
      string sportId,
      DateOnly activityDate,
      CancellationToken cancellationToken
   )
   {
      await using var command = dataSource
         .CreateCommand(FindMatchingActivityGroupSql);
      command.Parameters.AddWithValue("sport_id", sportId.Trim());
      command.Parameters.AddWithValue("title", title.Trim());
      command.Parameters.Add(
         "activity_date",
         NpgsqlDbType.Date
      ).Value = activityDate;

      var result = await command.ExecuteScalarAsync(cancellationToken);
      return result is null || result is DBNull ? null : (Guid)result;
   }

   public async Task<IReadOnlyList<string>> GetOtherGroupDescriptionsAsync(
      Guid activityGroupId,
      Guid? excludedActivityId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select btrim(description) as description
         from activities
         where activity_group_id = @activity_group_id
            and description is not null
            and btrim(description) <> ''
            and (@excluded_activity_id is null
               or id <> @excluded_activity_id)
         group by btrim(description)
         order by max(activity_date) desc, description
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "activity_group_id",
         activityGroupId
      );
      command.Parameters.Add(
         "excluded_activity_id",
         NpgsqlDbType.Uuid
      ).Value = (object?)excludedActivityId ?? DBNull.Value;
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var descriptions = new List<string>();

      while(await reader.ReadAsync(cancellationToken))
      {
         descriptions.Add(reader.GetString(0));
      }

      return descriptions;
   }

   public async Task<Guid?> GetActivityGroupIdAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select activity_group_id
         from activities
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      var result = await command.ExecuteScalarAsync(cancellationToken);

      return result is null || result is DBNull
         ? null
         : (Guid)result;
   }

   public async Task<string?> GetActivityGroupTitleAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select title
         from activity_groups
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      return (string?)await command.ExecuteScalarAsync(cancellationToken);
   }

   public async Task<ActivityGroupEditModel?> GetActivityGroupForEditAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            id,
            title,
            sport_id,
            start_date,
            end_date,
            no_grouping,
            public_date_mode
         from activity_groups
         where id = @id
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

      return new ActivityGroupEditModel
      {
         Id = reader.GetGuid(0),
         Title = reader.GetString(1),
         SportId = reader.GetString(2),
         StartDate = reader.GetFieldValue<DateOnly>(3),
         EndDate = reader.GetFieldValue<DateOnly>(4),
         NoGrouping = reader.GetBoolean(5),
         PublicDateMode = reader.GetString(6)
      };
   }

   public async Task<IReadOnlyList<ActivityGroupActivityListItem>>
      GetActivitiesForGroupEditAsync(
         Guid activityGroupId,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            a.id,
            a.title,
            a.description,
            a.activity_date,
            a.local_start_time,
            a.local_end_time,
            organization.canonical_name
         from activities a
         left join entities organization
            on organization.id = a.organization_entity_id
         where a.activity_group_id = @activity_group_id
         order by
            a.activity_date,
            a.local_start_time nulls last,
            a.title
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "activity_group_id",
         activityGroupId
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var activities = new List<ActivityGroupActivityListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         activities.Add(
            new ActivityGroupActivityListItem(
               reader.GetGuid(0),
               reader.GetString(1),
               ActivityQueryRepository.ReadString(reader, 2),
               reader.GetFieldValue<DateOnly>(3),
               ActivityQueryRepository.ReadTimeOnly(reader, 4),
               ActivityQueryRepository.ReadTimeOnly(reader, 5)
            )
            {
               OrganizationName = ActivityQueryRepository.ReadString(
                  reader,
                  6
               )
            }
         );
      }

      return activities;
   }

   public async Task<IReadOnlyList<ActivityGroupSourceListItem>>
      GetSourcesForGroupEditAsync(
         Guid activityGroupId,
         CancellationToken cancellationToken
      )
   {
      var sql = $"""
         select
            s.kind,
            s.url,
            s.title,
            s.excerpt,
            s.observed_at
         from activities a
         join sources s
            on s.correlation_type = '{SourceCorrelationTypes.Activity}'
            and s.correlation_id = a.id::text
         where a.activity_group_id = @activity_group_id
         order by s.observed_at desc, s.created_at desc, s.id desc
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "activity_group_id",
         activityGroupId
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var sources = new List<ActivityGroupSourceListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         sources.Add(
            new ActivityGroupSourceListItem(
               reader.GetString(0),
               reader.GetString(1),
               ActivityQueryRepository.ReadString(reader, 2),
               ActivityQueryRepository.ReadString(reader, 3),
               reader.GetFieldValue<DateTimeOffset>(4)
            )
         );
      }

      return sources;
   }

   public async Task<
      IReadOnlyDictionary<Guid, IReadOnlyList<ActivityGroupParticipant>>>
      GetActivityGroupParticipantsAsync(
         IReadOnlyCollection<Guid> activityGroupIds,
         CancellationToken cancellationToken
      )
   {
      if(activityGroupIds.Count == 0)
      {
         return new Dictionary<
            Guid,
            IReadOnlyList<ActivityGroupParticipant>
         >();
      }

      var sql = $$"""
         select distinct
            a.activity_group_id,
            e.id,
            e.canonical_name
         from activities a
         join activity_entity_links al on al.activity_id = a.id
         join entities e on e.id = al.entity_id
         where a.activity_group_id = any(@activity_group_ids)
            and e.entity_type_id in (
               '{{TrackedEntityTypeIds.Person}}',
               '{{TrackedEntityTypeIds.NationalTeam}}',
               '{{TrackedEntityTypeIds.Pair}}'
            )
         order by a.activity_group_id, e.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "activity_group_ids",
         activityGroupIds.Distinct().ToArray()
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var participants = new Dictionary<
         Guid,
         List<ActivityGroupParticipant>
      >();

      while(await reader.ReadAsync(cancellationToken))
      {
         var activityGroupId = reader.GetGuid(0);

         if(!participants.TryGetValue(activityGroupId, out var group))
         {
            group = [];
            participants[activityGroupId] = group;
         }

         group.Add(
            new ActivityGroupParticipant(
               reader.GetGuid(1),
               reader.GetString(2)
            )
         );
      }

      return participants.ToDictionary(
         pair => pair.Key,
         pair => (IReadOnlyList<ActivityGroupParticipant>)pair.Value
      );
   }
}
