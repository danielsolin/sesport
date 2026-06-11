namespace SESport.Core.Broadcast;

public sealed record BroadcastParticipationCheck(
   Guid RunId,
   string StatusId,
   string? SwedishParticipation,
   IReadOnlyList<string> SwedishParticipants,
   string? ErrorMessage
)
{
   public bool HasResult => !string.IsNullOrWhiteSpace(SwedishParticipation);

   public bool IsPositive =>
      string.Equals(
         SwedishParticipation,
         "Yes",
         StringComparison.OrdinalIgnoreCase
      );

   public string BadgeText => HasResult
      ? SwedishParticipation ?? StatusId
      : StatusId;

   public string ParticipantsPreview
   {
      get
      {
         if(SwedishParticipants.Count == 0)
         {
            return "";
         }

         if(SwedishParticipants.Count <= 3)
         {
            return string.Join(", ", SwedishParticipants);
         }

         var preview = string.Join(", ", SwedishParticipants.Take(3));
         var moreCount = SwedishParticipants.Count - 3;

         return $"{preview} +{moreCount} more";
      }
   }

   public string SummaryText =>
      !string.IsNullOrWhiteSpace(ErrorMessage)
         ? ErrorMessage
         : HasResult
            ? (SwedishParticipants.Count == 0
               ? BadgeText
               : $"{BadgeText}: {ParticipantsPreview}")
            : StatusId;
}
