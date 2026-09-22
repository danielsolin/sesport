# `web_get_page` Behavior

This document describes the current `web_get_page` implementation in the
source tree. It covers the fetch order, fallback conditions, timeouts,
extraction, and caching behavior.

The central implementation is:

- `src/SESport.AI/WebPages/WebPageContentClient.cs`
- `src/SESport.AI/WebPages/WebPageHtmlPageFetcher.cs`
- `src/SESport.AI/WebPages/WebPageBrowserPageFetcher.cs`
- `src/SESport.AI/WebPages/WebPageCurlPageFetcher.cs`

## Summary

`web_get_page` does not start with curl. The normal order is:

```text
URL validation
└── Primary HttpClient request
    ├── Successful PDF → PdfPig extraction → result
    ├── Sufficient HTML → HTML extraction → result
    ├── Insufficient HTML → Playwright
    │   ├── Usable browser result → result
    │   └── Browser failure → original HTML → possibly curl
    ├── 401, 403, 429, or request failure → Playwright
    │   ├── Usable browser result → result
    │   └── Browser failure → curl
    └── Other unsuccessful HTTP status → error result
```

Curl is therefore a late fallback. Several error paths terminate without
ever invoking it.

## 1. Entry Points

Both the in-process Llama tool and the MCP tool delegate page fetching to
`IWebPageContentClient.FetchAsync`.

The MCP method is `WebPageTool.FetchPageAsync`. It maps `WebPageContent` to
`WebPageToolResponse`. If the operation is canceled, the MCP method logs the
cancellation and returns `null`.

The Llama tool performs its own URL validation and formats the result as text.
It also caches fetched page content by normalized URL for the duration of the
tool loop. See the caching section below.

## 2. URL Validation

Before any network request, `WebPageUrlPolicy` validates the supplied URL.

The URL must:

- be non-empty;
- be no longer than 2,048 characters;
- be an absolute URL;
- use `http` or `https`;
- contain a host;
- not use a blocked host or an explicitly non-public IP address.

Blocked targets include `localhost`, `*.localhost`,
`metadata.google.internal`, loopback addresses, link-local addresses, and
explicit private or otherwise reserved IP ranges.

An invalid URL returns `null`. No fetcher is invoked.

The policy validates the host written in the URL. It does not resolve a DNS
name and then validate every address returned for that name.

## 3. Total and Per-Stage Timeouts

`WebPageContentClient` creates a linked cancellation token for the complete
fetch and cancels it after 60 seconds.

The 60-second limit includes:

- the primary HTTP request;
- Playwright startup, navigation, scrolling, and extraction;
- curl fallback;
- image downloads and OCR;
- retry delays.

If this internal deadline expires, the client returns an error result with:

```text
FetchErrorKind: Timeout
Fetcher: timeout
```

Caller cancellation is different. It is propagated instead of being changed
into the internal timeout result.

Important consequence: a later fallback is not guaranteed to run. For
example, if the primary HTTP request consumes 30 seconds and Playwright uses
the remaining time, the total deadline can expire before curl starts.

Additional limits include:

- primary `HttpClient` timeout: 30 seconds;
- Playwright navigation timeout: 30 seconds;
- Playwright network-idle timeout: 30 seconds;
- Playwright scrolling timeout: 15 seconds;
- Playwright content-stability timeout: 15 seconds;
- curl `--max-time`: 30 seconds;
- Tesseract timeout: 30 seconds per image.

All of these stages are still bounded by the overall 60-second deadline.

## 4. Browser User-Agent Discovery

Before the primary request, the client obtains a browser-like User-Agent.

The first lookup lazily starts bundled Chromium and reads its major version.
The generated value resembles a Linux Chrome User-Agent. If browser startup
fails, the code uses a fallback based on Chrome major version 125.

The lazy result is shared by later calls in the process.

## 5. Primary HTTP Request

The first page request uses `HttpClient`, not curl or Playwright.

It sends:

- a browser-oriented `Accept` header;
- `Accept-Language: en-US,en;q=0.9`;
- `Upgrade-Insecure-Requests: 1`;
- Chromium-style client-hint headers;
- the discovered browser-like User-Agent.

### Request exception or timeout

If the primary request fails before producing a response, the client starts
Playwright without a primary response.

This includes:

- connection and DNS failures;
- TLS or other `HttpRequestException` failures;
- `TaskCanceledException` caused by the `HttpClient` timeout;
- `TimeoutException`.

If Playwright then fails, the next step is curl. There is no original HTML
body to use between Playwright and curl.

### Unsuccessful HTTP status

The status determines whether fallback is attempted:

| Status | Behavior |
| --- | --- |
| `401` | Playwright, then curl if needed |
| `403` | Playwright, then curl if needed |
| `429` | Playwright, then curl if needed |
| Other non-success status | Return `HttpError` immediately |

