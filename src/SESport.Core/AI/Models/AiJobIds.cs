namespace SESport.Core.AI;

public enum AiJobTargetType
{
   Unknown,
   Activity,
   Broadcast,
   Person
}

public static class AiJobIds
{
   public const string GenerateActivityTeaser =
      "generate-activity-teaser";

   public const string DecidePrimaryCountryParticipation =
      "decide-swedish-participation";

   public const string FindActivityFacts =
      "find-activity-facts";

   public const string FindPersonData =
      "find-person-data";

   public const string TranslateText =
      "translate-text";

   public static AiJobTargetType GetTargetType(string jobId)
   {
      return jobId switch
      {
         GenerateActivityTeaser or FindActivityFacts =>
            AiJobTargetType.Activity,
         DecidePrimaryCountryParticipation =>
            AiJobTargetType.Broadcast,
         FindPersonData or TranslateText =>
            AiJobTargetType.Person,
         _ => AiJobTargetType.Unknown
      };
   }
}
