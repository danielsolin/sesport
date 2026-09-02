using Npgsql;

using SESport.Core.Configuration;

namespace SESport.Data.Broadcasts;

public sealed class BroadcastChannelLinkRepository(
   NpgsqlDataSource dataSource
)
{
   public async Task<BroadcastChannelLinkRow?>
      GetByNameAsync(
         string canonicalName,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select canonical_name, url, aliases, is_active, updated_at
         from broadcast_channel_links
         where canonical_name = @canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("canonical_name", canonicalName);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         return null;
      }

      return new BroadcastChannelLinkRow(
         reader.GetString(0),
         reader.GetString(1),
         reader.GetFieldValue<string[]>(2),
         reader.GetBoolean(3),
         reader.GetFieldValue<DateTimeOffset>(4)
      );
   }

   public async Task SaveAsync(
      string? originalCanonicalName,
      string canonicalName,
      string url,
      IReadOnlyList<string> aliases,
      bool isActive,
      CancellationToken cancellationToken
   )
   {
      var isNew = string.IsNullOrWhiteSpace(originalCanonicalName);
      const string insertSql = """
         insert into broadcast_channel_links (
            canonical_name,
            url,
            aliases,
            is_active
         )
         values (
            @canonical_name,
            @url,
            @aliases,
            @is_active
         )
         """;
      const string updateSql = """
         update broadcast_channel_links
         set url = @url,
            aliases = @aliases,
            is_active = @is_active,
            updated_at = now()
         where canonical_name = @original_canonical_name
         """;
      var sql = isNew ? insertSql : updateSql;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue(
         "canonical_name",
         canonicalName.Trim()
      );
      command.Parameters.AddWithValue("url", url.Trim());
      command.Parameters.AddWithValue("aliases", aliases.ToArray());
      command.Parameters.AddWithValue("is_active", isActive);
      command.Parameters.AddWithValue(
         "original_canonical_name",
         originalCanonicalName?.Trim() ?? string.Empty
      );

      var affectedRows = await command.ExecuteNonQueryAsync(
         cancellationToken
      );

      if(!isNew && affectedRows != 1)
      {
         throw new InvalidOperationException(
            "The broadcast channel link no longer exists."
         );
      }
   }

   public async Task<IReadOnlyList<BroadcastChannelLinkRow>>
      GetAllAsync(CancellationToken cancellationToken)
   {
      const string sql = """
         select canonical_name, url, aliases, is_active, updated_at
         from broadcast_channel_links
         order by canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var rows = new List<BroadcastChannelLinkRow>();

      while(await reader.ReadAsync(cancellationToken))
      {
         rows.Add(
            new BroadcastChannelLinkRow(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetFieldValue<string[]>(2),
               reader.GetBoolean(3),
               reader.GetFieldValue<DateTimeOffset>(4)
            )
         );
      }

      return rows;
   }

   public async Task DeleteAsync(
      string canonicalName,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from broadcast_channel_links
         where canonical_name = @canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("canonical_name", canonicalName);

      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task<IReadOnlyList<BroadcastChannelLinkDefinition>>
      GetActiveDefinitionsAsync(CancellationToken cancellationToken)
   {
      const string sql = """
         select canonical_name, url, aliases
         from broadcast_channel_links
         where is_active
         order by canonical_name
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var definitions = new List<BroadcastChannelLinkDefinition>();

      while(await reader.ReadAsync(cancellationToken))
      {
         definitions.Add(
            new BroadcastChannelLinkDefinition(
               reader.GetString(0),
               reader.GetString(1),
               reader.GetFieldValue<string[]>(2)
            )
         );
      }

      return definitions;
   }
}
