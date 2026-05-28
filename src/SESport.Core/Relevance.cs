namespace SESport.Core;

public sealed record Relevance(
    Country Country,
    Participant Participant,
    string Reason
);
