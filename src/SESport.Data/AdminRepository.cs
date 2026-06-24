using Npgsql;
using SESport.Core.Domain;

namespace SESport.Data;

public sealed class AdminRepository(NpgsqlDataSource dataSource)
{
   private static readonly IReadOnlyDictionary<string, ReferenceTable> Tables =
      new Dictionary<string, ReferenceTable>(StringComparer.OrdinalIgnoreCase)
      {
         ["sports"] = new(
            "sports",
            "Sports",
            "Sports available when creating activities and entities.",
            ReferenceTableKind.Sports
         ),
         ["sources"] = new(
            "sources",
            "Sources",
            "Source names used by proposals and evidence records.",
            "sources",
            "name",
            false,
            false
         ),
         ["activity-types"] = new(
            "activity-types",
            "Activity types",
            "Small controlled vocabulary for activity classification.",
            "activity_types",
            "label",
            true,
            true
         ),
         ["entity-types"] = new(
            "entity-types",
            "Entity types",
            "Types used by the curated entity watchlist.",
            "entity_types",
            "label",
            true,
            false
         ),
         ["country-relevance-kinds"] = new(
            "country-relevance-kinds",
            "Country relevance kinds",
            "Allowed explanations for why an entity is relevant to Sweden.",
            "country_relevance_kinds",
            "label",
            true,
            false
         ),
         ["countries"] = new(
            "countries",
            "Countries",
            "Countries used by entities and country relevance.",
            ReferenceTableKind.Countries
         ),
         ["entity-watch-priorities"] = new(
            "entity-watch-priorities",
            "Entity watch priorities",
            "Priority tiers used by the entity watchlist importer.",
            "entity_watch_priorities",
            "label",
            true,
            false
         ),
         ["entity-stability-kinds"] = new(
            "entity-stability-kinds",
            "Entity stability kinds",
            "Expected volatility for entity watchlist records.",
            "entity_stability_kinds",
            "label",
            true,
            false
         ),
         ["activity-entity-link-roles"] = new(
            "activity-entity-link-roles",
            "Activity entity link roles",
            "Proposal-time roles for suggested entity links.",
            "activity_entity_link_roles",
            "label",
            true,
            false
         ),
         ["producer-types"] = new(
            "producer-types",
            "Producer types",
            "Origins allowed for activity proposals.",
            "producer_types",
            "label",
            true,
            false
         ),
         ["proposal-statuses"] = new(
            "proposal-statuses",
            "Proposal statuses",
            "Review statuses for activity proposals.",
            "proposal_statuses",
            "label",
            true,
            false
         ),
         ["proposal-reject-reasons"] = new(
            "proposal-reject-reasons",
            "Proposal reject reasons",
            "Controlled reasons for rejected activity proposals.",
            "proposal_reject_reasons",
            "label",
            true,
            false
         ),
         ["activity-publication-statuses"] = new(
            "activity-publication-statuses",
            "Activity publication statuses",
            "Publication states used by the public site.",
            "activity_publication_statuses",
            "label",
            true,
            false
         )
      };

   public IReadOnlyList<AdminArea> GetAdminAreas()
   {
      return
      [
         new AdminArea(
            "Runs",
            "Inspect AI job runs.",
            "/Admin/Runs"
         ),
         new AdminArea(
            "Broadcasts",
            "Inspect imported sport broadcasts from EPG data.",
            "/Admin/Broadcasts"
         ),
         new AdminArea(
            "Activity proposals",
            "Review imported and AI-produced activity proposals.",
            "/Admin/Activities/Proposals"
         ),
         new AdminArea(
            "Activities",
            "Create and publish activities for the public site.",
            "/Admin/Activities"
         ),
         new AdminArea(
            "Config",
            "Manage lookup tables and AI configuration.",
            "/Admin/Config"
         ),
         new AdminArea(
            "Entities",
            "Maintain the curated entity watchlist.",
            "/Admin/Entities"
         )
      ];
   }

   public IReadOnlyList<ReferenceNavigationItem> GetReferenceNavigationItems()
   {
      return GetReferenceTables()
         .Select(table => new ReferenceNavigationItem(
            table.Title,
            $"/Admin/Config/{table.Id}"
         ))
         .ToList();
   }

