namespace SESport.Web.Services;

internal sealed class PersonFactsValidationException(string message)
   : InvalidOperationException(message);
