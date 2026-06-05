namespace SESport.Core.AIActivityTeasers;

internal static class ActivityTeaserPrompt
{
   public static string Create(ActivityTeaserRequest request)
   {
      return $$"""
      Write a Swedish teaser for a sports activity.

      Requirements:
      - Use Swedish.
      - Use 15 to 25 words.
      - Be factual, clear, and editorial.
      - Do not hype, speculate, or mention that you are an AI.
      - Return only the teaser text, without quotes or explanations.

      Activity:
      - title: {{request.Title}}
      - description: {{request.Description}}
      - type: {{request.ActivityType}}
      - sport: {{request.Sport}}
      - date: {{request.ActivityDate?.ToString("yyyy-MM-dd")}}
      - local start time: {{request.LocalStartTime?.ToString("HH:mm")}}
      - time zone: {{request.TimeZoneId}}
      - Swedish-relevant entities: {{Format(request.Entities)}}
      - related entities: {{Format(request.RelatedEntities)}}
      """;
   }

   private static string Format(IReadOnlyCollection<string> values)
   {
      return values.Count == 0
         ? "none"
         : string.Join(", ", values);
   }
}