   public IReadOnlyList<AdminNavGroup> GetConfigNavigationGroups()
   {
      var referenceItems = GetReferenceNavigationItems()
         .Select(item => new AdminNavItem(item.Title, item.Href))
         .ToList();
      var activityTypesIndex = referenceItems.FindIndex(item =>
         string.Equals(
            item.Title,
            "Activity types",
            StringComparison.OrdinalIgnoreCase
         )
      );

      var broadcastIgnoreRulesItem = new AdminNavItem(
         "Broadcast Ignore Rules",
         "/Admin/Config/BroadcastIgnoreRules"
      );

      if(activityTypesIndex >= 0)
      {
         referenceItems.Insert(
            activityTypesIndex + 1,
            broadcastIgnoreRulesItem
         );
      }
      else
      {
         referenceItems.Add(broadcastIgnoreRulesItem);
      }

      return
      [
         new AdminNavGroup(
            "AI",
            [
               new AdminNavItem("AI providers", "/Admin/Config/Ai/Providers"),
               new AdminNavItem("AI jobs", "/Admin/Config/Ai/Jobs"),
               new AdminNavItem("AI prompts", "/Admin/Config/Ai/Prompts")
            ]
         ),
         new AdminNavGroup(
            "Reference tables",
            referenceItems
         )
      ];
   }

   public IReadOnlyList<ReferenceTableInfo> GetReferenceTables()
   {
      return Tables.Values
         .OrderBy(table => table.Title)
         .Select(table => new ReferenceTableInfo(
            table.Key,
            table.Title,
            table.Description,
            table.Kind
         ))
         .ToList();
   }

   public async Task<ReferenceTableInfo?> GetReferenceTableInfoAsync(
      string tableKey,
      CancellationToken cancellationToken
   )
   {
      await Task.CompletedTask.WaitAsync(cancellationToken);
      return TryGetTable(tableKey, out var table)
         ? new ReferenceTableInfo(
            table.Key,
            table.Title,
            table.Description,
            table.Kind
         )
         : null;
   }

   public async Task<IReadOnlyList<ReferenceRow>> GetReferenceRowsAsync(
      string tableKey,
      CancellationToken cancellationToken
   )
   {
      var table = GetTable(tableKey);

      if (table.Kind == ReferenceTableKind.Sports)
      {
         return (await GetSportReferenceRowsAsync(cancellationToken))
            .Select(row => new ReferenceRow(
               row.Id,
               row.Name,
               null,
               null
            ))
            .ToList();
      }

      EnsureLookupTable(table);

      var sortSelect = table.HasSortOrder ? "sort_order" : "null";
      var activeSelect = table.HasIsActive ? "is_active" : "null";
      var orderBy = table.HasSortOrder
         ? $"sort_order, {table.LabelColumn}"
         : table.LabelColumn;
      var sql = $"""
         select id, {table.LabelColumn}, {sortSelect}, {activeSelect}
         from {table.TableName}
         order by {orderBy}
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var rows = new List<ReferenceRow>();

      while (await reader.ReadAsync(cancellationToken))
      {
         rows.Add(
            new ReferenceRow(
               reader.GetString(0),
               reader.GetString(1),
               reader.IsDBNull(2) ? null : reader.GetInt32(2),
               reader.IsDBNull(3) ? null : reader.GetBoolean(3)
            )
         );
      }

      return rows;
   }

   public async Task<IReadOnlyList<BroadcastIgnoreRuleListItem>>
      GetBroadcastIgnoreRulesAsync(
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select kind, value, source_key, reason, is_active
         from broadcast_ignore
         order by kind, value, source_key nulls first
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var rules = new List<BroadcastIgnoreRuleListItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         rules.Add(
            new BroadcastIgnoreRuleListItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.IsDBNull(2) ? null : reader.GetString(2),
               reader.IsDBNull(3) ? null : reader.GetString(3),
               reader.GetBoolean(4)
            )
         );
      }

      return rules;
   }

   public async Task<IReadOnlyList<CountryReferenceRow>>
      GetCountryReferenceRowsAsync(CancellationToken cancellationToken)
   {
      const string sql = """
         select id, code, name
         from countries
         order by name, code
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var rows = new List<CountryReferenceRow>();

      while (await reader.ReadAsync(cancellationToken))
      {
         rows.Add(
            new CountryReferenceRow(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetString(2)
            )
         );
      }

      return rows;
   }

