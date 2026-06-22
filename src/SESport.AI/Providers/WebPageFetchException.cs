namespace SESport.AI.Providers;

internal sealed class WebPageFetchException : Exception
{
   public WebPageFetchException(
      WebPageFetchErrorKind errorKind,
      string message
   )
      : base(message)
   {
      ErrorKind = errorKind;
   }

   public WebPageFetchErrorKind ErrorKind { get; }
}
