namespace SESport.Core.Configuration;

public static class PostgresConnectionStrings
{
   private const string DefaultHost = "localhost";
   private const string DefaultPort = "5432";
   private const string DefaultDatabase = "sesport";
   private const string DefaultUser = "sesport";
   private const string DefaultPassword = "sesport";

   public static string ResolveDefault()
   {
      var explicitConnectionString =
         Environment.GetEnvironmentVariable("ConnectionStrings__Default");

      if(!string.IsNullOrWhiteSpace(explicitConnectionString))
      {
         return explicitConnectionString;
      }

      var host =
         Environment.GetEnvironmentVariable("SESPORT_POSTGRES_HOST") ??
         DefaultHost;
      var port =
         Environment.GetEnvironmentVariable("SESPORT_POSTGRES_PORT") ??
         DefaultPort;
      var database =
         Environment.GetEnvironmentVariable("SESPORT_POSTGRES_DB") ??
         DefaultDatabase;
      var user =
         Environment.GetEnvironmentVariable("SESPORT_POSTGRES_USER") ??
         DefaultUser;
      var password =
         Environment.GetEnvironmentVariable("SESPORT_POSTGRES_PASSWORD") ??
         DefaultPassword;

      return "Host=" + host +
         ";Port=" + port +
         ";Database=" + database +
         ";Username=" + user +
         ";Password=" + password;
   }
}
