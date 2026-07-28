namespace SESport.Data.Broadcast;

public sealed record BroadcastIgnoreRule(
   string Kind,
   string Value,
   string? SourceKey
);
