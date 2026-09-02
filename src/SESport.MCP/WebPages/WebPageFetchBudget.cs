namespace SESport.AI.WebPages;

/// <summary>
/// Total-timeout budget for one page fetch. The deadline token is linked to
/// the caller token; the budget tracks which one fired so the caller can
/// distinguish "the operator canceled" from "we ran out of time".
/// </summary>
internal sealed class WebPageFetchBudget : IDisposable
{
   private readonly CancellationTokenSource _deadlineTokenSource;
   private readonly CancellationToken _callerToken;

   internal WebPageFetchBudget(
      TimeSpan totalTimeout,
      CancellationToken callerToken
   )
   {
      _callerToken = callerToken;
      DeadlineUtc = DateTimeOffset.UtcNow + totalTimeout;
      _deadlineTokenSource =
         CancellationTokenSource.CreateLinkedTokenSource(callerToken);
      _deadlineTokenSource.CancelAfter(totalTimeout);
   }

   internal DateTimeOffset DeadlineUtc { get; }

   internal CancellationToken DeadlineToken => _deadlineTokenSource.Token;

   internal bool CallerCanceled => _callerToken.IsCancellationRequested;

   internal TimeSpan Remaining =>
      DateTimeOffset.UtcNow < DeadlineUtc
         ? DeadlineUtc - DateTimeOffset.UtcNow
         : TimeSpan.Zero;

   public void Dispose()
   {
      _deadlineTokenSource.Dispose();
   }
}
