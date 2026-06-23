namespace SESport.Core.Broadcast;

public sealed record BroadcastParticipationCheck(
   Guid RunId,
   string StatusId,
   int ToolRoundCount,
   string? Participation,
   IReadOnlyList<string> Participants,
   IReadOnlyList<string> SourceUrls,
   string? ErrorMessage
)
{
   public bool HasResult => !string.IsNullOrWhiteSpace(Participation);

   public bool IsPositive =>
      string.Equals(
         Participation,
         "Yes",
         StringComparison.OrdinalIgnoreCase
      );

   public string BadgeText => HasResult
      ? Participation ?? StatusId
      : StatusId;

   public string ParticipantsPreview
   {
      get
      {
         return string.Join(", ", Participants);
      }
   }

   public string ParticipantsPreviewNames
   {
      get
      {
         return string.Join(", ", Participants);
      }
   }

   public string SummaryText =>
      !string.IsNullOrWhiteSpace(ErrorMessage)
         ? ErrorMessage
         : HasResult
            ? (Participants.Count == 0
               ? BadgeText
               : $"{BadgeText}: {ParticipantsPreview}")
            : StatusId;
}
