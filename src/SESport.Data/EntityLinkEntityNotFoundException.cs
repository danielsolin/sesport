namespace SESport.Data;

public sealed class EntityLinkEntityNotFoundException : Exception
{
   public EntityLinkEntityNotFoundException(
      Guid sourceEntityId,
      Guid targetEntityId,
      Exception innerException
   )
      : base("An entity in the requested link was not found.", innerException)
   {
      SourceEntityId = sourceEntityId;
      TargetEntityId = targetEntityId;
   }

   public Guid SourceEntityId { get; }

   public Guid TargetEntityId { get; }
}
