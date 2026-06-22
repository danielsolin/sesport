namespace SESport.Core.Broadcast;

public sealed record BroadcastEntityOption(
   Guid Id,
   string Name,
   string Type,
   string Sport,
   string Organization,
   string? AliasName = null
);
