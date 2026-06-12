using SESport.Core.Formatting;

namespace SESport.AI.ActivitySearch;

internal static class ActivitySearchPrompt
{
   public static string Create(ActivitySearchRequest request)
   {
      var timeFrameStart = DateDisplay.Format(
         request.SearchDate.AddDays(-request.LookBackDays)
      );
      var timeFrameEnd = DateDisplay.Format(
         request.SearchDate.AddDays(request.LookAheadDays)
      );
      var activityTypes = string.Join(", ", Enum.GetNames<ActivityType>());
      var entityRoles = string.Join(", ", Enum.GetNames<ActivityEntityRole>());

      return $$"""
      You job is to find planned sports activites for a given entity.

      The time period provided is confirmed. Do not ask which dates, season,
      month, or period to focus on. Search for concrete sport-related
      activities connected the entity in the time frame provided.

      Do not stop after the first source-backed candidate. Continue searching
      until you either reach the maximum proposal count or no more source-
      backed activities can be found in the date range. Prefer distinct
      activities over duplicate reports about the same activity.

      If the entity is a person and evidence shows they are selected for a
      national-team or club roster, also search for upcoming matches or
      competitions for that team in the date range.

      This is a non-interactive batch search. Do not ask the user for
      clarification. The entity data and time frame is enough context to start
      searching.

      Entity data:
      - name: {{request.Entity.Name}}
      - country: {{request.Entity.Country}}
      - type: {{request.Entity.Type}}
      - sport: {{request.Entity.Sport.Name}}
      - notes: {{request.Entity.Notes}}

      Time frame: {{timeFrameStart}} to {{timeFrameEnd}}
      
      Maximum proposals: {{request.MaxProposals}}

      Return only JSON with this shape:
      {
        "proposals": [
          {
            "title": "Sweden vs Finland",
            "description": "Short factual explanation.",
            "activityType": "{{ActivityType.Match}}",
            "activityDate": "2026-06-01",
            "localStartTime": "19:00",
            "timeZoneId": "{{SportDay.TimeZoneId}}",
            "context": "Competition or surrounding context",
            "entityRole": "{{ActivityEntityRole.CompetesIn}}",
            "entityExplanation": "Why this entity is connected.",
            "confidence": 0.85,
            "evidence": [
              {
                "sourceName": "Source name",
                "uri": "https://example.test/source",
                "title": "Source page title",
                "summary": "What the source supports.",
                "rawExcerpt": "Short excerpt if available."
              }
            ]
          }
        ]
      }

      Use only these activityType values:
      {{activityTypes}}.

      Use only these entityRole values:
      {{entityRoles}}.
      """;
   }
}
