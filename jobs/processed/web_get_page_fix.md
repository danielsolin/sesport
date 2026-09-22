# `web_get_page` Fix Plan

Status: planning only. This document does not authorize or include any code,
configuration, test, deployment, or runtime change.

The current behavior is documented in `web_get_page.md`. This plan addresses
the structural causes behind the critical consequences listed at the end of
that document. It is not a collection of isolated fallback patches.

## 1. Goal

Make `web_get_page` a deterministic, best-effort page retrieval pipeline that:

- uses one explicit decision model for every fetch path;
- gives each useful fallback a realistic chance within the total deadline;
- applies the same content-quality rules to HTTP, Playwright, and curl;
- treats Playwright as the renderer for pages that need a browser;
- treats curl as an independent transport fallback, not as a browser
  substitute;
- handles HTML, PDF, and OCR results without contradictory success states;
- preserves the most useful failure cause and the complete attempt history;
- behaves predictably when the same URL is fetched repeatedly;
- rejects unsafe initial URLs and unsafe redirect targets;
- has no site-specific exceptions.

The public entry points and response shape should remain compatible unless a
separate contract change is explicitly approved.

## 2. Non-goals

This work should not:

- make curl the primary fetcher;
- add proxy services or site-specific workarounds;
- treat any non-empty text as a successful page fetch;
- force every legitimate short page to reach an arbitrary character count;
- hide browser failures merely because another path returned some text;
- turn migrations or the database into fetch-configuration storage;
- add a second permanent implementation beside the repaired pipeline.

## 3. Critical Problems to Fix

### P0: Inconsistent decisions

The current components use different definitions of success. Initial HTML is
routed by a richness threshold, rendered HTML can succeed with very little
text, and fallback HTML can become final merely because it is non-empty.

The fix must introduce one shared content assessment used by every transport
and extractor.

### P0: Fallbacks can be starved

The complete operation has one deadline, but earlier stages can consume nearly
all of it. A configured fallback is not useful if it normally starts without
enough time to finish.

The fix must give the orchestrator ownership of the time budget and reserve
time for later stages when they are still eligible.

### P0: HTTP status handling stops useful recovery

A primary `404` or `500` currently stops before Playwright and curl. Real
not-found responses should not cause expensive retries, but empty, blocked, or
contradictory error responses must not be treated the same as a confirmed
not-found page. Transient server failures must also be recoverable.

The fix must classify the status together with response evidence instead of
using one blanket rule for every non-success status.

### P0: Browser strategy history consumes successful strategies

A browser strategy that worked for a URL is currently remembered as consumed,
so a later fetch rotates away from it. Eventually all strategies can be skipped
for that URL even though a known working strategy exists.

The fix must remember preference and health, not consumption across calls.

### P0: Failures are cached as final page content

Within a Llama tool loop, `null` and error results are reused without another
fetch. A transient failure can therefore become permanent for the rest of the
loop.

The fix must cache stable successful content separately from failed attempts.

### P0: Failure reporting is lossy

Several paths discard an earlier, more informative failure. Other paths can
return extracted text while leaving an unrelated fetch error attached.

The fix must preserve all attempts and derive the final result from them in one
place.

### P1: Curl is not a complete transport implementation

Curl currently accepts only exactly `200`, ignores its process exit code and
standard error, does not share response classification, and handles only HTML.

The fix must make curl produce the same typed response evidence as direct HTTP
without pretending that curl is equivalent to Playwright.

### P1: PDF extraction is a terminal branch

A PDF extraction failure currently prevents any useful alternate download or
image-based recovery.

The fix must separate PDF download from PDF extraction and allow bounded,
appropriate recovery.

### P1: Retry behavior does not cover the normal transient path

The outer retry loop normally does not see primary HTTP failures because they
are caught earlier. The final escaping exception may still reach the caller.

The fix must move retries into the orchestrated attempt policy and return a
structured result for all expected failures.

## 4. Target Design

### 4.1 One orchestrator owns the decision tree

`WebPageContentClient` should delegate the operation to one orchestrator. Only
that orchestrator may decide:

- which stage runs next;
- whether an attempt is terminal;
- whether content is usable, partial, or unusable;
- how much time a stage receives;
- which candidate becomes the final result;
- which failure kind and message are returned.

