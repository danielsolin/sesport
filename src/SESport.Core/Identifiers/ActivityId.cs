namespace SESport.Core.Identifiers;

public readonly record struct ActivityId(Guid Value)
{
   public static ActivityId New() => new(Guid.NewGuid());
}