For example, a primary `404` or `500` does not invoke Playwright or curl.

### Successful PDF response

A response is considered a PDF when:

- its media type is `application/pdf`;
- its media type is `application/x-pdf`; or
- the supplied URL path ends in `.pdf`.

The complete response body is read and extracted with PdfPig. The extractor
groups words by their positions to preserve visually aligned rows better than
simple PDF content order.

An empty PDF, a PDF with no extractable text, or a PDF parsing exception
returns a PDF error result. It does not fall back to Playwright or curl.

The curl fallback does not have separate PDF handling.

### Successful non-PDF response

Every other successful response is read as text and enters the HTML path.

## 6. Initial HTML Extraction and Sufficiency Check

The primary HTML is first parsed without allowing real curl fallback. This
pass extracts:

- title;
- publication date metadata;
- headings;
- structured table rows;
- selected values from embedded JSON or application state;
- visible body text;
- relevant links;
- relevant image candidates.

Template artifacts and common page chrome are removed from the returned page
text. The extraction also recognizes known block-page phrases and soft
not-found phrases.

The primary response is considered sufficiently rich only when all of these
conditions are true:

- extraction returned usable content;
- no fetch error was recorded;
- no `RenderWarning` was produced;
- cleaned visible HTML text contains at least 1,000 characters.

Embedded state may contribute to returned text, but it does not satisfy the
1,000-character visible-text requirement by itself.

When the content is sufficiently rich, it is returned with:

```text
Fetcher: html
```

Otherwise, the pipeline starts Playwright.

The same HTML parser is used for both this initial assessment and the later
fallback. Its log message can therefore mention an HTML fallback during the
assessment even though Playwright starts afterward.

## 7. Playwright Rendering

Playwright has five browser strategies, in this order:

1. bundled Chromium with the generated User-Agent;
2. the `chromium` channel with its own User-Agent;
3. the `chrome` channel with its own User-Agent;
4. bundled Firefox with its own User-Agent;
5. bundled WebKit with its own User-Agent.

Each browser is headless. The context uses locale `en-US` and a viewport of
1,440 by 2,400 pixels.

### Strategy memory

The process remembers attempted strategies by exact absolute URL.

- A strategy is reserved before it is launched.
- A successful strategy is still considered attempted.
- A later fetch of the same URL starts with the next unused strategy.
- Attempt history expires after 30 minutes.
- At most 1,000 URL histories are retained; the oldest are removed first.
- If all strategies are already recorded, browser rendering is skipped.

Within one fetch, an unusable result or a recognized browser failure advances
to the next unused strategy, as long as the overall deadline permits it.

### Navigation and page settling

For each strategy, Playwright:

1. starts the browser and creates a new context and page;
2. navigates while waiting for `DOMContentLoaded`;
3. continues with the current page if navigation times out;
4. waits for `NetworkIdle`, but continues after timeout or Playwright error;
5. scrolls downward in 75 percent viewport increments;
6. stops scrolling when the bottom is stable, after 20 steps, or after
   15 seconds;
7. scrolls back to the top;
8. samples body text until it is stable for three consecutive samples or
   until 15 seconds have elapsed;
9. extracts the rendered page.

### Rendered extraction

The browser path collects:

- title;
- navigation status when available;
- full rendered HTML;
- headings;
- normalized body text and embedded state;
- publication date metadata;
- relevant links;
- relevant image candidates;
- incomplete-content warnings.

Known access-denied and browser-verification text is classified as a blocked
page. Soft not-found text and non-success navigation statuses are classified
as HTTP errors.

A rendered result is usable when it:

- has no fetch error kind;
- has no fetch error message; and
- has body text or at least one relevant image candidate.

The 1,000-character minimum is not applied to a rendered result. A
`RenderWarning` also does not make an otherwise usable browser result fail.

A usable result is returned with:

```text
Fetcher: playwright
BrowserStrategy: <strategy identifier>
```

### Exhausted browser strategies

If no strategy succeeds, the browser fetcher prefers the last structured
browser error result, if one exists. Otherwise, it throws the last recognized
browser exception or returns `null`.

Recognized browser failures include `TimeoutException`,
`PlaywrightException`, and the internal `WebPageFetchException` wrapper.
Other exception types may escape and skip the normal HTML or curl fallback.

## 8. Fallback When Primary HTML Exists

When Playwright fails after a successful primary HTML response, the pipeline
reprocesses the saved original HTML.

At this stage, the earlier 1,000-character richness threshold is not used.
Any non-empty extracted text is accepted if it is not classified as blocked
or not found.

The outcomes are:

| Original HTML result | Behavior |
| --- | --- |
| Non-empty, not blocked | Return HTML content |
| Empty body | Try curl |
| No extracted text | Try curl |
| Known block page | Try curl |
| Soft not-found page | Return HTML `HttpError` |
| HTML extraction exception | Return HTML extraction error |

