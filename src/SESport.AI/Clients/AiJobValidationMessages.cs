using SESport.Core.AI;
using SESport.Core.Domain;

namespace SESport.AI.Clients;

internal static class AiJobValidationMessages
{
   public const string ParticipantSourceEvidenceTypeMismatch =
      "Participant source EvidenceType must match fetched source.";

   public const string ParticipantSourceMustNameParticipant =
      "Participant source must name the participant.";

   public const string ParticipantMentionSourceTargetCountryMessage =
      AiParticipationEvidenceTypeIds.ParticipantMention +
      " source must name the participant and target country.";

   public const string ParticipantSourcesMustBeFetchedMessage =
      "Participant sources must be fetched with " +
      WebToolNames.GetPage + " or " + WebToolNames.FindInPage + ".";

   public const string SubmitReportRequiresSupportedParticipantMessage =
      WebToolNames.SubmitReport +
      " requires at least one supported participant";
}
