using Npgsql;

using SESport.Core.Configuration;

namespace SESport.Data.Repositories;

public sealed class BroadcastChannelLinkRepository(
   NpgsqlDataSource dataSource
)
{
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
