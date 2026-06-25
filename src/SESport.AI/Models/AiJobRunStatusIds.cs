namespace SESport.AI.Models;

public static class AiJobRunStatusIds
{
   public const string Pending = "pending";

   public const string Running = "running";

   public const string Completed = "completed";

   public const string Failed = "failed";

   public const string Archived = "archived";

   public static readonly string[] DefaultRunListStatuses =
   [
      Running,
      Pending
   ];
}
