# Product Goal

The purpose of SE Sport is to answer a simple question:

```text
What international sports activities are connected to the entities that matter
for a selected country?
```

The system should:

1. Identify relevant sport-related entities.
2. Collect activities related to those entities.
3. Explain why those activities are relevant.

The first configured country is Sweden.

Examples of Swedish entities include athletes, national teams, clubs,
coaches, and sport-related families or groups.

The first public version should focus on proving that entity discovery and
activity collection work.

The goal is not comprehensive sports coverage. The goal is useful coverage of
activities connected to relevant entities.

## Product Boundary

SE Sport is not a sports celebrity product by default.

The default product should focus on activities with direct sporting relevance:
competition, participation, performance, selection, availability, transfers,
rankings, coaching roles, and other events that affect or describe an entity's
sport-related role.

Private-life, gossip, lifestyle, social media, dating, fashion, parties, and
similar public-personal attention should not be collected or shown by default.

Such material may be supported later as an explicit user opt-in, but it must be
clearly classified and kept separate from the default sporting experience.

A useful rule is:

```text
Sporting relevance by default. Personal/public context only by explicit opt-in.
```

## Product Priority

When making tradeoffs, prioritize:

- identifying relevant entities;
- collecting activities for those entities;
- explaining relevance;
- maintaining data quality.

Avoid work that turns the product into a generic sports calendar, results
service, sports news site, or sports celebrity feed.