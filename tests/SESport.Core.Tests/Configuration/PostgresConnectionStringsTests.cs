using SESport.Core.Configuration;

using System.Data.Common;

namespace SESport.Core.Tests.Configuration;

[Collection("Postgres configuration")]
public sealed class PostgresConnectionStringsTests
{
   [Theory]
   [InlineData("semi;colon")]
   [InlineData("quote\"and'apostrophe")]
   [InlineData(" leading and trailing ")]
   public void PasswordIsPreservedAsOneConnectionSetting(string password)
   {
      const string key = "SESPORT_POSTGRES_PASSWORD";
      var previous = Environment.GetEnvironmentVariable(key);
      try
      {
         Environment.SetEnvironmentVariable(key, password);
         var builder = new DbConnectionStringBuilder
         {
            ConnectionString = PostgresConnectionStrings.ResolveDefault()
         };

         Assert.Equal(password, builder["Password"]);
         Assert.Equal(5, builder.Count);
      }
      finally
      {
         Environment.SetEnvironmentVariable(key, previous);
      }
   }
}

[CollectionDefinition("Postgres configuration", DisableParallelization = true)]
public sealed class PostgresConfigurationCollection;
