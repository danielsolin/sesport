namespace SESport.AI.WebPages;

internal sealed class WebPageFetchException : Exception
{
   public WebPageFetchException(
      WebPageFetchErrorKind errorKind,
      string message,
      Exception? innerException = null
   )
      : base(message, innerException)
   {
      ErrorKind = errorKind;
   }

   public WebPageFetchErrorKind ErrorKind { get; }
}
