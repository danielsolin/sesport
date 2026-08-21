using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SESport.Core.Configuration;
using SESport.Web.Workers;

namespace SESport.Core.Tests.Workers;

public sealed class MemberPushNotificationWorkerTests
{
   [Fact]
   public void WorkerIsDisabledByDefaultEvenWhenPushIsConfigured()
   {
      var options = CreateConfiguredOptions();
      var environment = CreateEnvironment(Environments.Production);

      Assert.False(
         MemberPushNotificationWorker.IsWorkerAllowed(
            options,
            environment
         )
      );
   }

   [Fact]
   public void WorkerIsDisabledOutsideProduction()
   {
      var options = CreateConfiguredOptions() with
      {
         WorkerEnabled = true
      };
      var environment = CreateEnvironment(Environments.Development);

      Assert.False(
         MemberPushNotificationWorker.IsWorkerAllowed(
            options,
            environment
         )
      );
   }

   [Fact]
   public void WorkerIsEnabledForConfiguredProductionOptIn()
   {
      var options = CreateConfiguredOptions() with
      {
         WorkerEnabled = true
      };
      var environment = CreateEnvironment(Environments.Production);

      Assert.True(
         MemberPushNotificationWorker.IsWorkerAllowed(
            options,
            environment
         )
      );
   }

   private static MemberPushOptions CreateConfiguredOptions()
   {
      return new MemberPushOptions
      {
         Subject = "mailto:test@example.com",
         PublicKey = "public-key",
         PrivateKey = "private-key"
      };
   }

   private static IHostEnvironment CreateEnvironment(
      string environmentName
   )
   {
      return new TestHostEnvironment
      {
         EnvironmentName = environmentName
      };
   }

   private sealed class TestHostEnvironment : IHostEnvironment
   {
      public string EnvironmentName { get; set; } = string.Empty;

      public string ApplicationName { get; set; } = string.Empty;

      public string ContentRootPath { get; set; } = string.Empty;

      public IFileProvider ContentRootFileProvider { get; set; } =
         new NullFileProvider();
   }
}
