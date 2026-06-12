namespace SESport.Core.Domain;

public static class TrackedEntityTypeIds
{
   public const string Person = nameof(TrackedEntityType.Person);
   public const string NationalTeam = nameof(TrackedEntityType.NationalTeam);
   public const string Club = nameof(TrackedEntityType.Club);
   public const string RecurringEvent =
      nameof(TrackedEntityType.RecurringEvent);
   public const string Pair = nameof(TrackedEntityType.Pair);
   public const string Organization = nameof(TrackedEntityType.Organization);
   public const string Other = nameof(TrackedEntityType.Other);
}