HTTP, browser, curl, HTML, PDF, and OCR components should not select the next
fallback. They should perform one bounded task and report what happened.

### 4.2 Every attempt returns structured evidence

Introduce an internal attempt result that can represent, at minimum:

- transport and strategy name;
- requested and effective URL;
- start time and elapsed time;
- HTTP status and content type, when available;
- redirect information;
- response bytes or an extracted content candidate;
- transport, rendering, and extraction outcomes;
- block-page and not-found signals;
- warnings and a safe exception summary;
- whether cancellation came from the caller or the operation deadline.

Do not encode this information in an error-message string. The final public
result can remain compact while structured logs retain the attempt details.

### 4.3 Separate transport, extraction, and assessment

The pipeline must keep these three questions separate:

1. Did a transport retrieve a valid response?
2. Could the relevant extractor produce a content candidate?
3. Is that candidate useful enough for the requested operation?

This prevents a successful TCP or HTTP operation from becoming a content
success, and prevents an extraction warning from being mistaken for a
transport failure.

### 4.4 Use one shared content assessment

Assess every HTML-derived candidate with the same service, regardless of
whether the bytes came from direct HTTP, Playwright, or curl.

The assessment should return a classification such as:

- `Usable`;
- `Partial`;
- `NeedsRendering`;
- `Empty`;
- `Blocked`;
- `NotFound`.

The decision must use multiple signals, including:

- visible main text and body text;
- headings, title, and meaningful metadata;
- placeholder or client-rendering markers;
- block-page markers;
- soft not-found markers;
- render warnings;
- HTTP status;
- extraction errors.

Text length may be one configurable signal, but it must not be the sole
definition of quality. A concise legitimate page can be usable, while a long
block page must remain unusable.

Only `Usable` is a clean success. A `Partial` candidate may be returned when no
better result exists, but it must carry an explicit warning or error state.

### 4.5 Use an attempt ledger

Keep all attempt results in order for the duration of one fetch. The ledger is
the source for:

- selecting the best content candidate;
- selecting the final failure kind;
- structured logging;
- explaining why a fallback ran or was skipped;
- verifying time-budget behavior.

The last failure must not automatically replace a more useful earlier failure.
For example, a Playwright block result must remain visible if curl later fails
with a generic process error.

## 5. Target Decision Tree

### Step 1: Validate the URL

- Accept only supported schemes and valid public destinations.
- Resolve and validate the destination before connecting.
- Disable automatic redirect following in transports.
- Validate every redirect target before following it.
- Apply a configurable redirect limit.
- Record policy rejection as a terminal structured failure.

This closes the gap where an allowed written URL can redirect to a disallowed
destination.

### Step 2: Try direct HTTP

Direct HTTP remains first because it is cheaper and more deterministic than a
browser.

Handle its result as follows:

- For a usable `2xx` HTML response, return it immediately.
- For an insufficient or client-rendered `2xx` HTML response, preserve the
  candidate and continue to Playwright.
- For a `2xx` PDF response, enter the PDF path.
- For `401`, `403`, or `429`, continue to Playwright when the response can
  plausibly be improved by a browser.
- For transient transport errors, `408`, `425`, `429`, and `5xx`, apply the
  bounded retry policy and then continue to eligible fallbacks.
- For a clear `404` or `410` with consistent not-found evidence, return a
  terminal not-found result.
- For an empty, blocked, or contradictory `404` or `410`, continue to an
  independent transport before declaring not-found.
- For other permanent `4xx` responses, stop unless the evidence identifies a
  browser-recoverable access or challenge response.

An error response body must not become clean success merely because it contains
enough text.

### Step 3: Try Playwright when rendering can help

Playwright should run for JavaScript shells, incomplete HTML, access challenges,
and eligible direct-transport failures. It should not run blindly for a valid
binary PDF.

For each browser attempt:

- use the known healthy strategy for the origin first;
- try each eligible strategy at most once within the current fetch;
- assess extracted content with the shared assessment;
- return immediately only for a usable candidate;
- retain a partial candidate while trying another useful path;
- treat blocked, empty, and not-found pages as classifications, not success;
- retain render warnings in the result and attempt ledger.

