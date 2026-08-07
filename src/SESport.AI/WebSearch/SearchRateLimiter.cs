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

   public async Task<bool> TryWaitAsync(
      string engine,
      CancellationToken cancellationToken
   )
   {
      if(GetEngineCooldownDelay(engine) > TimeSpan.Zero)
      {
         return false;
      }

      await requestGate.WaitAsync(cancellationToken);

      try
      {
         if(GetEngineCooldownDelay(engine) > TimeSpan.Zero)
         {
            return false;
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

         if(GetEngineCooldownDelay(engine) > TimeSpan.Zero)
         {
            return false;
         }

         ReserveNextRequestSlot();
         return true;
      }
      finally
      {
         requestGate.Release();
      }
   }

   public async Task WaitForAnyEngineAsync(
      IReadOnlyList<string> engines,
      CancellationToken cancellationToken
   )
   {
      while(true)
      {
         var cooldownDelay = GetEarliestEngineCooldownDelay(engines);

         if(cooldownDelay <= TimeSpan.Zero)
         {
            return;
         }

         await Task.Delay(cooldownDelay, TimeProvider, cancellationToken);
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

   private TimeSpan GetEarliestEngineCooldownDelay(
      IReadOnlyList<string> engines
   )
   {
      var now = TimeProvider.GetUtcNow();
      var earliestCooldown = TimeSpan.MaxValue;

      lock(stateLock)
      {
         foreach(var engine in engines)
         {
            if(!engineCooldowns.TryGetValue(
               engine,
               out var cooldownUntil
            ))
            {
               continue;
            }

            var cooldownDelay = cooldownUntil - now;

            if(cooldownDelay <= TimeSpan.Zero)
            {
               engineCooldowns.Remove(engine);
               continue;
            }

            earliestCooldown = TimeSpan.FromTicks(
               Math.Min(
                  earliestCooldown.Ticks,
                  cooldownDelay.Ticks
               )
            );
         }
      }

      return earliestCooldown == TimeSpan.MaxValue
         ? TimeSpan.Zero
         : earliestCooldown;
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
