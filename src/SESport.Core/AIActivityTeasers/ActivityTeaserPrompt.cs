namespace SESport.Core.AIActivityTeasers;

public static class ActivityTeaserPrompt
{
   public static string Create(ActivityTeaserRequest request)
   {
      return $$"""
      Write the final teaser for a sports activity.

      Requirements:
      - Use Swedish.
      - Use 15 to 25 words.
      - Be factual, clear, and editorial.
      - Do not include reasoning, analysis, markdown, or extra keys.
      - Return a JSON object with only this property:
        - teaser: the final teaser text

      Activity:
      - title: {{request.Title}}
      - description: {{request.Description}}
      - type: {{request.ActivityType}}
      - sport: {{request.Sport}}
      - date: {{request.ActivityDate?.ToString("yyyy-MM-dd")}}
      - local start time: {{request.LocalStartTime?.ToString("HH:mm")}}
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
