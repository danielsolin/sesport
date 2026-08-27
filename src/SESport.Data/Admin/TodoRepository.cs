using Npgsql;

using SESport.Core.Domain;
using SESport.Data.Models;

namespace SESport.Data.Admin;

public sealed class TodoRepository(NpgsqlDataSource dataSource)
{
   public async Task<Guid> CreateAsync(
      string targetTypeId,
      string text,
      string? correlationId,
      CancellationToken cancellationToken
   )
   {
      if(!TodoTargetTypeIds.IsSupported(targetTypeId))
      {
         throw new ArgumentException(
            "Unsupported todo target type.",
            nameof(targetTypeId)
         );
      }

      var normalizedText = text.Trim();
      if(normalizedText.Length == 0)
      {
         throw new ArgumentException(
            "Todo text is required.",
            nameof(text)
         );
      }

      var id = Guid.NewGuid();
      const string sql = """
         insert into todos (
            id,
            target_type_id,
            text,
            correlation_id
         )
         values (
            @id,
            @target_type_id,
            @text,
            @correlation_id
         )
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      command.Parameters.AddWithValue("target_type_id", targetTypeId);
      command.Parameters.AddWithValue("text", normalizedText);
      command.Parameters.AddWithValue(
         "correlation_id",
         (object?)correlationId ?? DBNull.Value
      );
      await command.ExecuteNonQueryAsync(cancellationToken);

      return id;
   }

   public async Task<IReadOnlyList<TodoItem>> GetOpenAsync(
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         select
            id,
            target_type_id,
            text,
            correlation_id,
            created_at
         from todos
         where done_at is null
         order by created_at, id
         """;

      await using var command = dataSource.CreateCommand(sql);
      await using var reader = await command.ExecuteReaderAsync(
         cancellationToken
      );
      var todos = new List<TodoItem>();

      while(await reader.ReadAsync(cancellationToken))
      {
         todos.Add(ReadTodo(reader));
      }

      return todos;
   }

   public async Task<bool> MarkDoneAsync(
      Guid id,
      CancellationToken cancellationToken
   )
   {
      const string sql = """
         update todos
         set done_at = now()
         where id = @id
            and done_at is null
         """;

      await using var command = dataSource.CreateCommand(sql);
      command.Parameters.AddWithValue("id", id);
      return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
   }

   private static TodoItem ReadTodo(NpgsqlDataReader reader) => new(
      reader.GetGuid(0),
      reader.GetString(1),
      reader.GetString(2),
      reader.IsDBNull(3) ? null : reader.GetString(3),
      reader.GetFieldValue<DateTimeOffset>(4)
   );
}
