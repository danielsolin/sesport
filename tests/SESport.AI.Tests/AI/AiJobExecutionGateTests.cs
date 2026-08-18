using SESport.AI.Jobs;

namespace SESport.Core.Tests.AI;

public sealed class AiJobExecutionGateTests
{
   [Fact]
   public async Task AllowsDifferentProvidersAtTheSameTime()
   {
      var gate = new AiJobExecutionGate();

      await gate.WaitAsync("llama-local", CancellationToken.None);
      var codexWait = gate.WaitAsync(
         "codex-cli",
         CancellationToken.None
      ).AsTask();

      Assert.True(codexWait.IsCompletedSuccessfully);

      await codexWait;
      gate.Release("codex-cli");
      gate.Release("llama-local");
   }

   [Fact]
   public async Task BlocksTheSecondRunForTheSameProvider()
   {
      var gate = new AiJobExecutionGate();

      await gate.WaitAsync("llama-local", CancellationToken.None);
      var secondWait = gate.WaitAsync(
         "llama-local",
         CancellationToken.None
      ).AsTask();

      Assert.False(secondWait.IsCompleted);

      gate.Release("llama-local");
      await secondWait;
      gate.Release("llama-local");
   }
}
