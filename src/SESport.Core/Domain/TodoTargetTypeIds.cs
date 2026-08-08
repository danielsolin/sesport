namespace SESport.Core.Domain;

public static class TodoTargetTypeIds
{
   public const string Broadcasts = "Broadcasts";
   public const string Activities = "Activities";
   public const string Entities = "Entities";

   public static bool IsSupported(string? targetTypeId) =>
      targetTypeId is Broadcasts or Activities or Entities;
}
