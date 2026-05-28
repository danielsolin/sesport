namespace SESport.Core.Domain;

public sealed record CountryConnection(
   Country Country,
   Participant EventParticipant,
   Person? Person,
   CountryConnectionKind Kind,
   string Reason
);