A render warning may be non-terminal when good content was already captured,
but it must never be silently ignored.

### Step 4: Try curl as an independent transport fallback

Curl should run when direct HTTP and Playwright did not produce usable content
and a different HTTP stack could still change the outcome.

The curl component must:

- use `ProcessStartInfo.ArgumentList` or an equivalent safe argument API;
- use a shared, configured request profile where appropriate;
- enforce its stage deadline;
- inspect process exit code, standard error, and response headers;
- accept applicable successful `2xx` statuses, not only `200`;
- keep headers and body separate;
- identify the final status and content type;
- avoid automatic redirect following;
- return PDF bytes to the PDF extractor;
- return HTML to the shared HTML extractor and assessment;
- report a typed failure instead of a fabricated content result.

Curl must not be reported as success when the process failed, the response was
blocked, or extraction produced only an unusable candidate.

### Step 5: Apply bounded OCR only to viable candidates

HTML image OCR should run only when:

- a transport successfully produced a page candidate;
- text remains insufficient;
- useful image candidates exist;
- the OCR stage has enough remaining budget.

OCR text should be added to a new candidate and reassessed. An OCR success must
not clear an unrelated transport failure unless the orchestrator deliberately
selects the resulting candidate as usable.

Image-only PDFs need a PDF-specific rendering and OCR path. Do not send raw PDF
bytes through the HTML image logic.

### Step 6: Select the final result

At the end of the allowed attempts:

1. Return the best usable candidate, if one exists.
2. Otherwise return the best partial candidate with explicit diagnostics.
3. Otherwise return the most informative structured failure.

The selection rules must be deterministic and independently testable. They
must not depend on whichever component happened to finish last.

## 6. Time-Budget Policy

Replace independently competing timeouts with a budget coordinator based on a
monotonic clock.

All timeout, retry, threshold, and strategy limits must live in
`SESport.Core.Configuration` and be bound by each executable project.

The options should cover at least:

- total fetch timeout;
- direct HTTP attempt timeout;
- browser attempt timeout;
- maximum browser attempts per fetch;
- curl timeout;
- minimum fallback reserve;
- transient retry count and delays;
- redirect limit;
- content-assessment thresholds;
- OCR limits;
- browser preference and failure-cache lifetimes.

The coordinator must:

- pass a bounded cancellation token to every stage;
- prevent a stage from consuming a reserved fallback budget;
- skip a stage when it cannot receive a meaningful minimum budget;
- record why a stage was skipped;
- avoid retry delays when too little time remains for another attempt;
- distinguish caller cancellation from the internal total deadline.

The concrete defaults should be chosen from representative runtime cases. They
must not be scattered as literals through fetchers.

## 7. Browser Strategy Policy

Replace the exact-URL consumed-strategy set with two separate concepts:

- process-wide strategy availability and launch health;
- origin-scoped preferred strategy based on recent success.

Per fetch, maintain a new local attempted-strategy set.

The policy should follow these rules:

- a successful strategy becomes preferred, not consumed;
- the preferred strategy is tried first on the next fetch;
- a content failure does not automatically mark a browser binary unhealthy;
- a launch failure can temporarily mark that strategy unavailable;
- a site block can demote the strategy for that origin without disabling it
  globally;
- expired preference data returns to the configured default order;
- prior calls can never cause every browser strategy to be skipped;
- unavailable installed channels are detected without repeated long waits.

The strategy state must be thread-safe and observable in structured logs.

## 8. HTTP and Redirect Policy

Create one response-classification policy shared by direct HTTP and curl.

It should explicitly define:

- successful statuses;
- transient statuses;
- browser-recoverable access statuses;
- confirmed not-found responses;
- permanent client failures;
- redirect handling;
- acceptable content types and size limits.

Both HTTP implementations must use the same URL safety checks for each
resolved address and redirect. DNS rebinding and private redirect targets must
be considered in the implementation design, not only the original URL string.

## 9. PDF Policy

Separate these operations:

1. download PDF bytes;
2. validate content type and basic file shape;
3. extract text and metadata;
4. assess extracted content;
5. optionally render and OCR image-only pages.

