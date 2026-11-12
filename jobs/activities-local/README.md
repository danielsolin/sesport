# Standalone Local Activities Runner

## Goal and boundary

Build a Python workflow that executes `../activities.md` as one unattended run.

- Keep it independent of `SESport.AI`, its providers, queue, prompts, and MCP tools.
- Call the configured local Gemma 4 26B A4B inference server over HTTP.
- Keep `../activities.md` as the source of operational rules.
- At runtime, change application data and run reports only.
- Do not let the job change application code, assets, schema, configuration, or deployment.

## Operating contract

- Run from snapshot through final report without per-event approval.
- Continue after item-level uncertainty or errors; do not wait for an operator.
- Give each starting broadcast exactly one final outcome:
  - `Rejected`: hidden with a reason.
  - `Processed`: linked, marked processed, and hidden.
  - `Unresolved`: visible and unprocessed, with a reason and known organization.
- Apply supported changes to production during the run and inspect the result on
  `https://sesport.se`.
- Do not add a pre-publication approval gate; capture the live result before any
  evidence-backed correction.
- Record discrepancies as feedback for later workflow updates.
- Roll back an item on a failed write guard and continue with other items.
- Checkpoint completed work so an interrupted run can resume without duplicate writes.

## Pipeline

1. **Snapshot**
   - Record the start time and every visible broadcast, including processed records.
   - Save exact broadcast IDs, current state, fields, links, and relevant context.
   - Persist the snapshot and run state before changing data.

2. **Triage and group**
   - Apply clear eligibility and rejection rules in Python.
   - Ask Gemma to classify ambiguous broadcasts and suggest event groups.
   - Keep uncertain participation for research; do not infer it from a missing name.
   - Preserve the decision, reason, and evidence references for every ID.

3. **Match existing records**
   - Compare candidates with existing activities regardless of processed state.
   - Match by event, date, time, segment, and context.
   - Reuse exact matches and surface conflicts for an `Unresolved` outcome.

4. **Collect and synthesize evidence**
   - Use Python search and page-fetch adapters to collect external sources.
   - Store source URL, access time, type, relevant text, and a stable source ID.
   - Ask Gemma to extract facts only from supplied evidence and cite source IDs.
   - Research shared event and person facts once per run; repeat when context differs.
   - Keep each segment's times, channels, and links tied to its own evidence.

5. **Build a structured change plan**
   - Ask Gemma for proposed event, group, activity, participant, and person data.
   - Include exact input IDs, source IDs, proposed links, and explicit unknowns.
   - Let Python calculate dates, public date ranges, and affected card counts.
   - Reject malformed plans and route unsupported or conflicting facts to `Unresolved`.

6. **Validate and apply changes**
   - Convert validated plans to named Python operations, never model-written SQL.
   - Use the repository-approved database path and connection settings from `.env`.
   - Guard writes with exact IDs, expected prior state, and expected row counts.
   - Roll back on mismatches, read changes back, and checkpoint after each event.
   - Track every changed activity and link for verification.

7. **Verify changed data and live pages**
   - Verify only changed activity IDs, including detail activities and linked records.
   - Check times, streams, participants, person facts, profiles, and publication state.
   - Fetch affected public dates from `https://sesport.se` and inspect rendered cards.
   - Recheck card counts and explain differences from the pre-change counts.
   - Check the attention dashboard and process supported data corrections in the run.

8. **Reconcile and report**
   - Reconcile every starting ID to exactly one final outcome.
   - Write the dated report from the run journal and actual read-back results.
   - Keep the checkpoint until reconciliation and report generation both succeed.
   - Make report commit and push an explicit scheduled-run capability.

## Model and runner responsibilities

### Python runner

- Own run state, checkpointing, exact IDs, retries, dates, and final reconciliation.
- Own search and fetch operations, evidence storage, database access, and live checks.
- Validate model output against versioned JSON schemas before any write is planned.
- Apply only named operations with transaction guards and verified read-back.
- Continue unrelated work when an individual item becomes unresolved.

### Gemma

- Classify ambiguous broadcasts and suggest event groupings.
- Extract sourced facts and prepare structured event and activity proposals.
- Suggest concise Swedish public titles using the supplied naming rules.
- Return explicit uncertainty instead of filling gaps with model knowledge.
- Never issue SQL, shell commands, or direct database or site mutations.

Make calls per stage and event, with compact inputs. Keep model, prompt, schema, and
runner versions in each run record. JSON validity alone does not establish factual
correctness; validate evidence, IDs, links, and state transitions separately.

## Live feedback loop

For every detected discrepancy, save a feedback case containing:

- Run ID, stage, model and prompt versions, and affected broadcast/activity IDs.
- Proposed and stored values, verification result, and affected live URL.
- The first live result, plus any later evidence-backed correction.
- Relevant evidence IDs and a failure category, such as matching, sourcing, timing,
  persistence, or public rendering.

Use cases to improve the responsible Python rule, prompt, or validation step. Add each
confirmed case to a versioned replay set and activate a workflow update only after
the affected cases pass. Keep the regular run unattended; do not mutate active code
or prompts in place during a run. Live-page checks catch presentation and consistency
errors; factual errors also need independent evidence or a recorded correction.

## Delivery stages

1. Define typed schemas for run state, evidence, decisions, change plans, and feedback.
2. Build read-only snapshot, search, fetch, model, and validation adapters.
3. Replay historical runs and verify ID accounting, source use, and event matching.
4. Add guarded production writes, read-back, live-site verification, and reporting.
5. Add resumable scheduling, operational logs, and the feedback-driven update process.

Historical replay is development validation, not an approval gate for daily runs.
Once enabled, a daily run applies supported changes without waiting for a person.

## Decisions to settle during implementation

- Confirm the local inference endpoint and health-check behavior at runtime.
- Confirm how Python will use the repository-approved database access path.
- Define where feedback cases and the versioned replay set will live.
- Configure report commit and push authorization for unattended runs.
