namespace SESport.Core.Domain;

public sealed record ActivityTime(
   ActivityTimeKind Kind,
   DateTimeOffset? StartsAt,
   DateOnly? StartsOn,
   DateOnly? EndsOn,
   string? Description
)
{
   public static ActivityTime ExactStart(DateTimeOffset startsAt)
   {
      return new ActivityTime(
         ActivityTimeKind.ExactStart,
         startsAt,
         null,
         null,
         null
      );
   }

   public static ActivityTime DateRange(
      DateOnly startsOn,
      DateOnly endsOn,
      string? description = null
   )
   {
      if (endsOn < startsOn)
      {
         throw new ArgumentException(
            "Activity end date must be on or after the start date.",
            nameof(endsOn)
         );
      }

      return new ActivityTime(
         ActivityTimeKind.DateRange,
         null,
         startsOn,
         endsOn,
         description
      );
   }

   public static ActivityTime ToBeDetermined(string? description = null)
   {
      return new ActivityTime(
         ActivityTimeKind.ToBeDetermined,
         null,
         null,
         null,
         description
      );
   }
}
