# `web_get_page` Fix — Reply to Review and Remediation Log

Date: 2026-09-02
Branch: `web-get-page-pipeline-refactor`
Baseline: `d4c5ffc7` (`Refactor web page fetch pipeline`)

This document is the reply to
`jobs/web_get_page_fix_status_review.md`. Every finding was re-verified
against the committed source before being accepted. Verdicts below are
final for the claims as written; the work log at the bottom tracks
implementation progress and is updated as work proceeds.

## Overall response

The review is correct. All seven P0 findings were verified against the
source and all are real. Where I initially believed a behavior was
enforced, the production path did not enforce it: the manual redirect
policy was dead code in production (the handler followed redirects first),
and the page-cache retry logic was shadowed by the generic duplicate-call
replay. The review's recommended order (B: cache/concurrency, C:
transport/SSRF, D: budget, E: decisions, F: OCR/PDF/observability) is the
order I am following.

No finding is disputed. Three places carry documented design decisions
that go beyond the letter of the review; they are flagged inline as
"Decision:".

## Finding verdicts

### P0 — Failed repeated page calls bypass the retry path

Confirmed. `ExecuteToolCallAsync` runs the generic
`LlamaToolCallHistory.TryGetRepeatedToolResult` before the
`web_get_page`/`web_find_in_page` branches, so an identical repeated
page call replays the recorded failure and the page-specific clean-cache
gate in `FormatPageContentAsync`/`FormatPageFindResultsAsync` never runs.

Fix: exclude page tools from the generic replay shortcut only. They keep
their own gated replay (clean-cache replay, retryable failures, negative
throttle).