A successful HTML fallback normally does not retain the Playwright error
message. If the HTML result itself is an error, the Playwright message is
appended. A known browser strategy is retained separately.

## 9. Curl Fallback

Curl is invoked as a separate process with arguments equivalent to:

```text
curl --silent --show-error --location
     --proto =http,https
     --proto-redir =http,https
     --compressed
     --max-time 30
     --output -
     --write-out <status marker>
     <URL>
```

Curl does not receive the browser headers or User-Agent used by the primary
`HttpClient` request.

The implementation reads stdout and stderr, but only stdout is parsed. It
does not explicitly inspect the process exit code. The body and final HTTP
status are separated using a private marker written by curl.

The result is accepted only when:

- the marker is present;
- the final HTTP status is exactly `200`;
- extracted text is non-empty;
- no soft not-found signature is detected;
- no known block-page signature is detected.

A successful result has:

```text
Fetcher: curl
```

A non-null curl error result is terminal. There is no fetcher after curl.

## 10. Image OCR

After the page fetch returns, the client checks for relevant image candidates.
Candidates can come from the primary HTML, rendered HTML, or both.

At most three distinct candidates are processed. Images are selected using
semantic hints such as `entry`, `start`, `result`, `participant`, and `list`,
or by minimum dimensions and area.

For each candidate:

1. the image URL is validated with `WebPageUrlPolicy`;
2. the image is downloaded with `HttpClient`;
3. the response must have an `image/*` media type;
4. the image must not exceed 10 MiB;
5. Tesseract runs with English language data and page-segmentation mode 3;
6. output is accepted only with at least six words and mean confidence 60.

Accepted OCR text is appended to the full page text with the image URL as a
label. The client then marks the result as having body text.

Most Tesseract failures are logged and ignored. A network exception during
the image download can reach the outer transient retry loop.

OCR does not clear an existing fetch error. A result can therefore contain
both appended OCR text and fetch-error metadata.

## 11. Text Cutoff

The externally returned main text is limited to 50,000 characters.

When the full text is longer, the returned value ends with:

```text
[CUTOFF]
```

`MainTextFull` remains available internally. `web_find_in_page` uses the full
text so it can find content beyond the `web_get_page` cutoff.

## 12. Transient Retry Loop

The client has a maximum of three fetch attempts. It retries only an
`HttpRequestException` that escapes the complete fetch attempt.

The normal primary-request `HttpRequestException` path is caught inside
`FetchOnceAsync` and immediately enters Playwright and curl fallback. It does
not activate the outer retry loop.

Escaping transient failures wait:

- two seconds after the first failed attempt;
- five seconds after the second failed attempt.

The final escaping `HttpRequestException` is not caught by the retry filter
and may propagate to the caller.

## 13. Llama Tool-Loop Caching

`LlamaServerClient` stores `WebPageContent` in a case-insensitive dictionary
using the normalized URL as the key.

Within one tool loop:

- the first fetch stores the result, including `null` or an error result;
- another `web_get_page` call for the same URL reuses the stored result;
- `web_find_in_page` for the same URL also reuses that page content;
- no new HTTP, browser, curl, or OCR operation is performed for that URL.

An identical repeated `web_get_page` call also reuses its previously formatted
tool result.

The MCP server does not have this page-content cache. Its browser strategy
history is process-wide, however, and still affects repeated MCP calls.

## 14. Result Metadata

The internal `WebPageContent` record can identify these fetchers:

| `Fetcher` | Meaning |
| --- | --- |
| `html` | Extracted from the primary or fallback HTML |
| `playwright` | Extracted from a rendered browser page |
| `curl` | Extracted from curl output or failed after curl |
| `pdf` | Extracted from the primary PDF response |
| `http` | Primary HTTP failure with no fallback |
| `timeout` | The complete 60-second deadline expired |

Possible error kinds are:

- `BrowserBlocked`;
- `Timeout`;
- `HttpError`.

The MCP response exposes:

- title;
- URL;
- publication time;
- headings;
- cutoff main text;
- body-text presence;
- fetch error message and kind;
- fetcher;
- browser strategy;
- render warning.

It does not expose `MainTextFull`, relevant links, or image candidates.

## Important Behavioral Consequences

- Curl is not the primary fetcher.
- A primary `404` or `500` stops before Playwright and curl.
- A soft not-found HTML fallback stops before curl.
- A PDF extraction failure stops before Playwright and curl.
- The total timeout can prevent an otherwise configured fallback from running.
- The HTML richness threshold is only a browser-routing decision.
- Short HTML can still become the final fallback result.
- Short rendered text can be accepted as a successful browser result.
- A render warning does not reject an otherwise usable browser result.
- Repeated Llama calls for the same URL do not perform a fresh fetch.
- Repeated browser fetches of the same URL rotate through remembered browser
  strategies for 30 minutes.
