# `web_get_page` Fix Status Review and Practical Remediation Plan

Review date: 2026-09-02  
Branch: `web-get-page-pipeline-refactor`  
Checkpoint reviewed: `d4c5ffc7` (`Refactor web page fetch pipeline`)

## Scope

This document reviews the current pipeline refactor and defines a smaller,
practical remediation plan. It is a plan only. It does not change application
code, configuration, tests, runtime state, or Git history.

The goal is a reliable internal page-fetching capability. The goal is not to
build a general-purpose, internet-facing URL-fetching security product.

## Operating assumptions

The current deployment is an internal SESport installation on a local server:

- MCP is deployed on loopback by default.
- The public site does not expose the MCP endpoint directly.
- Manual AI job entry points are in the authenticated admin area.
- The normal input is an ordinary public web page.
- Predictable results, bounded resource use, and useful diagnostics matter
  more than a comprehensive threat model for every possible network topology.

These assumptions are part of the decision. If MCP becomes reachable by
untrusted callers, accepts URLs from untrusted users, or moves into a more
sensitive network, the network policy must be reviewed again.

For the current deployment, modest defence in depth is appropriate. A small
amount of URL and resource validation is useful, but a complex DNS and SSRF
subsystem would add failure modes and maintenance cost without a proportionate
benefit.

## Executive verdict

The refactor has a useful shape. A single orchestrator, typed transport
evidence, shared content assessment, browser fallback, and curl support are
good foundations.

The checkpoint is not ready to be considered complete. The remaining work
should focus on the behaviour users will notice:

1. Do not repeat expensive page calls unnecessarily.
2. Make retries, fallbacks, redirects, and timeouts predictable.
3. Bound response and temporary-file use.
4. Select the final result from consistent evidence.
5. Keep failures understandable and observable.

Current status:

> The core pipeline refactor is a sound direction, but cache correctness,
> timeout behaviour, transport limits, redirects, and final-result decisions
> still need focused remediation and verification.

## Parts to preserve

The following design decisions are useful and should remain unless an
implementation detail makes them demonstrably unreliable:

1. `WebPageFetchOrchestrator` owns the overall fetch flow.
2. HTTP and curl return structured evidence instead of only a string.
3. Content assessment is shared between the direct and fallback paths.
4. Results distinguish usable, partial, empty, blocked, rendering, and
   not-found outcomes.
5. Curl keeps response content and diagnostics separate.
6. Browser preference is a strategy choice, not a separate result model.
7. Existing public and MCP result shapes remain compatible where practical.

The checkpoint build and existing tests are useful baseline evidence, but they
do not by themselves prove the edge cases below.

## Findings and remediation plan

### 1. Make repeated page calls reliable

This is the most important operational issue. The fetcher can be called more
than once for the same page because of retries, fallback strategies, tool
replay, or concurrent requests. Those paths must not turn one page request into
an accidental request storm.

The cache must distinguish three cases:

- A clean, useful result that is safe to share for the configured cache time.
- A failed or empty result that may be retried when there is budget left.
- A partial result that is useful to the caller but should not suppress a
  better follow-up attempt.

Required changes:

- Exclude page-fetch calls from generic blind tool replay where the replay
  would bypass page-specific cache and retry rules.
- Cache only a clean result with usable body text and no error or warning.
- Keep failures and partial results separate from the clean-result cache.
- Use one canonical URL key for equivalent requests.
- Synchronize cache and in-flight dictionaries with one clear ownership rule.
- Remove in-flight entries in `finally`, using the same key and task identity
  that created the entry.
- Make cancellation ownership explicit so a cancelled caller does not leave a
  permanently stuck shared task.
- Keep any negative throttling short and bounded; it must not become a silent
  long-lived cache of failure.

Acceptance criteria:

- A failed first call can retry when the remaining budget allows it.
- A clean result is shared by later equivalent calls.
- A partial result does not masquerade as a clean cache hit.
- Concurrent equivalent calls perform one fetch where intended.
- Exceptions and cancellation clean up the in-flight state.
- Equivalent URL spellings use the intended cache key.
- Once the retry or time budget is exhausted, no extra call is attempted.

### 2. Make redirects explicit and predictable

Automatic redirects make it difficult to account for time, response size, and
the final URL. They also make diagnostics less useful. A small, explicit
redirect policy is worthwhile for stability even without a full DNS security
framework.

Required changes:

- Disable automatic redirects in the page-fetch HTTP client.
- Follow `Location` manually in the transport/orchestrator.
- Validate every redirect target with the existing basic URL policy.
- Make the maximum redirect hops configurable.
- Return a structured failure for invalid, looping, or over-budget redirects.
- Apply the same basic URL policy to downloaded OCR images where that path
  follows a URL.

Do not add a custom DNS resolver merely to implement manual redirects. For the
current loopback-only internal deployment, accepting normal hostname
resolution is a conscious and documented boundary.

Acceptance criteria:

- A normal public redirect reaches the final page within the hop limit.
- Each `Location` is checked before the next request is made.
- Redirect loops and excessive hops produce a useful bounded failure.
- A disallowed redirect is not fetched.
- The final URL is retained in the result and diagnostics.

### 3. Bound response and temporary-file use

Limiting extracted text is not enough. A response can consume memory before
content assessment, and curl, PDF, or OCR paths can consume temporary disk.

Required changes:

- Add a configurable maximum response size in bytes.
- Reject a declared `Content-Length` above the limit before downloading.
- Stream responses with a counting limit when the length is unknown.
- Treat a body exactly at the limit as valid; reject only bytes beyond it.
- Apply the limit consistently to direct HTTP and curl transports.
- Ensure browser, OCR, and PDF paths retain only bounded data.
- Remove temporary files in success, failure, cancellation, and timeout paths.

Acceptance criteria:

- No supported transport reads an unbounded response into memory.
- A response exactly at the configured limit succeeds.
- A response over the limit returns a typed, understandable failure.
- Temporary files do not accumulate after an unsuccessful operation.

### 4. Make the total timeout useful

The caller needs one understandable time budget for the complete page
operation. A stage timeout that is merely added to every fallback can make the
real operation much longer than expected.

Required changes:

- Measure the total budget with a monotonic stopwatch.
- Create a linked deadline token for each stage from the remaining budget.
- Do not start a retry or fallback when no useful time remains.
- Preserve caller cancellation as cancellation, rather than reporting it as a
  normal page failure.
- Distinguish an internal deadline from a caller-requested cancellation.
- Record when a strategy was skipped because of the remaining budget.

Do not add complex time reservations or scheduler machinery unless a measured
case requires it. A single total deadline and clear stage boundaries should be
enough.

Acceptance criteria:

- The complete operation cannot run past its configured total deadline,
  subject to unavoidable process and transport shutdown latency.
- A caller cancellation is distinguishable from an internal timeout.
- A fallback is not started when it cannot produce a useful result in time.
- Reported remaining time never increases during one operation.

### 5. Make final content decisions truthful

The transport status, body assessment, and fallback result must be combined in
a deterministic order. A useful page should not be replaced by a later weak
diagnostic, and an empty or blocked page should not be called successful just
because it returned HTTP 200.

Required changes:

- Use the same content assessment rules for direct HTTP and curl.
- Handle blocked or access-denied evidence before soft not-found heuristics.
- Confirm 404 and 410 outcomes from the independent transport evidence before
  declaring a page not found.
- Treat an empty or rendering-only 2xx response as a fallback candidate, not
  as successful content.
- Mark partial content explicitly and retain its useful text.
- Preserve the most useful failure when all strategies fail.
- Do not cache a result as clean when it contains a warning, error, or only a
  long access-denied page.

Keep the decision model small and deterministic. It should be possible to
explain the final outcome from the recorded evidence without reconstructing
the entire fetch process.

Acceptance criteria:

- Rich direct content is returned immediately.
- Empty or script-only direct content can trigger the browser fallback.
- A confirmed not-found response is reported without unnecessary expensive
  work.
- Conflicting transport evidence remains marked as uncertain or failed.
- Blocked content is not reported as a successful page.
- Partial content is available to the caller and is not falsely promoted to a
  clean result.

### 6. Keep browser strategy health separate from content success

Browser preference should be learned from successful page retrieval, not from
whether a browser process merely started or produced any HTML.

Required changes:

- Count browser strategy health only for a clean result with usable content.
- Keep launch, navigation, timeout, and content failures distinguishable.
- Do not let a partial browser result permanently suppress a later attempt.
- Keep origin preference observable with a small amount of diagnostics.

There is no need in this plan to build a complete security policy for every
browser subresource. The browser should follow the same top-level URL and
resource limits that the rest of the pipeline can reasonably enforce.

Acceptance criteria:

- A browser launch failure is not recorded as a healthy page-fetch strategy.
- A browser result with no useful content does not improve strategy health.
- A clean browser result can improve the preference for that origin.
- Health state cannot grow without bound or survive longer than configured.

### 7. Keep OCR and PDF handling bounded and honest

OCR and PDF extraction are useful fallbacks, but they should not turn a normal
page fetch into an unbounded second pipeline.

Required changes:

- Run OCR only when the image is useful, text is insufficient, and time
  remains.
- Use the same response and temporary-file limits for image downloads.
- Reassess the page after OCR before clearing an existing meaningful error.
- Keep image download failures distinct from OCR recognition failures.
- Support textual PDFs through the normal bounded extraction path.
- Allow at most one bounded alternate download attempt where it is needed.
- Do not download and process the same bytes repeatedly.
- Treat image-only or unsupported documents as an explicit limited outcome.

Acceptance criteria:

- OCR never starts after the total deadline has expired.
- OCR can improve an otherwise insufficient result without hiding a transport
  failure.