When direct PDF extraction fails:

- retain the extraction error;
- allow one alternate download through curl when budget permits;
- retry extraction only when new bytes were actually obtained;
- use bounded PDF rendering and OCR only when configured;
- return a structured PDF failure if no usable content is recovered.

Playwright should be used only if the URL actually resolves to an HTML PDF
viewer or download interstitial, not as a generic binary PDF parser.

## 10. Retry Policy

Remove the outer retry loop once equivalent behavior exists in the
orchestrator.

Retries should apply only to operations likely to improve on repetition:

- transient network exceptions;
- configured transient HTTP statuses;
- selected browser launch failures;
- selected curl process failures.

Do not retry:

- confirmed not-found pages;
- URL policy violations;
- unsupported content types;
- deterministic parser failures on identical bytes;
- caller cancellation.

Every retry must respect the remaining budget and use configurable delays.
Expected operational failures must become structured results rather than escape
through the tool boundary.

## 11. Cache Policy

Change the Llama tool-loop cache so that:

- only clean successful content receives normal page-content caching;
- `web_get_page` and `web_find_in_page` can share that successful content;
- `null`, timeout, blocked, and error results are not cached as page content;
- a failed fetch can be attempted again later in the same loop;
- duplicate concurrent fetches for one URL can share the same in-flight task;
- cache keys use one canonical URL normalization rule;
- any negative throttling is short-lived, configurable, and distinct from the
  successful content cache.

Do not add a public force-refresh parameter unless a concrete caller needs it.
The internal model should nevertheless make refresh behavior explicit.

## 12. Error and Result Semantics

Define one final mapping from attempt evidence to the existing error kinds.
Add a new error kind only if the current set cannot truthfully represent a
required result.

The mapping must ensure that:

- a clean success has no fetch error;
- a partial result is visibly partial;
- a blocked response cannot become clean success due to page length;
- OCR cannot leave an obsolete error attached to a clean result;
- timeout is reported only when the deadline caused the final failure;
- browser-block evidence survives a later generic curl failure;
- a real HTTP status remains available in diagnostics;
- internal exceptions are logged safely and do not leak sensitive data.

The MCP response can remain backward compatible. Additional attempt detail
should initially go to structured logs rather than expanding the public tool
payload without a demonstrated need.

## 13. Implementation Phases

No phase in this section is performed by creating this document.

### Phase 1: Establish the internal contract

- Add the attempt result, content classification, ledger, and budget types.
- Adapt existing fetchers to report structured evidence.
- Keep the existing external response contract.
- Preserve behavior temporarily while moving decisions into one place.

Completion condition: one component owns final result selection, even if the
old decision rules are still reproduced during this phase.

### Phase 2: Centralize extraction and assessment

- Make direct HTML, rendered HTML, and curl HTML use the same extractor and
  assessor.
- Separate block, not-found, rendering-needed, partial, and usable states.
- Remove independent definitions based only on non-empty text.

Completion condition: identical HTML and status evidence receives the same
classification regardless of transport.

### Phase 3: Repair orchestration and budgeting

- Implement the target decision tree.
- Add status-aware fallback decisions.
- Add the monotonic budget coordinator and fallback reserve.
- Replace the ineffective outer retry loop.

Completion condition: each eligible stage either receives a meaningful budget
or has a recorded reason for being skipped.

### Phase 4: Repair Playwright strategy handling

- Replace consumed-strategy history with health and preference state.
- Use per-fetch attempt tracking.
- Feed render warnings and extracted output into the shared assessor.
- Ensure a known successful strategy remains preferred on repeated calls.

Completion condition: repeated calls do not rotate away from a healthy strategy
or exhaust all strategies through historical use alone.

### Phase 5: Normalize curl, PDF, and OCR

- Make curl report full transport evidence and support HTML and PDF responses.
- Add safe redirect handling shared with direct HTTP.
- Separate PDF download, extraction, and optional OCR.
- Make OCR produce a reassessed candidate without contradictory errors.

Completion condition: each of these components returns one typed attempt result
and never decides the final tool result itself.

### Phase 6: Repair caching and diagnostics

