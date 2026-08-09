namespace SESport.Core.AI;

public static class AiAutomationEventIds
{
   public const string ActivityCreated = "activity-created";

   public const string ActivityGroupCreated = "activitygroup-created";

   public const string PersonCreated = "person-created";
}

public sealed record AiAutomationRuleListItem(
   Guid Id,
   string EventId,
   string JobId,
   string JobLabel,
   bool Enabled
);

public sealed class AiAutomationRuleEditModel
{
   public Guid? Id { get; set; }

   public string EventId { get; set; } =
      AiAutomationEventIds.ActivityCreated;

   public string JobId { get; set; } = string.Empty;

   public bool Enabled { get; set; } = true;
}
