using Npgsql;

using SESport.Core.Configuration;

namespace SESport.TestSupport;

public static class PostgresTestDatabase
{
   public static readonly DateOnly DistantActivityDate =
      new(2199, 12, 1);

   public static NpgsqlDataSource CreateDataSource()
   {
      return new NpgsqlDataSourceBuilder(
         PostgresConnectionStrings.ResolveDefault()
      ).Build();
   }
}