- PDF processing has bounded bytes, time, and temporary storage.
- Unsupported document cases are reported clearly and do not cause loops.

### 8. Keep diagnostics modest and useful

The existing ledger/result model is useful if it remains concise. Diagnostics
should answer what was tried and why the final result was selected.

Record, where available:

- stage and strategy;
- HTTP status or process outcome;
- final URL;
- failure category;
- skipped retry or fallback and its reason;
- final content decision.

Do not record page bodies, cookies, credentials, or complete query strings in
operational diagnostics. A correlation ID is useful only if it can be followed
through the relevant logs.

## Proportionate security boundary

The following basic protections are in scope because they are small and also
help predictable operation:

- accept only `http` and `https` URLs;
- reject obviously local hostnames and literal non-public IP addresses;
- check manually followed redirects with the same basic URL policy;
- bound response bytes, extracted text, and temporary files;
- return safe, structured policy failures.

The following are deliberately out of scope for this internal deployment:

- resolving every hostname and rejecting any mixed public/private answer;
- DNS rebinding detection and custom address pinning;
- `ConnectCallback`-based network enforcement;
- curl `--resolve` pinning;
- intercepting every Playwright navigation and subresource;
- a hand-maintained catalogue of NAT64, 6to4, IPv4-compatible, and other
  exotic IPv6 representations;
- a new network-isolation or general SSRF subsystem.

This is an explicit risk decision, not an assertion that those techniques are
never useful. The current loopback MCP boundary and internal operator model do
not justify making them prerequisites for a stable page fetcher. If the
exposure changes, service-account restrictions, a container/network namespace,
or an outbound proxy may be a better next step than more byte-level address
exceptions in application code.

## Recommended implementation order

### Phase A: Confirm the small contracts

- Confirm the result, cache, timeout, and failure contracts in code.
- Define the basic URL policy and configurable limits.
- Keep the current public and MCP contracts compatible.
- Capture a small baseline of representative fetch outcomes.

### Phase B: Finish cache and concurrency behaviour

- Separate clean cache entries from failed and partial results.
- Fix canonical keys, synchronization, and in-flight cleanup.
- Exclude page fetching from incompatible generic replay.
- Add focused regression coverage for retries and concurrent calls.

### Phase C: Stabilize transports

- Disable automatic redirects.
- Implement bounded manual redirects using the basic URL policy.
- Apply response-byte limits to all direct download paths.
- Verify temporary-file cleanup.

### Phase D: Stabilize time and final decisions

- Enforce one total deadline across retries and fallbacks.
- Make cancellation and internal timeout distinct.
- Make content/result selection deterministic.
- Adjust browser strategy health to follow clean content success.

### Phase E: Finish bounded OCR/PDF behaviour and documentation

- Keep OCR and PDF as limited, observable fallbacks.
- Add only the diagnostics needed to understand decisions.
- Document the deployment assumptions and the deliberate security boundary.

There is no separate DNS-hardening phase in this plan.

## Verification plan

Repository instructions prohibit creating, updating, or running automated tests
until the operator says to commit. Therefore this document does not authorize
test work now.

When the operator authorizes a commit, add or update the smallest relevant
automated coverage before creating it. The focused cases should include:

- failed call followed by an allowed retry;
- clean-result sharing;
- partial and failed results not entering the clean cache;
- concurrent equivalent calls and in-flight cleanup;
- canonical URL variants;
- public, looping, excessive, and disallowed redirects;
- exact-limit and over-limit responses;
- cancellation and total-timeout boundaries;
- rich, empty, blocked, partial, rendering, and not-found decisions;
- browser strategy health transitions;
- bounded OCR and PDF fallback outcomes.

Do not add a large DNS or exotic IPv6 test matrix for this scope.

After focused tests, run the relevant AI project checks and broader solution
checks in proportion to the final diff. Also run:

- `git diff --check`;
- a review of the staged diff before committing;
- configuration and deployment checks for the actual local service;
- direct MCP verification after publishing/restarting, if those operations
  are part of the requested handoff.

Useful live cases are a rich public page, a partial page, a JavaScript-heavy
page, a confirmed not-found page, a blocked page, a normal public redirect,
a disallowed redirect, a textual PDF, and a fallback-triggering page.

## Definition of ready

The refactor is ready for normal internal use when:

- failed calls can retry within the remaining budget;
- only clean useful results enter the clean cache;
- cache and in-flight state are synchronized and cleaned up;
- redirects are bounded, manual, and checked by the basic URL policy;
- response and temporary-file use are bounded;
- one total timeout covers the complete operation;
- cancellation and timeout are distinguishable;
- final statuses describe the evidence truthfully;
- browser health reflects clean content success;
- OCR and PDF fallbacks remain bounded and honest;
- focused and relevant solution checks pass;
- live verification matches the deployed configuration;
- documentation matches the deliberately limited security boundary.

The result should be described as a stable internal page-fetching pipeline,
not as a hardened public SSRF service.
