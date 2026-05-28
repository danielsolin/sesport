# Use Cases

SE Sport should eventually support user-defined views over sports events,
participants, people, rosters, and country relevance evidence.

These use cases depend on data availability. They describe product direction,
not guaranteed v1 behavior.

## Roster Threshold Alerts

A user can ask for upcoming events where a participating team has at least a
given number of people connected to a followed country.

Example:

```text
Show me NHL games starting within 48 hours where either team has at least
four Swedish players.
```

This requires:

- upcoming event schedules
- event participants
- team rosters
- person nationalities
- roster membership validity

## Derived Team Watchlists

A user can define a watchlist from a query result instead of manually selecting
teams.

Example:

```text
First, show which NHL team has the most Swedish players. Then show all
upcoming games for that team.
```

This requires:

- league or competition membership
- current team rosters
- person nationalities
- ranking or aggregation over roster evidence
- a way to save the resulting participant as a followed target
