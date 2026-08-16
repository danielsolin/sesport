namespace SESport.AI.WebPages;

internal sealed class WebPageFetchException : Exception
{
   public WebPageFetchException(
      WebPageFetchErrorKind errorKind,
      string message,
      Exception? innerException = null,
      string? browserStrategy = null
   )
      : base(message, innerException)
   {
      ErrorKind = errorKind;
      BrowserStrategy = browserStrategy;
   }

   public WebPageFetchErrorKind ErrorKind { get; }

   public string? BrowserStrategy { get; }
}
