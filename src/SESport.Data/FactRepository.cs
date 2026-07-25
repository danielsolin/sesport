using Npgsql;

using SESport.Core.Facts;

namespace SESport.Data;

public sealed class FactRepository(NpgsqlDataSource dataSource)
{
   public Task<FactRecord> CreateForActivityAsync(
      Guid activityId,
      string text,
      CancellationToken cancellationToken
   )
   {
      return CreateAsync(
         activityId,
         null,
         text,
         cancellationToken
      );
   }

   public Task<FactRecord> CreateForEntityAsync(
      Guid entityId,
      string text,
      CancellationToken cancellationToken
   )
   {
      return CreateAsync(
         null,
         entityId,
         text,
         cancellationToken
      );
   }

   public async Task<FactRecord?> GetAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            id,
            activity_id,
            entity_id,
            fact_text,
            created_at,
            updated_at
         from facts
         where id = @id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      return await reader.ReadAsync(cancellationToken)
         ? ReadFact(reader)
         : null;
   }

   public Task<IReadOnlyList<FactRecord>> GetForActivityAsync(
      Guid activityId,
      CancellationToken cancellationToken
   )
   {
      return GetForSubjectAsync(
         "activity_id",
         activityId,
         cancellationToken
      );
   }

   public Task<IReadOnlyList<FactRecord>> GetForEntityAsync(
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      return GetForSubjectAsync(
         "entity_id",
         entityId,
         cancellationToken
      );
   }

   public async Task<FactRecord?> UpdateAsync(
      Guid id,
      string text,
      CancellationToken cancellationToken
   )
   {
      var normalizedText = NormalizeText(text);
      const string sql = """
         update facts
         set
            fact_text = @fact_text,
            updated_at = now()
         where id = @id
         returning
            id,
            activity_id,
            entity_id,
            fact_text,
            created_at,
            updated_at
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("fact_text", normalizedText);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      return await reader.ReadAsync(cancellationToken)
         ? ReadFact(reader)
         : null;
   }

   public async Task<bool> DeleteAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = "delete from facts where id = @id";
      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);

      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }

   private async Task<FactRecord> CreateAsync(
      Guid? activityId,
      Guid? entityId,
      string text,
      CancellationToken cancellationToken
   )
   {
      var id = Guid.NewGuid();
      var normalizedText = NormalizeText(text);
      const string sql = """
         insert into facts (
            id,
            activity_id,
            entity_id,
            fact_text
         )
         values (
            @id,
            @activity_id,
            @entity_id,
            @fact_text
         )
         returning
            id,
            activity_id,
            entity_id,
            fact_text,
            created_at,
            updated_at
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue(
         "activity_id",
         (object?)activityId ?? DBNull.Value
      );
      command.Parameters.AddWithValue(
         "entity_id",
         (object?)entityId ?? DBNull.Value
      );
      command.Parameters.AddWithValue("fact_text", normalizedText);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );

      if(!await reader.ReadAsync(cancellationToken))
      {
         throw new InvalidOperationException("The fact was not created.");
      }

      return ReadFact(reader);
   }

   private async Task<IReadOnlyList<FactRecord>> GetForSubjectAsync(
      string subjectColumn,
      Guid subjectId,
      CancellationToken cancellationToken
   )
   {
      var sql = $"""
         select
            id,
            activity_id,
            entity_id,
            fact_text,
            created_at,
            updated_at
         from facts
         where {subjectColumn} = @subject_id
         order by created_at, id
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("subject_id", subjectId);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var facts = new List<FactRecord>();

      while(await reader.ReadAsync(cancellationToken))
      {
         facts.Add(ReadFact(reader));
      }

      return facts;
   }

   private static FactRecord ReadFact(NpgsqlDataReader reader)
   {
      Guid? activityId = reader.IsDBNull(1) ? null : reader.GetGuid(1);
      Guid? entityId = reader.IsDBNull(2) ? null : reader.GetGuid(2);

      return new FactRecord(
         reader.GetGuid(0),
         activityId is not null
            ? FactSubjectTypes.Activity
            : FactSubjectTypes.Entity,
         activityId ?? entityId ?? throw new InvalidOperationException(
            "The fact has no subject."
         ),
         reader.GetString(3),
         reader.GetFieldValue<DateTimeOffset>(4),
         reader.GetFieldValue<DateTimeOffset>(5)
      );
   }

   private static string NormalizeText(string text)
   {
      if(string.IsNullOrWhiteSpace(text))
      {
         throw new ArgumentException(
            "Fact text cannot be empty.",
            nameof(text)
         );
      }

      return text.Trim();
   }
}
