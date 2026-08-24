using Npgsql;

using SESport.Core.Broadcast;
using SESport.Core.Configuration;
using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Core.Sources;
using SESport.Data.Models;

namespace SESport.Data.Repositories;

public sealed class AdminReferenceRepository(NpgsqlDataSource dataSource)
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
            "Allowed explanations for why an entity is relevant to " +
            PrimaryCountry.CountryName + ".",
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

      if(table.Kind == ReferenceTableKind.Sports)
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

      while(await reader.ReadAsync(cancellationToken))
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
      var sql = $"""
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

      while(await reader.ReadAsync(cancellationToken))
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
         select
            id, name, display_name, icon_id,
            requires_start_time, is_team_sport
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
               reader.IsDBNull(2) ? null : reader.GetString(2),
               reader.IsDBNull(3) ? null : reader.GetString(3),
               reader.GetBoolean(4),
               reader.GetBoolean(5)
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

      if(!await reader.ReadAsync(cancellationToken))
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

      if(!await reader.ReadAsync(cancellationToken))
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
         select
            id, name, display_name, icon_id,
            requires_start_time, is_team_sport
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
         DisplayName = reader.IsDBNull(2) ? null : reader.GetString(2),
         IconId = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
         RequiresStartTime = reader.GetBoolean(4),
         IsTeamSport = reader.GetBoolean(5)
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

      if(table.HasSortOrder)
      {
         columns.Add("sort_order");
         values.Add("@sort_order");
         assignments.Add("sort_order = @sort_order");
      }

      if(table.HasIsActive)
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

      if(table.HasSortOrder)
      {
         command.Parameters.AddWithValue(
            "sort_order",
            model.SortOrder ?? 1000
         );
      }

      if(table.HasIsActive)
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
      var displayName = NormalizeNullable(model.DisplayName);
      var iconId = string.IsNullOrWhiteSpace(model.IconId)
         ? null
         : model.IconId.Trim();

      var sql = isNew
         ? """
            insert into sports (
               id, name, display_name, icon_id,
               requires_start_time, is_team_sport
            )
            values (
               @id, @name, @display_name, @icon_id,
               @requires_start_time, @is_team_sport
            )
            """
         : """
            update sports
            set
               id = @id,
               name = @name,
               display_name = @display_name,
               icon_id = @icon_id,
               requires_start_time = @requires_start_time,
               is_team_sport = @is_team_sport,
               updated_at = now()
            where id = @original_id
            """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("name", name);
      command.Parameters.AddWithValue(
         "display_name",
         (object?)displayName ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "icon_id",
         (object?)iconId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "requires_start_time",
         model.RequiresStartTime
      );
      command.Parameters.AddWithValue(
         "is_team_sport",
         model.IsTeamSport
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

   private static ReferenceTable GetTable(string tableKey)
   {
      if(TryGetTable(tableKey, out var table))
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
      if(table.Kind != ReferenceTableKind.Lookup)
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
