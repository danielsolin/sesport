namespace SESport.Core.AIActivitySearch;

internal static class ActivitySearchPrompt
{
   public static string Create(ActivitySearchRequest request)
   {
      return $$"""
      You are finding current sports activity proposals for SESport.

      Time period of interest:
      {{request.SearchDate.AddDays(-request.LookBackDays):yyyy-MM-dd}} through
      {{request.SearchDate.AddDays(request.LookAheadDays):yyyy-MM-dd}}.

      This date range is confirmed. Do not ask which dates, season, month, or
      period to focus on.

      Search for concrete upcoming or very recent sport-related activities
      connected to this Sweden-relevant tracked entity. Prefer official
      schedules, federation pages, competition pages, team pages, and reliable
      news sources. Do not invent events. If evidence is weak, return an empty
      proposals array.

      Titles, descriptions, context, and entity explanations must be directly
      supported by the evidence. Do not add round names, seedings, champion
      status, or importance claims unless one of the evidence summaries or raw
      excerpts clearly supports that exact claim. If sources disagree, choose
      the narrower source-backed wording.

      Do not stop after the first source-backed candidate. Continue searching
      across the likely activity types until you either reach the maximum
      proposal count or no more source-backed activities can be found in the
      date range. Prefer distinct activities over duplicate reports about the
      same activity.

      If the entity is a person and evidence shows they are selected for a
      national-team or club roster, also search for upcoming matches or
      competitions for that team in the date range. Only propose those matches
      when the roster evidence connects the person to the team and schedule
      evidence confirms the match.

      This is a non-interactive batch search. Do not ask the user clarifying
      questions. The entity, sport, country connection, search date, likely
      activity types, and suggested evidence sources are enough context to
      start searching. If a search tool asks for clarification, choose the
      broadest reasonable interpretation for current international sport
      activity connected to this entity and continue.

      Search for activities from
      {{request.SearchDate.AddDays(-request.LookBackDays):yyyy-MM-dd}} through
      {{request.SearchDate.AddDays(request.LookAheadDays):yyyy-MM-dd}}. Treat
      this as the confirmed date range. Do not ask which dates or season to
      focus on.

      Entity:
      - watchlistId: {{request.Entity.WatchlistId.Value}}
      - name: {{request.Entity.Name}}
      - type: {{request.Entity.Type}}
      - sport: {{request.Entity.Sport.Name}}
      - Sweden connection: {{request.Entity.SwedenConnection}}
      - current status: {{request.Entity.CurrentRelationshipOrStatus}}
      - likely activity types: {{string.Join(", ",
         request.Entity.LikelyActivityTypes)}}
      - suggested evidence sources: {{request.Entity.SuggestedEvidenceSources}}
      - notes: {{request.Entity.Notes}}

      Search date: {{request.SearchDate:yyyy-MM-dd}}
      Maximum proposals: {{request.MaxProposals}}

      Suggested search queries:
      - "{{request.Entity.Name}}" "{{request.Entity.Sport.Name}}" schedule
        {{request.SearchDate:yyyy}}
      - "{{request.Entity.Name}}" "{{request.Entity.Sport.Name}}" fixtures
        squad {{request.SearchDate:yyyy}}
      - "{{request.Entity.Name}}" "{{request.Entity.Sport.Name}}" tournament
        {{request.SearchDate:yyyy}}
      - "{{request.Entity.Name}}" "{{request.Entity.Sport.Name}}" results
        ranking injury medal {{request.SearchDate:yyyy}}
      - "{{request.Entity.Name}}" "{{request.Entity.Sport.Name}}"
        "{{request.SearchDate:yyyy-MM-dd}}"
        "{{request.SearchDate.AddDays(request.LookAheadDays):yyyy-MM-dd}}"
      - "{{request.Entity.Name}}" {{request.Entity.SuggestedEvidenceSources}}

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
