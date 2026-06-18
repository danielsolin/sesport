using System;

namespace SESport.AI;

public static class ExecutionEnvironment
{
   public static string Current => BuildCurrent();

   private static string BuildCurrent()
   {
      var environmentName = Environment.GetEnvironmentVariable(
         "ASPNETCORE_ENVIRONMENT"
      );

      if(string.IsNullOrWhiteSpace(environmentName))
      {
         environmentName = Environment.GetEnvironmentVariable(
            "DOTNET_ENVIRONMENT"
         );
      }

      if(string.IsNullOrWhiteSpace(environmentName))
      {
         environmentName = "Production";
      }

      return $"{environmentName.Trim()}-{Environment.MachineName}";
   }
}
