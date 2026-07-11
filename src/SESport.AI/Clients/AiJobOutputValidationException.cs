namespace SESport.AI.Clients;

internal sealed class AiJobOutputValidationException
   : InvalidOperationException
{
   public AiJobOutputValidationException(string message)
      : base(message)
   {
   }
}
