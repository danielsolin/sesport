namespace SESport.Web.Validation;

internal sealed class PersonFactsValidationException(string message)
   : InvalidOperationException(message);
