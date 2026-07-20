using Npgsql;

using SESport.Core.Sources;

namespace SESport.Data;

public sealed class SourceReferenceRepository(NpgsqlDataSource dataSource)
{
   public async Task<SourceReference> CreateAsync(
      string correlationType,
      string correlationId,
      string kind,
      string url,
      string? title,
      string? excerpt,
      DateTimeOffset? observedAt,
      CancellationToken cancellationToken
   )
   {
      var id = Guid.NewGuid();
      var effectiveObservedAt = observedAt ?? DateTimeOffset.UtcNow;
      const string sql = """
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
         returning created_at
         """;

      await using var command = dataSource.CreateCommand(sql);
      AddCorrelationParameters(
         command,
         correlationType,
         correlationId,
         kind
      );
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("url", url);
      command.Parameters.AddWithValue(
         "title",
         (object?)title ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "excerpt",
         (object?)excerpt ?? DBNull.Value
      );
      command.Parameters.AddWithValue("observed_at", effectiveObservedAt);

      var createdAtValue = await command.ExecuteScalarAsync(
         cancellationToken
      );
      var createdAt = ToDateTimeOffset(createdAtValue);

      return new SourceReference(
         id,
         correlationType,
         correlationId,
         kind,
         url,
         title,
         excerpt,
         effectiveObservedAt,
         createdAt
      );
   }

   public async Task<SourceReference?> GetAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            id,
            correlation_type,
            correlation_id,
            kind,
            url,
            title,
            excerpt,
            observed_at,
            created_at
         from sources
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      return await reader.ReadAsync(cancellationToken)
         ? ReadSourceReference(reader)
         : null;
   }

   public async Task<IReadOnlyList<SourceReference>> GetByCorrelationAsync(
      string correlationType,
      string correlationId,
      string? kind,
      CancellationToken cancellationToken
   )
   {
      var sql = """
         select
            id,
            correlation_type,
            correlation_id,
            kind,
            url,
            title,
            excerpt,
            observed_at,
            created_at
         from sources
         where correlation_type = @correlation_type
            and correlation_id = @correlation_id
         """;

      if(!string.IsNullOrWhiteSpace(kind))
      {
         sql += " and kind = @kind";
      }

      sql += " order by observed_at desc, created_at desc, id desc";

      await using var command = dataSource.CreateCommand(sql);
      AddCorrelationParameters(
         command,
         correlationType,
         correlationId,
         kind
      );
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var sources = new List<SourceReference>();

      while(await reader.ReadAsync(cancellationToken))
      {
         sources.Add(ReadSourceReference(reader));
      }

      return sources;
   }

   public async Task DeleteAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = "delete from sources where id = @id";
      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   public async Task DeleteByCorrelationAsync(
      string correlationType,
      string correlationId,
      CancellationToken cancellationToken,
      string? kind = null
   )
   {
      var sql = """
         delete from sources
         where correlation_type = @correlation_type
            and correlation_id = @correlation_id
         """;

      if(!string.IsNullOrWhiteSpace(kind))
      {
         sql += " and kind = @kind";
      }

      await using var command = dataSource.CreateCommand(sql);
      AddCorrelationParameters(
         command,
         correlationType,
         correlationId,
         kind
      );
      await command.ExecuteNonQueryAsync(cancellationToken);
   }

   private static void AddCorrelationParameters(
      NpgsqlCommand command,
      string correlationType,
      string correlationId,
      string? kind
   )
   {
      command.Parameters.AddWithValue("correlation_type", correlationType);
      command.Parameters.AddWithValue("correlation_id", correlationId);
      if(!string.IsNullOrWhiteSpace(kind))
      {
         command.Parameters.AddWithValue("kind", kind);
      }
   }

   private static SourceReference ReadSourceReference(
      NpgsqlDataReader reader
   )
   {
      return new SourceReference(
         reader.GetGuid(0),
         reader.GetString(1),
         reader.GetString(2),
         reader.GetString(3),
         reader.GetString(4),
         ReadNullableString(reader, 5),
         ReadNullableString(reader, 6),
         reader.GetFieldValue<DateTimeOffset>(7),
         reader.GetFieldValue<DateTimeOffset>(8)
      );
   }

   private static string? ReadNullableString(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
   }

   private static DateTimeOffset ToDateTimeOffset(object? value)
   {
      return value switch
      {
         DateTimeOffset dateTimeOffset => dateTimeOffset,
         DateTime dateTime => new DateTimeOffset(
            DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
         ),
         _ => throw new InvalidOperationException(
            "The source reference was not created."
         )
      };
   }
}
