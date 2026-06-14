using System.Threading.Channels;

namespace SESport.Web.Services;

public sealed class BroadcastParticipationCheckQueue
{
   private readonly Channel<BroadcastParticipationCheckWorkItem> channel =
      Channel.CreateUnbounded<BroadcastParticipationCheckWorkItem>(
         new UnboundedChannelOptions
         {
            SingleReader = true,
            SingleWriter = false
         }
      );

   public ValueTask EnqueueAsync(
      IReadOnlyCollection<Guid> broadcastIds,
      CancellationToken cancellationToken
   )
   {
      if(cancellationToken.IsCancellationRequested)
      {
         return ValueTask.FromCanceled(cancellationToken);
      }

      return channel.Writer.WriteAsync(
         new BroadcastParticipationCheckWorkItem(
            broadcastIds.ToArray()
         ),
         cancellationToken
      );
   }

   public IAsyncEnumerable<BroadcastParticipationCheckWorkItem> DequeueAsync(
      CancellationToken cancellationToken
   )
   {
      return channel.Reader.ReadAllAsync(cancellationToken);
   }
}

public sealed record BroadcastParticipationCheckWorkItem(
   IReadOnlyList<Guid> BroadcastIds
);
