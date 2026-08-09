namespace SESport.Core.AI;

public enum AiJobTargetType
{
   Unknown,
   Activity,
   ActivityGroup,
   Broadcast,
   Person
}

public static class AiJobIds
{
   public const string GenerateActivityTeaser =
      "generate-activity-teaser";

   public const string DecidePrimaryCountryParticipation =
      "decide-swedish-participation";

   public const string FindActivityGroupFacts =
      "find-activitygroup-facts";

   public const string FindParticipantsStart =
      "find-participants-start";

   public const string FindParticipantsResult =
      "find-participants-result";

   public const string FindPersonData =
      "find-person-data";

   public const string TranslateText =
      "translate-text";

   public static AiJobTargetType GetTargetType(string jobId)
   {
      return jobId switch
      {
         GenerateActivityTeaser or FindParticipantsStart or
            FindParticipantsResult =>
            AiJobTargetType.Activity,
         FindActivityGroupFacts => AiJobTargetType.ActivityGroup,
         DecidePrimaryCountryParticipation =>
            AiJobTargetType.Broadcast,
         FindPersonData or TranslateText =>
            AiJobTargetType.Person,
         _ => AiJobTargetType.Unknown
      };
   }
}