Decision: page calls remain recorded in `ToolCallHistory`. The
per-turn repeated-call annotation ("do not make this identical call
again") still applies and is useful spam protection; only the blind
replay is removed.

### P0 — Partial results eligible for the clean page cache

Confirmed. `IsCachablePageContent` only checked null + error fields, so a
partial result (`Fetcher = "partial"`, `RenderWarning` set) was cached as
clean.

Fix: strict clean-success predicate: no `FetchErrorMessage`, no
`FetchErrorKind`, no `RenderWarning`, `HasBodyText` true, non-empty
`MainTextFull`. Every path that builds non-clean content sets at least
one of those markers; the partial path is covered by `RenderWarning`.
Stale `PageFailureContent` entries are cleared when clean content is
stored for the same URL.

Decision: the uncachable bucket also covers partial (non-error) results.
A URL that keeps returning partial content still consumes its attempt
budget and then replays the last partial instead of hammering the network
forever. This keeps "partial remains retryable" true while bounding the
retries, and it reuses the existing negative-throttle mechanism without a
second one.

### P0 — In-flight and cache dictionaries not protected consistently

Confirmed. `PageContentCache` was read outside any lock, written in
`FetchAndRecordAsync` without a lock, and the in-flight entry was removed
only after a successful await — a faulted or canceled task would remain
in `PageInFlight` forever and poison every later call for that URL.

Fix: one lock object on `ToolLoopState` guards all four page
dictionaries; the cache read and the in-flight decision happen under that
lock; recording happens under that lock; the in-flight entry is removed in
a `finally` with a task-identity check. Only the caller that created the
shared task removes it; concurrent waiters await through
`WaitAsync(own token)` so their cancellation can neither strand the
creator's task nor be confused with an internal deadline.

### P0 — Production HTTP clients follow redirects automatically

Confirmed. Neither the Web registration
(`AiServiceCollectionExtensions`) nor the MCP registration
(`Program.cs`) configured `AllowAutoRedirect = false`, so the handler
followed redirect chains before the transport's per-hop policy could see
them. The manual redirect policy was dead code in production and the unit
tests never caught it because they inject fake message handlers.

Fix: both composition roots configure the primary handler with
`AllowAutoRedirect = false`. The same typed client performs OCR image
downloads, so image downloads also become manual;
`WebPageImageOcr.DownloadImageAsync` gains a small explicit bounded
redirect loop that validates every `Location` with the shared URL policy.

### P0 — SSRF protection validates literal hosts only

Confirmed. `WebPageUrlPolicy` checks syntax, scheme, and literal
blocked hosts/IPs; a hostname that resolves to a private or reserved
address passed. Browser navigation had no request-level policy at all.

Fix: a new shared destination-policy component with an injectable DNS
resolver:

- Separates syntactic URL validation (kept in `WebPageUrlPolicy`, still
  used for parsing links and arguments) from network-destination
  validation (mandatory before any request).
- Resolves the hostname and rejects unless every returned address is a
  public unicast destination. Explicit policy covers loopback,
  unspecified, private IPv4, link-local (incl. cloud metadata),
  multicast, site-local IPv6 (`fc00::/7`), CGNAT (`100.64.0.0/10`),
  documentation/benchmarking ranges, reserved space, and IPv4-mapped
  IPv6 (checked as the embedded IPv4 address).
- Applied to: direct HTTP initial URL and every manual redirect hop;
  curl initial URL and every manual redirect hop; Playwright requests via
  route interception; OCR image downloads and their redirects.

Decision (DNS rebinding boundary): pinning is implemented where the
stack allows a real pin, and the residual is documented per transport:

- Curl: the pre-resolved approved endpoint is passed with `--resolve`,
  which makes curl skip DNS entirely for that host:port. No TOCTOU.
- Direct HTTP: in addition to the pre-request check, the DI-registered
  primary handler installs a `SocketsHttpHandler.ConnectCallback` that
  re-resolves and re-validates at connect time and pins the connection to
  an approved endpoint (SNI/Host preserved). This closes the TOCTOU for
  direct HTTP.
- Playwright: route interception validates every http/https request
  before it is allowed and aborts unsafe targets. The browser performs
  its own resolution between our check and connect, so a rebinding TOCTOU
  remains for the browser transport. Accepted residual risk, documented:
  the check runs per request, subresource bytes are never returned to the
  caller, and the navigation destination is re-checked on every hop.

Policy rejection returns a structured failure (distinct error kind, no
resolver or internal-network details in the public message).

### P1 — Response-size limits not enforced at the transport boundary

Confirmed. Direct HTTP read unbounded bodies; curl wrote unbounded temp
files; the OCR stream read unbounded when `Content-Length` was absent.

Fix: a configured maximum response byte count (separate from the
character cutoff for returned text) enforced in every download path:

- Direct HTTP: reject a known-oversized `Content-Length` up front;
  stream unknown-length bodies with a counting cap.
- Curl: `--max-filesize` plus a post-download re-check (defense in
  depth). Temp file deleted on success, failure, cancellation, and
  process-start errors.
- Browser-rendered HTML: capped before extraction.
- OCR image downloads: counting cap also applies when
  `Content-Length` is unknown.

### P0 — Total deadline is not a real staged budget

Confirmed. The budget used wall-clock `DateTimeOffset.UtcNow`, direct
HTTP received the overall deadline token and could consume the whole
operation, OCR ran with the overall token, and the minimum-stage checks
were admission tests, not reservations.

Fix:

- Monotonic `Stopwatch` clock; the UTC deadline is kept only for
  diagnostics.
- A stage-token helper derives a linked cancellation token that fires
  when the remaining budget would drop below a caller-supplied reserve.
- Direct HTTP retries (not the first attempt) run with a reserve equal to
  the minimum budgets of the fallbacks the first response made eligible.
- The browser stage runs with a reserve for curl when curl is eligible.
- OCR runs with its own bounded token and is skipped (with a ledger
  reason) when its minimum budget is unavailable.
- Retry delays are skipped when no useful attempt fits in the remainder.
- Every budget-based skip is recorded in the ledger. Caller
  cancellation, internal stage timeout, and total-deadline timeout remain
  distinguishable outcomes.

### P0 — Status and content decision rules have contradictory edge cases

Confirmed. The assessor checked soft not-found markers before block
signatures, so a body containing both was terminal not-found. The
not-found early returns in the HTTP and curl branches duplicated logic
and discarded ledger evidence.

Fix:

- Block/challenge evidence is classified before soft not-found; a body
  with contradictory signals is blocked/ambiguous, never terminal
  not-found.
- One shared stage-decision function is used by direct HTTP and curl.
- 404/410 is terminal only on confirmed evidence: status plus
  not-found content assessment, or two independent transports both
  returning 404/410 without blocked evidence. Ambiguous 404/410
  (empty, or contradictory evidence) goes to an eligible independent
  transport first.
- 401/403/429/408/425/5xx and transport errors keep bounded retries plus
  browser/curl fallbacks; URL-policy violations are terminal with no
  retry; long block bodies can never become clean success because
  assessment runs before any length-based acceptance.
- Every final path (clean success, partial, not-found, blocked, PDF
  failure, timeout, policy failure) goes through one final-decision
  helper that includes the ledger summary and keeps the most informative
  earlier failure visible.

### P1 — Browser strategy success reported before content success

Confirmed. `ReportSuccess` ran when a strategy rendered without a
transport error, before the shared assessment said anything about the
content, so a strategy that rendered a block page became the preferred
strategy for the origin.

Fix: strategy health is updated only by clean assessed content with an
acceptable navigation status. Launch failures keep the existing
launch-failure cooldown. Partial and not-found renders no longer stop the
strategy loop from trying an eligible later strategy; the orchestrator
receives all attempts as evidence and makes the fallback decision.

### P0 — Final result and ledger handling not uniform on early exits

Confirmed. Early not-found and PDF failures built messages without the
ledger, so the "full ledger" claim in the old status document was not
true on those paths.

Fix: the ledger becomes structured attempt records (stage, strategy,
requested/effective URL, status, classification, elapsed time,
skip/retry reason, safe error summary) under one correlation id per
fetch. All exits route through the single final-decision helper above.
Detailed evidence goes to structured logs; the public tool payload keeps
its current shape with a concise ledger summary (contract unchanged).
Log lines carry no response bodies, cookies, or sensitive query data.

### P1 — OCR can change text without a full reassessment

Confirmed. The character-count-based error clearing was a design flaw.
Note: with the current constructors it is not reachable today, because
failure content does not carry `RelevantImages` — but the flaw would have
become reachable the moment that changed, so it is fixed properly.

Fix: OCR runs only for a viable candidate with insufficient text, useful
image candidates, and its minimum budget; it gets its own bounded token.
The OCR-augmented candidate is rebuilt and re-assessed with the shared
assessor; error fields are cleared only when the re-assessed candidate
is actually selected as clean. OCR transport errors stay separate from
page transport evidence. Image URLs are re-validated and size-capped per
download (already partly true; completed).

### P1 — PDF handling needs an explicit extraction/recovery contract

Confirmed. The curl call inside `HandlePdfAsync` could escape cancellation
as an unhandled exception, and a curl download that returned identical
bytes was re-parsed pointlessly.

Fix: PDF work is split into download validation, extraction, and
assessment steps with typed outcomes; the first extraction error is
preserved alongside alternate download evidence; identical bytes are not
re-extracted (content hash comparison); cancellation is converted to typed
evidence while caller cancellation is preserved.

Decision: image-only PDF rendering/OCR is an intentional non-goal and is
documented as such (the review permits "implemented and tested or
explicitly disabled and reported"). A PDF that yields no text is
reported as a typed extraction failure with the ledger.

## Work log

Updated as work proceeds. Each phase ends with a green build and the
focused test suite run.

### Phase B — cache correctness and concurrency — DONE

- [x] Exclude page tools from the generic replay shortcut (`IsPageTool`
  guard in `ExecuteToolCallAsync`; page tools keep their own gated replay;
  they remain recorded in `ToolCallHistory` so the per-turn repeat
  annotation still applies)
- [x] Strict clean-success cache predicate (no error fields, no
  `RenderWarning`, `HasBodyText`, non-empty `MainTextFull`); stale
  `PageFailureContent` cleared when clean content is stored
- [x] Single `PageStateLock` guards all four page dictionaries; cache
  read, in-flight decision, budget check, and recording all run under it;
  in-flight removed in `finally` with task-identity check; only the
  creator removes; waiters observe through `WaitAsync(own token)`
- [x] Canonical URL normalization at the cache boundary
  (`NormalizePageUrl`, same rule as the page-call-history signatures)
- [x] Tests: `WebPageCacheTests` (7 tests: retry within budget, replay
  after exhaustion, clean caching/sharing, partial not cached, concurrent
  duplicates share one fetch, faulted task cleaned up, waiter cancel does
  not strand creator) plus the loop-level regression test
  `LlamaServerGenerateAsyncRefetchesFailedPageCalls`

Result: 882 tests passing across the full solution (was 874).

### Phase C — transport and SSRF gaps

- [ ] `AllowAutoRedirect = false` in Web and MCP registrations
- [ ] Shared destination policy with injectable resolver (IPv4/IPv6)
- [ ] Direct HTTP: pre-check per hop + connect-time pinning
- [ ] Curl: `--resolve` pinning + per-hop checks
- [ ] Browser: route interception for navigations and subresources
- [ ] OCR: explicit bounded redirect following with policy checks
- [ ] Response-byte limits on HTTP, curl, browser, OCR; temp-file cleanup
- [ ] Tests: literal and resolved private/reserved targets rejected
  (IPv4, IPv6, mixed answers); public redirects work; private redirects
  rejected; oversized responses bounded

### Phase D — real time budget

- [ ] Monotonic clock in `WebPageFetchBudget`
- [ ] Stage tokens with reserves; reserves for HTTP retries, browser, OCR
- [ ] Budget-based skips recorded in the ledger
- [ ] Distinct outcomes for caller cancel / stage timeout / total timeout
- [ ] Tests: slow stages leave fallback reserves; clock changes cannot
  increase remaining time

### Phase E — decision ownership and result semantics

- [ ] Block evidence classified before soft not-found
- [ ] Shared stage-decision function for HTTP and curl
- [ ] Confirmed vs ambiguous 404/410
- [ ] Browser strategy health only from clean content; partial renders do
  not stop later strategies
- [ ] Single final-decision helper; ledger on every exit
- [ ] Tests: the review's decision table, including contradictory
  block/not-found evidence

### Phase F — OCR/PDF and observability

- [ ] OCR re-assessment; bounded OCR token; skip reasons
- [ ] PDF typed outcomes; no re-parse of identical bytes; image-only PDF
  documented non-goal
- [ ] Structured ledger with correlation id; safe structured logs
- [ ] `jobs/web_get_page.md` and this document updated to match the
  implementation

## Status

In progress. The branch remains "implemented refactor with known
follow-up blockers" until every acceptance criterion in the review is met
and verified.
