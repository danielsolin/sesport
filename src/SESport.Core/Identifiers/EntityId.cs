namespace SESport.Core.Identifiers;

public readonly record struct EntityId(Guid Value)
{
   public static EntityId New() => new(Guid.NewGuid());
}