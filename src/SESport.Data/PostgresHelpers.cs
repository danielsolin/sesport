using Npgsql;
using NpgsqlTypes;

namespace SESport.Data;

internal static class PostgresHelpers
{
   public static object BlankToDbNull(string? value)
   {
      return string.IsNullOrWhiteSpace(value)
         ? DBNull.Value
         : value.Trim();
   }

   public static object BlankToDbNullGuid(string? value)
   {
      return string.IsNullOrWhiteSpace(value)
         ? DBNull.Value
         : Guid.Parse(value.Trim());
   }

   public static void AddJsonbParameter(
      NpgsqlCommand command,
      string name,
      string? value
   )
   {
      var normalizedValue = PostgreSqlJson.Normalize(value);

      command.Parameters.Add(
         new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
         {
            Value = (object?)normalizedValue ?? DBNull.Value
         }
      );
   }

   public static string? ReadNullableString(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetString(ordinal);
   }

   public static Guid? ReadNullableGuid(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetGuid(ordinal);
   }

   public static int? ReadNullableInt32(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetInt32(ordinal);
   }

   public static decimal? ReadNullableDecimal(
      NpgsqlDataReader reader,
      int ordinal
   )
   {
      return reader.IsDBNull(ordinal)
         ? null
         : reader.GetDecimal(ordinal);
   }
}
