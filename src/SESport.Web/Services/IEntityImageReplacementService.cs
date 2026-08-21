using SESport.Core.Sources;

namespace SESport.Web.Services;

public interface IEntityImageReplacementService
{
   Task ReplaceAsync(
      Guid entityId,
      WikimediaCommonsImageReference source,
      CancellationToken cancellationToken
   );
}

public sealed class EntityImageReplacementException : Exception
{
   public EntityImageReplacementException(string message)
      : base(message)
   {
   }

   public EntityImageReplacementException(
      string message,
      Exception innerException
   ) : base(message, innerException)
   {
   }
}