   public async Task<IReadOnlyList<SportReferenceRow>>
      GetSportReferenceRowsAsync(CancellationToken cancellationToken)
   {
      const string sql = """
         select id, name, icon_id
         from sports
         order by name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var rows = new List<SportReferenceRow>();

      while(await reader.ReadAsync(cancellationToken))
      {
         rows.Add(
            new SportReferenceRow(
               reader.GetString(0),
               reader.GetString(1),
               reader.IsDBNull(2) ? null : reader.GetString(2)
            )
         );
      }

      return rows;
   }

   public async Task<ReferenceEditModel?> GetReferenceForEditAsync(
      string tableKey,
      string id,
      CancellationToken cancellationToken
   )
   {
      var table = GetTable(tableKey);
      EnsureLookupTable(table);

      var sortSelect = table.HasSortOrder ? "sort_order" : "null";
      var activeSelect = table.HasIsActive ? "is_active" : "null";
      var sql = $"""
         select id, {table.LabelColumn}, {sortSelect}, {activeSelect}
         from {table.TableName}
         where id = @id
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

      return new ReferenceEditModel
      {
         OriginalId = reader.GetString(0),
         Id = reader.GetString(0),
         Label = reader.GetString(1),
         SortOrder = reader.IsDBNull(2) ? null : reader.GetInt32(2),
         IsActive = reader.IsDBNull(3) || reader.GetBoolean(3)
      };
   }

   public async Task<CountryReferenceEditModel?> GetCountryForEditAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select id, code, name
         from countries
         where id = @id
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

