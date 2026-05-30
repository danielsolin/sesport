# Source Strategy

SE Sport collects information in two stages.

## Stage 1: Entity Discovery

Identify sport-related entities that are relevant to a selected country.

Examples:

- athletes;
- national teams;
- clubs competing internationally;
- coaches;
- sport-related families or groups.

Entity discovery may be manual, assisted by AI, or automated.

The result should be a curated set of entities that are considered relevant for
that country.

## Stage 2: Activity Collection

Collect activities related to known entities.

Examples:

- matches;
- races;
- tournaments;
- championship events;
- qualification events;
- roster announcements;
- transfers;
- other international sport-related activities.

Different entities may require different collection methods.

## Design Principles

- Start with entities, not events.
- Preserve source information.
- Prefer explainable relevance.
- Allow manual verification and correction.
- Treat AI as an assistant, not as the source of truth.
- Store normalized data that can be queried later.

The system should always be able to explain:

```text
Why is this activity relevant to this country?
```

The answer should be traceable through one or more relevant entities.