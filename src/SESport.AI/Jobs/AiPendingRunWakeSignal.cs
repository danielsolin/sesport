using System.Threading.Channels;

namespace SESport.AI.Jobs;

public sealed class AiPendingRunWakeSignal
{
   private readonly Channel<bool> notifications =
      Channel.CreateBounded<bool>(
         new BoundedChannelOptions(1)
         {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
         }
      );

   public void Notify()
   {
      notifications.Writer.TryWrite(true);
   }

   public async ValueTask WaitAsync(CancellationToken cancellationToken)
   {
      await notifications.Reader.ReadAsync(cancellationToken);
   }
}
