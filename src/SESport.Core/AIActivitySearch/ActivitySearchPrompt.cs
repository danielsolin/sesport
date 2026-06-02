namespace SESport.Core.AIActivitySearch;

internal static class ActivitySearchPrompt
{
   public static string Create(ActivitySearchRequest request)
   {
      return $$"""
      You job is to find planned sports activites for a given entity.

      The time period provided is confirmed. Do not ask which dates, season,
      month, or period to focus on.

      Search for concrete sport-related activities connected to this Sweden-
      relevant tracked entity in the time frame provided.

      Do not stop after the first source-backed candidate. Continue searching
      until you either reach the maximum proposal count or no more source-
      backed activities can be found in the date range. Prefer distinct
      activities over duplicate reports about the same activity.

      If the entity is a person and evidence shows they are selected for a
      national-team or club roster, also search for upcoming matches or
      competitions for that team in the date range.

      This is a non-interactive batch search. Do not ask the user clarifying
      questions. The entity, sport, country connection, time frame, likely
      activity types, and suggested evidence sources are enough context to
      start searching.

      Time period of interest:
      {{request.SearchDate.AddDays(-request.LookBackDays):yyyy-MM-dd}} to {{request.SearchDate.AddDays(request.LookAheadDays):yyyy-MM-dd}}

      Entity:
      - watchlistId: {{request.Entity.WatchlistId.Value}}
      - name: {{request.Entity.Name}}
      - type: {{request.Entity.Type}}
      - sport: {{request.Entity.Sport.Name}}
      - notes: {{request.Entity.Notes}}

      Maximum proposals: {{request.MaxProposals}}

      Return only JSON with this shape:
      {
        "proposals": [
          {
            "title": "Sweden vs Finland",
            "description": "Short factual explanation.",
            "activityType": "Match",
            "activityDate": "2026-06-01",
            "localStartTime": "19:00",
            "timeZoneId": "Europe/Stockholm",
            "context": "Competition or surrounding context",
            "entityRole": "CompetesIn",
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

      Use only these activityType values when possible:
      Match, Race, Tournament, Stage, Championship, Qualification,
      RosterAnnouncement, Transfer, Ranking, CoachingRole,
      OtherSportingActivity.

      Use only these entityRole values when possible:
      CompetesIn, PlaysForContext, SelectedForRoster, TransferSubject,
      CoachingRole, RecurringEventEdition, RelatedOrganization, Other.
      """;
   }
}
