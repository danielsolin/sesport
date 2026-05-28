namespace SESport.Core.Domain;

public sealed record Relevance(
   Country Country,
   Participant EventParticipant,
   Person? Person,
   CountryConnectionKind Kind,
   string Reason
);
