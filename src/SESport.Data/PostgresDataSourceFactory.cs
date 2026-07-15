using Npgsql;

using SESport.Data.Configuration;

namespace SESport.Data;

public static class PostgresDataSourceFactory
{
   public static NpgsqlDataSource CreateDefault(
      string? configuredConnectionString
   )
   {
      return NpgsqlDataSource.Create(
         configuredConnectionString ??
         PostgresConnectionStrings.ResolveDefault()
      );
   }
}