- Cache only stable successful page content.
- Deduplicate concurrent in-flight requests.
- Add structured attempt-ledger logging and final-decision logging.
- Keep the MCP tool contract compatible.

Completion condition: transient failures can recover on a later call and the
reason for every final result can be reconstructed from logs.

### Phase 7: Remove obsolete paths and update documentation

- Remove superseded retry, fallback, and strategy-consumption code.
- Verify that no fetcher still makes orchestration decisions.
- Update `web_get_page.md` to describe the implemented behavior.
- Document all new configuration keys and defaults.

Completion condition: there is one active pipeline and its documentation
matches the source.

## 14. Verification Plan

Repository instructions prohibit creating, changing, or running automated tests
until the operator says to commit. Therefore, none are part of the creation of
this plan. When execution and a commit are authorized, add focused automated
coverage before creating that commit.

The automated matrix should cover at least:

- rich server-rendered HTML returns after direct HTTP;
- a JavaScript shell proceeds to Playwright;
- a known browser strategy remains preferred on the next call;
- a failed browser strategy does not suppress all later browser attempts;
- short legitimate content can be usable;
- short placeholder content is not a clean success;
- a long block page is rejected;
- a render warning is preserved and affects final classification;
- confirmed `404` and `410` responses stop as not-found;
- ambiguous or blocked `404` responses can use an independent fallback;
- `401`, `403`, and `429` follow the configured browser path;
- `408`, `425`, and `5xx` use bounded retries and fallbacks;
- a browser timeout leaves enough reserved time for curl;
- curl inspects exit code, standard error, headers, and final status;
- successful non-`200` `2xx` responses are classified correctly;
- direct HTTP and curl classify identical HTML consistently;
- a textual PDF succeeds without Playwright;
- a failed PDF extraction can use a new curl download;
- identical failed PDF bytes are not pointlessly parsed again;
- image-only PDF recovery is bounded and optional;
- OCR cannot produce a clean result with a stale fetch error;
- a redirect to a private or otherwise disallowed target is rejected;
- a caller cancellation is distinct from the internal deadline;
- a failed URL can be fetched again in the same Llama tool loop;
- successful cached content is shared with `web_find_in_page`;
- concurrent identical requests share one in-flight fetch;
- the final error preserves the most informative earlier failure;
- no expected transport exception escapes the MCP tool boundary.

Live checks should use a small, agreed set of representative URLs and local
fixtures for deterministic error cases. Live tests must remain separately
gated and must not be the only verification of the decision rules.

## 15. Observability Requirements

For every fetch, emit one correlation identifier and structured events for:

- normalized URL and origin;
- stage and strategy;
- elapsed and remaining time;
- status and content type;
- content classification;
- fallback reason;
- skipped-stage reason;
- final candidate source;
- final error kind.

Do not log response bodies, secrets, cookies, or sensitive query data. Logging
must make it possible to answer why curl did not run, why a strategy was chosen,
and why a partial candidate won without reproducing the issue interactively.

## 16. Rollout Approach

Implement the phases incrementally, but keep one external pipeline. Use
behavior-preserving internal steps before changing fallback policy.

Before deployment:

- compare old documented cases with the new decision matrix;
- verify configuration binding in Web and MCP;
- confirm browser binaries and channels available in the publish environment;
- inspect structured logs from representative manual fetches;
- verify the published MCP build using its documented publish procedure.

If a temporary compatibility switch is needed for deployment safety, define an
owner and removal condition when adding it. Do not leave two permanent fetch
pipelines.

## 17. Definition of Done

The repair is complete only when:

- one orchestrator owns every fallback and final-result decision;
- all HTML paths share one content assessment;
- browser strategy success is preferred rather than consumed;
- eligible fallbacks cannot be accidentally starved by earlier stages;
- HTTP statuses have explicit, evidence-based behavior;
- curl, PDF, and OCR return consistent typed outcomes;
- transient failures are not cached as stable page content;
- redirects receive the same URL safety validation as initial URLs;
- the final result cannot contain contradictory success and error states;
- structured logs explain every attempt and final decision;
- focused automated and approved live checks pass;
- configuration and behavior documentation match the implementation;
- no site-specific parsing or transport exception has been introduced.

