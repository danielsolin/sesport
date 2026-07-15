using Npgsql;

using SESport.Core.Configuration;

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