      return new CountryReferenceEditModel
      {
         OriginalId = reader.GetString(0),
         Id = reader.GetString(0),
         Code = reader.GetString(1),
         Name = reader.GetString(2)
      };
   }

   public async Task<SportReferenceEditModel?> GetSportForEditAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select id, name, icon_id
         from sports
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

      return new SportReferenceEditModel
      {
         OriginalId = reader.GetString(0),
         Id = reader.GetString(0),
         Name = reader.GetString(1),
         IconId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
      };
   }

   public async Task<BroadcastIgnoreRuleEditModel?>
      GetBroadcastIgnoreRuleForEditAsync(
         string kind,
         string value,
         string? sourceKey,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select kind, value, source_key, reason, is_active
         from broadcast_ignore
         where kind = @kind
            and value = @value
            and source_key is not distinct from @source_key
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("kind", kind);
      command.Parameters.AddWithValue("value", value);
      command.Parameters.AddWithValue(
         "source_key",
         (object?)NormalizeNullable(sourceKey) ?? DBNull.Value
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return new BroadcastIgnoreRuleEditModel
      {
         OriginalKind = reader.GetString(0),
         OriginalValue = reader.GetString(1),
         OriginalSourceKey = reader.IsDBNull(2) ? null : reader.GetString(2),
         Kind = reader.GetString(0),
         Value = reader.GetString(1),
         SourceKey = reader.IsDBNull(2) ? null : reader.GetString(2),
         Reason = reader.IsDBNull(3) ? null : reader.GetString(3),
         IsActive = reader.GetBoolean(4)
      };
   }

   public async Task SaveReferenceAsync(
      string tableKey,
      ReferenceEditModel model,
      CancellationToken cancellationToken
   )
   {
      var table = GetTable(tableKey);
      EnsureLookupTable(table);

      var isNew = string.IsNullOrWhiteSpace(model.OriginalId);
      var id = NormalizeId(model.Id);
      var label = model.Label.Trim();
      var columns = new List<string> { "id", table.LabelColumn };
      var values = new List<string> { "@id", "@label" };
      var assignments = new List<string>
      {
         $"{table.LabelColumn} = @label"
      };

      if (table.HasSortOrder)
      {
         columns.Add("sort_order");
         values.Add("@sort_order");
         assignments.Add("sort_order = @sort_order");
      }

      if (table.HasIsActive)
      {
         columns.Add("is_active");
         values.Add("@is_active");
         assignments.Add("is_active = @is_active");
      }

      var sql = isNew
         ? $"""
            insert into {table.TableName} ({string.Join(", ", columns)})
            values ({string.Join(", ", values)})
            """
         : $"""
            update {table.TableName}
            set {string.Join(", ", assignments)}
            where id = @original_id
            """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("label", label);
      command.Parameters.AddWithValue(
         "original_id",
         model.OriginalId ?? string.Empty
      );

      if (table.HasSortOrder)
      {
         command.Parameters.AddWithValue(
            "sort_order",
            model.SortOrder ?? 1000
         );
      }

      if (table.HasIsActive)
      {
         command.Parameters.AddWithValue("is_active", model.IsActive);
      }

      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task SaveCountryAsync(
      CountryReferenceEditModel model,
      CancellationToken cancellationToken
   )
   {
      var isNew = string.IsNullOrWhiteSpace(model.OriginalId);
      var id = model.Id.Trim().ToLowerInvariant();
      var code = model.Code.Trim().ToUpperInvariant();
      var name = model.Name.Trim();

      var sql = isNew
         ? """
            insert into countries (id, code, name)
            values (@id, @code, @name)
            """
         : """
            update countries
            set
               id = @id,
               code = @code,
               name = @name,
               updated_at = now()
            where id = @original_id
            """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("code", code);
      command.Parameters.AddWithValue("name", name);
      command.Parameters.AddWithValue(
         "original_id",
         model.OriginalId ?? string.Empty
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task SaveSportAsync(
      SportReferenceEditModel model,
      CancellationToken cancellationToken
   )
   {
      var isNew = string.IsNullOrWhiteSpace(model.OriginalId);
      var id = model.Id.Trim();
      var name = model.Name.Trim();
      var iconId = string.IsNullOrWhiteSpace(model.IconId)
         ? null
         : model.IconId.Trim();

      var sql = isNew
         ? """
            insert into sports (id, name, icon_id)
            values (@id, @name, @icon_id)
            """
         : """
            update sports
            set
               id = @id,
               name = @name,
               icon_id = @icon_id,
               updated_at = now()
            where id = @original_id
            """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("name", name);
      command.Parameters.AddWithValue(
         "icon_id",
         (object?)iconId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "original_id",
         model.OriginalId ?? string.Empty
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task SaveBroadcastIgnoreRuleAsync(
      BroadcastIgnoreRuleEditModel model,
      CancellationToken cancellationToken
   )
   {
      var isNew = string.IsNullOrWhiteSpace(model.OriginalKind);
      var kind = model.Kind.Trim();
      var value = model.Value.Trim();
      var sourceKey = NormalizeNullable(model.SourceKey);
      var reason = NormalizeNullable(model.Reason);

      var sql = isNew
         ? """
            insert into broadcast_ignore (
               id, kind, value, source_key, reason, is_active
            )
            values (
               @id, @kind, @value, @source_key, @reason, @is_active
            )
            """
         : """
            update broadcast_ignore
            set
               kind = @kind,
               value = @value,
               source_key = @source_key,
               reason = @reason,
               is_active = @is_active
            where kind = @original_kind
               and value = @original_value
               and source_key is not distinct from @original_source_key
            """;

      await using var command = dataSource.CreateCommand(sql);
      if(isNew)
      {
         command.Parameters.AddWithValue("id", Guid.NewGuid());
      }
      command.Parameters.AddWithValue("kind", kind);
      command.Parameters.AddWithValue("value", value);
      command.Parameters.AddWithValue(
         "source_key",
         (object?)sourceKey ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "reason",
         (object?)reason ?? DBNull.Value
      );
      command.Parameters.AddWithValue("is_active", model.IsActive);
      command.Parameters.AddWithValue(
         "original_kind",
         model.OriginalKind ?? string.Empty
      );
      command.Parameters.AddWithValue(
         "original_value",
         model.OriginalValue ?? string.Empty
      );
      command.Parameters.AddWithValue(
         "original_source_key",
         (object?)NormalizeNullable(model.OriginalSourceKey) ?? DBNull.Value
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task DeleteReferenceAsync(
      string tableKey,
      string id,
      CancellationToken cancellationToken
   )
   {
      var table = GetTable(tableKey);
      EnsureLookupTable(table);

      var sql = $"delete from {table.TableName} where id = @id";
      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task DeleteCountryAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      const string sql = "delete from countries where id = @id";
      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task DeleteSportAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      const string sql = "delete from sports where id = @id";
      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task DeleteBroadcastIgnoreRuleAsync(
      string kind,
      string value,
      string? sourceKey,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from broadcast_ignore
         where kind = @kind
            and value = @value
            and source_key is not distinct from @source_key
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("kind", kind);
      command.Parameters.AddWithValue("value", value);
      command.Parameters.AddWithValue(
         "source_key",
         (object?)NormalizeNullable(sourceKey) ?? DBNull.Value
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static string? NormalizeNullable(string? value)
   {
      return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
   }

   public async Task<IReadOnlyList<SourceListItem>> GetSourcesAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select id, name, updated_at
         from sources
         order by name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var sources = new List<SourceListItem>();

      while (await reader.ReadAsync(cancellationToken))
      {
         sources.Add(
            new SourceListItem(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetFieldValue<DateTimeOffset>(2)
            )
         );
      }

      return sources;
   }

   public async Task<SourceEditModel?> GetSourceForEditAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select id, name
         from sources
         where id = @id
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

      return new SourceEditModel
      {
         OriginalId = reader.GetString(0),
         Id = reader.GetString(0),
         Name = reader.GetString(1)
      };
   }

   public async Task SaveSourceAsync(
      SourceEditModel model,
      CancellationToken cancellationToken
   )
   {
      var isNew = string.IsNullOrWhiteSpace(model.OriginalId);
      var sql = isNew
         ? """
            insert into sources (id, name)
            values (@id, @name)
            """
         : """
            update sources
            set name = @name, updated_at = now()
            where id = @original_id
            """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", NormalizeId(model.Id));
      command.Parameters.AddWithValue("name", model.Name.Trim());
      command.Parameters.AddWithValue(
         "original_id",
         model.OriginalId ?? string.Empty
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task DeleteSourceAsync(
      string id,
      CancellationToken cancellationToken
   )
   {
      const string sql = "delete from sources where id = @id";
      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<IReadOnlyList<EntityListItem>> GetEntitiesAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            e.id,
            e.canonical_name,
            et.label,
            s.name,
            p.id,
            p.label,
            coalesce(c.name, e.country_id, ''),
            coalesce(linked.related_entity_names, '')
         from entities e
         join entity_types et on et.id = e.entity_type_id
         join sports s on s.id = e.sport_id
         join entity_watch_priorities p on p.id = e.watch_priority_id
         left join countries c on c.id = e.country_id
         left join lateral (
            select string_agg(linked_name, ', ' order by linked_name)
               as related_entity_names
            from (
               select distinct e2.canonical_name as linked_name
               from entity_to_entity_links l
               join entities e2
                  on e2.id = case
                     when l.source_entity_id = e.id
                        then l.target_entity_id
                     else l.source_entity_id
                  end
               where l.source_entity_id = e.id
                  or l.target_entity_id = e.id
            ) linked_entities
         ) linked on true
         order by e.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var entities = new List<EntityListItem>();

      while (await reader.ReadAsync(cancellationToken))
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
               reader.GetString(7)
            )
         );
      }

      return entities;
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

      if (!await reader.ReadAsync(cancellationToken))
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
         PersonGenderId = reader.IsDBNull(10) ? null : reader.GetString(10)
      };

      await reader.DisposeAsync();

      const string linkSql = $$"""
         select
            case
               when source_entity_id = @id then target_entity_id
               else source_entity_id
            end as linked_entity_id
         from entity_to_entity_links
         where source_entity_id = @id or target_entity_id = @id
         order by linked_entity_id
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

      if (!await reader.ReadAsync(cancellationToken))
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
         PersonGenderId = reader.IsDBNull(10) ? null : reader.GetString(10)
      };

      await reader.DisposeAsync();

      var linkSql = $@"
         select
            case
               when source_entity_id = @id then target_entity_id
               else source_entity_id
            end as linked_entity_id
         from entity_to_entity_links l
         join entities linked
            on linked.id = case
               when source_entity_id = @id then target_entity_id
               else source_entity_id
            end
            where (
               source_entity_id = @id
               or target_entity_id = @id
            )
            and linked.entity_type_id in (
               '{TrackedEntityTypeIds.NationalTeam}',
               '{TrackedEntityTypeIds.Organization}',
               '{TrackedEntityTypeIds.Series}',
               '{TrackedEntityTypeIds.Tour}',
               '{TrackedEntityTypeIds.League}',
               '{TrackedEntityTypeIds.Championship}'
            )
         order by linked_entity_id
         ";

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

      if (excludeEntityId is not null)
      {
         command.Parameters.AddWithValue("exclude_entity_id", excludeEntityId);
      }

      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<EntityLinkOption>();

      while (await reader.ReadAsync(cancellationToken))
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
      const string sql = $"""
         select
            e.id,
            e.canonical_name,
            et.label,
            s.name
         from entities e
         join entity_types et on et.id = e.entity_type_id
         join sports s on s.id = e.sport_id
         where e.entity_type_id not in (
            '{TrackedEntityTypeIds.Person}',
            '{TrackedEntityTypeIds.NationalTeam}',
            '{TrackedEntityTypeIds.Pair}'
         )
         order by e.canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<EntityLinkOption>();

      while (await reader.ReadAsync(cancellationToken))
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

      const string sql = $"""
         select
            e.id,
            e.canonical_name,
            et.label,
            s.name
         from entities e
         join entity_types et on et.id = e.entity_type_id
         join sports s on s.id = e.sport_id
         where e.entity_type_id not in (
            '{TrackedEntityTypeIds.Person}',
            '{TrackedEntityTypeIds.NationalTeam}',
            '{TrackedEntityTypeIds.Pair}'
         )
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

   public async Task<IReadOnlyList<EntityNameOption>>
      GetPersonEntityNameOptionsAsync(
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
            where e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
            union all
            select
               e.id,
               e.alias_name as name
            from entities e
            where e.entity_type_id = '{{TrackedEntityTypeIds.Person}}'
               and e.alias_name is not null
         ) names
         order by name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var options = new List<EntityNameOption>();

      while (await reader.ReadAsync(cancellationToken))
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

      while (await reader.ReadAsync(cancellationToken))
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
         new LookupOption(PersonGenderIds.Male, "Male"),
         new LookupOption(PersonGenderIds.NonBinary, "Non-binary")
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
      return includePersonGender
         ? """
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
               person_gender_id
            from entities
            where id = @id
            """
         : """
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
               null::text as person_gender_id
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
               alias_name
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
               @alias_name
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

      await SaveEntityLinksAsync(
         connection,
         transaction,
         id,
         model.LinkedEntityIds,
         cancellationToken
      );

      await transaction.CommitAsync(cancellationToken);
      model.Id = id;
   }

   public async Task DeleteEntityAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = "delete from entities where id = @id";
      await using var command = dataSource.CreateCommand(sql);
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
         (object?)NormalizePersonAliasName(model) ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "person_gender_id",
         (object?)NormalizePersonGenderId(model) ?? DBNull.Value
      );
   }

   private static string? NormalizePersonAliasName(EntityEditModel model)
   {
      if(!string.Equals(
         model.EntityTypeId,
         TrackedEntityTypeIds.Person,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return null;
      }

      return string.IsNullOrWhiteSpace(model.AliasName)
         ? null
         : model.AliasName.Trim();
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

      await using (var deleteCommand = new NpgsqlCommand(
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

      foreach (var targetEntityId in (targetEntityIds ?? []).Distinct())
      {
         if (targetEntityId == sourceEntityId)
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

   private static ReferenceTable GetTable(string tableKey)
   {
      if (TryGetTable(tableKey, out var table))
      {
         return table;
      }

      throw new InvalidOperationException("Unknown reference table.");
   }

   private static bool TryGetTable(string tableKey, out ReferenceTable table)
   {
      return Tables.TryGetValue(tableKey, out table!);
   }

   private static void EnsureLookupTable(ReferenceTable table)
   {
      if (table.Kind != ReferenceTableKind.Lookup)
      {
         throw new InvalidOperationException("Reference view is not editable.");
      }
   }

   private static string NormalizeId(string value)
   {
      return value.Trim();
   }

   private sealed record ReferenceTable(
      string Key,
      string Title,
      string Description,
      string TableName,
      string LabelColumn,
      bool HasSortOrder,
      bool HasIsActive,
      ReferenceTableKind Kind = ReferenceTableKind.Lookup
   )
   {
      public ReferenceTable(
         string key,
         string title,
         string description,
         ReferenceTableKind kind
      ) : this(key, title, description, "", "", false, false, kind)
      {
      }
   }
}
