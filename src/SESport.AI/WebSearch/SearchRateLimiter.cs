namespace SESport.AI.WebSearch;

public sealed class SearchRateLimiter
{
   private readonly SemaphoreSlim requestGate = new(1, 1);
   private readonly Lock stateLock = new();
   private readonly Dictionary<string, DateTimeOffset> engineCooldowns =
      new(StringComparer.OrdinalIgnoreCase);

   private DateTimeOffset nextRequestAt = DateTimeOffset.MinValue;

   public SearchRateLimiter()
      : this(new WebSearchRateLimitOptions(), null)
   {
   }

   internal SearchRateLimiter(
      WebSearchRateLimitOptions options,
      TimeProvider? timeProvider = null
   )
   {
      Options = options;
      TimeProvider = timeProvider ?? TimeProvider.System;
   }

   private WebSearchRateLimitOptions Options { get; }

   private TimeProvider TimeProvider { get; }

   public async Task WaitAsync(
      string engine,
      CancellationToken cancellationToken
   )
   {
      while(true)
      {
         var cooldownDelay = GetEngineCooldownDelay(engine);

         if(cooldownDelay > TimeSpan.Zero)
         {
            await Task.Delay(cooldownDelay, TimeProvider, cancellationToken);
            continue;
         }

         await requestGate.WaitAsync(cancellationToken);

         try
         {
            cooldownDelay = GetEngineCooldownDelay(engine);

            if(cooldownDelay > TimeSpan.Zero)
            {
               continue;
            }

            var requestDelay = GetGlobalRequestDelay();

            if(requestDelay > TimeSpan.Zero)
            {
               await Task.Delay(
                  requestDelay,
                  TimeProvider,
                  cancellationToken
               );
            }

            cooldownDelay = GetEngineCooldownDelay(engine);

            if(cooldownDelay > TimeSpan.Zero)
            {
               continue;
            }

            ReserveNextRequestSlot();
            return;
         }
         finally
         {
            requestGate.Release();
         }
      }
   }

   public void RegisterRateLimitedFailure(string engine)
   {
      RegisterCooldown(engine, Options.RateLimitedCooldown);
   }

   public void RegisterTransientFailure(string engine)
   {
      RegisterCooldown(engine, Options.TransientFailureCooldown);
   }

   private TimeSpan GetEngineCooldownDelay(string engine)
   {
      lock(stateLock)
      {
         if(!engineCooldowns.TryGetValue(engine, out var cooldownUntil))
         {
            return TimeSpan.Zero;
         }

         var delay = cooldownUntil - TimeProvider.GetUtcNow();

         if(delay <= TimeSpan.Zero)
         {
            engineCooldowns.Remove(engine);
            return TimeSpan.Zero;
         }

         return delay;
      }
   }

   private TimeSpan GetGlobalRequestDelay()
   {
      lock(stateLock)
      {
         var delay = nextRequestAt - TimeProvider.GetUtcNow();
         return delay <= TimeSpan.Zero ? TimeSpan.Zero : delay;
      }
   }

   private void ReserveNextRequestSlot()
   {
      lock(stateLock)
      {
         nextRequestAt = TimeProvider.GetUtcNow() +
            Options.MinimumRequestInterval;
      }
   }

   private void RegisterCooldown(string engine, TimeSpan cooldown)
   {
      var cooldownUntil = TimeProvider.GetUtcNow() + cooldown;

      lock(stateLock)
      {
         if(
            engineCooldowns.TryGetValue(engine, out var existingCooldown) &&
            existingCooldown >= cooldownUntil
         )
         {
            return;
         }

         engineCooldowns[engine] = cooldownUntil;
      }
   }
}
