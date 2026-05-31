# AI Strategy

## Purpose

The first phase of SESport should be deliberately manual.

The goal is not to avoid automation, but to create a reliable editorial foundation before automation is introduced. By manually identifying and documenting important Swedish sports events day by day, we build a verified reference set for what actually matters.

This reference set becomes the basis for later AI-assisted discovery, filtering, classification, and summarization.

## Why start manually

AI systems are useful for searching, clustering, summarizing, and suggesting candidates. They are much weaker when the underlying definition of relevance is still vague.

For SESport, the key question is not simply:

> What Swedish sports news happened today?

The real question is:

> Which blue-and-yellow sports events are important enough to include?

That requires editorial judgment.

Starting manually allows us to define that judgment through real examples instead of abstract rules.

## What to collect

Each daily event should be stored in a structured and verifiable format.

Suggested fields:

```text
Date:
Title:
Sport:
Person or team:
Swedish connection:
Event type:
Why it matters:
Source 1:
Source 2:
Time sensitivity:
Priority: 1-5
Publish: yes/no
Notes:
```

When useful, rejected candidates should also be saved. These are valuable because they show the difference between a general Swedish sports item and something important enough for SESport.

## Expected value

After enough manually curated days, the dataset should help us identify:

- recurring source patterns
- recurring search terms
- sports or competitions that require manual attention
- types of events that are frequently missed by automated search
- weak signals that later turn out to be important
- common noise that should be filtered out

This creates a practical ground truth log for future automation.

## Later automation

Once the manual reference set is large enough, AI can be introduced gradually.

Possible AI-assisted steps:

1. Search for candidate events across known sources.
2. Extract structured event data.
3. Suggest relevance scores.
4. Explain why an event may or may not matter.
5. Compare new candidates against previous accepted and rejected examples.
6. Produce draft summaries for human review.

The important principle is that AI should first assist the editorial process, not replace it.

## Evaluation

The manual dataset should be used to test the automation.

Useful evaluation questions:

- Did the AI find the same important events as the manual process?
- Did it miss any high-priority events?
- Did it include too much low-value noise?
- Did it correctly understand the Swedish connection?
- Did it provide sources good enough for verification?
- Did its reasoning match the editorial standard?

## Guiding principle

Manual work in the beginning is not wasted work.

It is how SESport learns what relevance means before asking AI to scale the process.
