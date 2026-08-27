using Npgsql;

using SESport.Core.Broadcast;
using SESport.Core.Sources;

namespace SESport.Data.Repositories;

internal static class BroadcastStreamSourcePersistence
{
   public static async Task UpsertForBroadcastAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid broadcastId,
      IReadOnlyCollection<BroadcastStreamLink> streamLinks,
      CancellationToken cancellationToken
   )
   {
      var links = NormalizeLinks(streamLinks);

      foreach(var link in links)
      {
         await DeletePreviousProviderLinkAsync(
            connection,
            transaction,
            SourceCorrelationTypes.Broadcast,
            broadcastId.ToString(),
            link.ProviderName,
            link.Url,
            cancellationToken
         );
         await UpsertSourceAsync(
            connection,
            transaction,
            SourceCorrelationTypes.Broadcast,
            broadcastId.ToString(),
            link.ProviderName,
            link.Url,
            excerpt: null,
            DateTimeOffset.UtcNow,
            cancellationToken
         );
      }
   }

   public static async Task CopyToActivityAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid activityId,
      IReadOnlyCollection<Guid> broadcastIds,
      CancellationToken cancellationToken
   )
   {
      var normalizedBroadcastIds = broadcastIds.Distinct().ToArray();
      if(normalizedBroadcastIds.Length == 0)
      {
         return;
      }

      var sources = await ReadBroadcastSourcesAsync(
         connection,
         transaction,
         normalizedBroadcastIds,
         cancellationToken
      );

      foreach(var source in sources)
      {
         if(string.IsNullOrWhiteSpace(source.Title))
         {
            continue;
         }

         await DeletePreviousProviderLinkAsync(
            connection,
            transaction,
            SourceCorrelationTypes.Activity,
            activityId.ToString(),
            source.Title,
            source.Url,
            cancellationToken
         );
         await UpsertSourceAsync(
            connection,
            transaction,
            SourceCorrelationTypes.Activity,
            activityId.ToString(),
            source.Title,
            source.Url,
            source.Excerpt,
            source.ObservedAt,
            cancellationToken
         );
      }
   }

   public static async Task CopyToLinkedActivitiesAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      Guid broadcastId,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select activity_id
         from activity_broadcast_links
         where broadcast_id = @broadcast_id
         """;

      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("broadcast_id", broadcastId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var activityIds = new List<Guid>();

      while(await reader.ReadAsync(cancellationToken))
      {
         activityIds.Add(reader.GetGuid(0));
      }

      await reader.DisposeAsync();

      foreach(var activityId in activityIds)
      {
         await CopyToActivityAsync(
            connection,
            transaction,
            activityId,
            [broadcastId],
            cancellationToken
         );
      }
   }

   private static async Task<IReadOnlyList<StreamSource>>
      ReadBroadcastSourcesAsync(
         NpgsqlConnection connection,
         NpgsqlTransaction transaction,
         IReadOnlyCollection<Guid> broadcastIds,
         CancellationToken cancellationToken
      )
   {
      const string sql = """
         select
            s.title,
            s.url,
            s.excerpt,
            s.observed_at
         from sources s
         where s.correlation_type = @correlation_type
            and s.correlation_id = any(@broadcast_ids)
            and s.kind = @kind
         order by s.observed_at desc, s.created_at desc, s.id desc
         """;

      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue(
         "correlation_type",
         SourceCorrelationTypes.Broadcast
      );
      command.Parameters.AddWithValue(
         "broadcast_ids",
         broadcastIds.Select(id => id.ToString()).ToArray()
      );
      command.Parameters.AddWithValue("kind", SourceKinds.StreamLink);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var sources = new List<StreamSource>();

      while(await reader.ReadAsync(cancellationToken))
      {
         sources.Add(
            new StreamSource(
               reader.IsDBNull(0) ? null : reader.GetString(0),
               reader.GetString(1),
               reader.IsDBNull(2) ? null : reader.GetString(2),
               reader.GetFieldValue<DateTimeOffset>(3)
            )
         );
      }

      return sources;
   }

   private static async Task DeletePreviousProviderLinkAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      string correlationType,
      string correlationId,
      string providerName,
      string url,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         delete from sources
         where correlation_type = @correlation_type
            and correlation_id = @correlation_id
            and kind = @kind
            and title = @title
            and url <> @url
         """;

      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("correlation_type", correlationType);
      command.Parameters.AddWithValue("correlation_id", correlationId);
      command.Parameters.AddWithValue("kind", SourceKinds.StreamLink);
      command.Parameters.AddWithValue("title", providerName);
      command.Parameters.AddWithValue("url", url);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static async Task UpsertSourceAsync(
      NpgsqlConnection connection,
      NpgsqlTransaction transaction,
      string correlationType,
      string correlationId,
      string title,
      string url,
      string? excerpt,
      DateTimeOffset observedAt,
      CancellationToken cancellationToken
   )
   {
      var sql = $"""
         insert into sources (
            id,
            correlation_type,
            correlation_id,
            kind,
            url,
            title,
            excerpt,
            observed_at
         )
         values (
            @id,
            @correlation_type,
            @correlation_id,
            @kind,
            @url,
            @title,
            @excerpt,
            @observed_at
         )
         on conflict (correlation_type, correlation_id, url)
            where kind = '{SourceKinds.StreamLink}'
         do update set
            title = excluded.title,
            excerpt = excluded.excerpt,
            observed_at = excluded.observed_at
         """;

      await using var command = new NpgsqlCommand(
         sql,
         connection,
         transaction
      );
      command.Parameters.AddWithValue("id", Guid.NewGuid());
      command.Parameters.AddWithValue("correlation_type", correlationType);
      command.Parameters.AddWithValue("correlation_id", correlationId);
      command.Parameters.AddWithValue("kind", SourceKinds.StreamLink);
      command.Parameters.AddWithValue("url", url);
      command.Parameters.AddWithValue("title", title);
      command.Parameters.AddWithValue(
         "excerpt",
         (object?)excerpt ?? DBNull.Value
      );
      command.Parameters.AddWithValue("observed_at", observedAt);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static IReadOnlyList<BroadcastStreamLink> NormalizeLinks(
      IEnumerable<BroadcastStreamLink> streamLinks
   )
   {
      return streamLinks
         .Select(NormalizeLink)
         .Where(link => link is not null)
         .Select(link => link!)
         .GroupBy(
            link => link.Url,
            StringComparer.OrdinalIgnoreCase
         )
         .Select(group => group.First())
         .ToArray();
   }

   private static BroadcastStreamLink? NormalizeLink(
      BroadcastStreamLink link
   )
   {
      if(
         string.IsNullOrWhiteSpace(link.ProviderName) ||
         !StreamLinkUrlNormalizer.TryNormalize(link.Url, out var normalizedUrl)
      )
      {
         return null;
      }

      return new BroadcastStreamLink(
         link.ProviderName.Trim(),
         normalizedUrl
      );
   }

   private sealed record StreamSource(
      string? Title,
      string Url,
      string? Excerpt,
      DateTimeOffset ObservedAt
   );
}
