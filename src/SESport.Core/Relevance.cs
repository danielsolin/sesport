namespace SESport.Core;

public sealed record Relevance(
   Country Country,
   Participant EventParticipant,
   Person? Person,
   string Reason
);
