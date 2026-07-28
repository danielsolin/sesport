namespace SESport.Data.Broadcast;

public sealed record BroadcastSaveResult(
   int SavedCount,
   int InsertedCount,
   int UpdatedCount
);
