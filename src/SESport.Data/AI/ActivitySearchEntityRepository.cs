using Npgsql;

using SESport.AI.ActivitySearch;
using SESport.Core.Domain;
using SESport.Core.Identifiers;
using SESport.Core.Ingestion;

namespace SESport.Data.AI;

public sealed class ActivitySearchEntityRepository : IAsyncDisposable
{
   private readonly NpgsqlDataSource dataSource;
   private readonly bool ownsDataSource;

   public ActivitySearchEntityRepository(NpgsqlDataSource dataSource)
   {
      this.dataSource = dataSource;
   }

   private ActivitySearchEntityRepository(
      NpgsqlDataSource dataSource,
      bool ownsDataSource
   )
   {
      this.dataSource = dataSource;
      this.ownsDataSource = ownsDataSource;
   }

   public static ActivitySearchEntityRepository Connect(string connectionString)
   {
      return new ActivitySearchEntityRepository(
         NpgsqlDataSource.Create(connectionString),
         ownsDataSource: true
      );
   }

   public async ValueTask DisposeAsync()
   {
      if(ownsDataSource)
      {
         await dataSource.DisposeAsync();
      }
   }

   public async Task<IReadOnlyCollection<ActivitySearchEntity>> SelectAsync(
      string? entityIdOrName,
      int take,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            e.id::text,
            e.canonical_name,
            e.entity_type_id,
            e.sport_id,
            s.name,
            c.name,
            e.country_relevance_reason,
            e.watch_priority_id,
            e.expected_stability_id
         from entities e
         join sports s on s.id = e.sport_id
         join countries c on c.id = e.country_id
         where
            @entity_id_or_name is null or
            e.id::text = @entity_id_or_name or
            e.canonical_name ilike @entity_id_or_name or
            e.alias_name ilike @entity_id_or_name
         order by
            case e.watch_priority_id
               when 'tier_1' then 1
               when 'tier_2' then 2
               when 'tier_3' then 3
               else 4
            end,
            e.canonical_name
         limit @take
         """;

      await using var connection = await dataSource.OpenConnectionAsync(
         cancellationToken
      );
      await using var command = new NpgsqlCommand(sql, connection);
      command.Parameters.AddWithValue(
         "entity_id_or_name",
         (object?)entityIdOrName ?? DBNull.Value
      );
      command.Parameters.AddWithValue("take", take);

      var entities = new List<ActivitySearchEntity>();
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      while(await reader.ReadAsync(cancellationToken))
      {
         var relevanceReason = reader.GetString(6).Trim();
         var notes = string.IsNullOrWhiteSpace(relevanceReason)
            ? $"Watch priority: {reader.GetString(7)}. " +
               $"Expected stability: {reader.GetString(8)}."
            : relevanceReason;

         entities.Add(new ActivitySearchEntity(
            new ExternalEntityId(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            new ImportedSport(
               new ExternalEntityId(reader.GetString(3)),
               reader.GetString(4)
            ),
            relevanceReason,
            null,
            [],
            null,
            notes,
            reader.GetString(5)
         ));
      }

      return entities;
   }
}
