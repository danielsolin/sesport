# SESport Web Tools — Improvement Status

After implementing a round of fixes, the three `sesport_web_*` MCP tools
were re-tested against the same real-world pages. This document records
what improved, what's still broken, and what to do next.

## Test URLs used

| URL | What it demonstrates |
|-----|----------------------|
| `https://www.diva-portal.org/smash/get/diva2:2030539/FULLTEXT01.pdf` | 51 KiB PDF (bachelor thesis, ~45 pages). Text extraction works, page numbers leak into text, math notation mangled, 50k cutoff mid-sentence. Used to test `web_find_in_page` with "Granger" and "Concluding". |
| `https://hurbra.se/us-open-30-augusti-2026/` | WordPress news site (HurBra.se). Heavy CSS/JS config noise in MainText, but article text is complete and readable. Headings now populated. |
| `https://hurbra.se/us-open-31-augusti-2026/` | Same site, second article. Same noise pattern. Used `web_find_in_page` with "Göransson" to locate the relevant passage. |
| `https://www.espn.com/tennis/us-open/bracket/_/season/2026/competitionType/3` | ESPN US Open Men's Doubles bracket. Client-side rendered — returns static shell full of "TBD" entries. `RenderWarning` now flags this. |
| `https://www.espn.com/tennis/us-open/bracket/_/season/2026/competitionType/4` | ESPN US Open Women's Doubles bracket. Same client-side rendering issue. |
| `https://www.usopen.org/en/USOpen/Draws/2026/mens-doubles` | Official US Open site. Returns HTTP 404 page ("This page does not exist. Try another serve?") as a successful response with `HasBodyText: true`. Not yet flagged. |
| `https://www.usopen.org/en/USOpen/Draws/2026/draws.aspx?event=mens-doubles` | Same 404-as-success issue. |
| `https://www.atptour.com/en/players/andre-goransson/ge03/overview` | ATP Tour player profile. Client-side rendered Vue.js app — Headings populated but full of template placeholders (`{{tournament.SponsorTitle}}`, "Header 2"). Page didn't hydrate. |
| `https://www.flashscore.com/tennis/match/andre-goransson_robert-galloway/` | Flashscore. Returns HTTP 404 — now properly flagged as `FetchErrorMessage`. |
| `https://usopentennisinfo.com/live-scores/` | US Open live scores aggregator. Static HTML, extracts cleanly. Good reference for a page that works well. |
| `https://www.sofascore.com/tennis/match/melo-smith-j-p-galloway-goransson/rjgkstjgk` | Search result only (not fetched). Winston-Salem Open match page. |

## What's been fixed

### 1. Headings extraction (was: always empty)
**Status: Fixed, with caveats.**

Server-rendered and static pages now return a populated `Headings` array
with heading level prefixes (`H1:`, `H2:`, etc.).

- **HurBra.se**: Returns a clean article outline — "US Open i dag 30
  augusti", "Djokovic börjar klockan 01.00", court names, "US Open på TV
  och streaming", etc. Exactly what was needed.
- **usopentennisinfo.com**: Clean hierarchy — "Live Tennis Scores",
  "Men's Singles", "Round of 128", FAQ questions, etc.
- **ATP Tour**: Headings are populated but full of Vue.js template
  placeholders (`{{tournament.SponsorTitle}}`, `{{newsContent.title}}`,
  "Header 2", "Header 3"). The page is a client-side app that didn't
  hydrate, so the raw template markup leaked through. Not useful as-is,
  but the presence of `{{...}}` placeholders is a signal that the page
  didn't render properly.

### 2. RenderWarning for placeholder content (was: silent TBD soup)
**Status: Fixed.**

ESPN's bracket page now returns:
```
RenderWarning: "Rendered page may be incomplete; placeholder content
was detected (TBD)."
```
The agent can now see that the page is a client-side shell and decide to
try another source, instead of silently consuming 40 "TBD" entries.

### 3. HTTP 404 detection for direct fetches (was: silent success)
**Status: Fixed for HTTP fetcher, not for Playwright.**

- **Flashscore** (direct HTTP): Now returns
  `FetchErrorMessage: "HTTP 404 Not Found while fetching ..."`,
  `HasBodyText: false`. Properly flagged as an error.
- **US Open** (Playwright-rendered): Still returns the 404 page content
  with `HasBodyText: true`, `Headings: ["H1: Let!"]`. The Playwright
  fetcher isn't checking the final HTTP status after navigation.

### 4. Search tool description (was: no operator hints)
**Status: Fixed.**

The `sesport_web_search` tool description now includes:
> "Standard search operators such as site:, filetype:, and quoted exact
> phrases are passed to SearXNG."

### 5. Fetcher metadata (new)
**Status: New, working.**

Each response now includes `Fetcher` ("html", "http", "playwright") and
`BrowserStrategy` (e.g. "chromium-bundled", "firefox-bundled"). Useful
for debugging and for the agent to understand what kind of rendering was
attempted.

## What's still broken

### A. Playwright fetcher doesn't check HTTP status after navigation
The US Open 404 page (`usopen.org/en/USOpen/Draws/2026/mens-doubles`)
returns a full error page ("This page does not exist. Try another
serve?") with `HasBodyText: true` and no error flag. The Playwright
fetcher navigates to the URL, the server returns 404, but the tool
treats the rendered error page as successful content.

**Fix**: After `page.goto()`, check `response.status()`. If 4xx/5xx,
set `FetchErrorMessage` and `HasBodyText: false`, same as the HTTP
fetcher does.

### B. Client-side rendered pages leak template placeholders
The ATP Tour player profile is a Vue.js app. The HTML fetcher (not
Playwright) grabbed the raw template markup:
- `Headings` full of `{{tournament.SponsorTitle}}`, "Header 2", "Header 3"
- `MainText` full of `{{playerDetails.BirthDate}}`, `:data-video-id=`,
  `0" @click="clearSearch"` — raw Vue.js directives and JS fragments

The page didn't hydrate because it was fetched with the plain HTML
fetcher, not Playwright. But even if Playwright were used, the
placeholder text would still leak into Headings if the app renders
skeleton/placeholder headings before data loads.

**Fix options**:
- Filter out headings containing `{{` or `}}` (template placeholders)
- If a page has `{{...}}` patterns in the first N characters of MainText,
  set `RenderWarning` similar to the TBD detection
- Consider whether ATP Tour could use Playwright instead of plain HTML
  (the URL pattern might be addable to a heuristic)

### C. HurBra.se MainText still has CSS/JS noise
The article text is complete and readable, but the first ~10% of
MainText is still CSS fragments, cookie consent text, and JS config
strings:
```
name: US Open 30 augusti 2026: Matcher, tider och TV | description: ...
search_term_string hurbra.se
art_title: eyJhbGwiOiIwIDAgN3B4IiwibGFuZHNjYXBlIjoiMCAwIDVweDciLCJwb3J0...
imageExt: jpg | jpeg | gif | png | tiff | bmp | webp | avif | pdf | doc | docx | xls | xlsx | php
placeholdertext: Klicka för att godkänna {category} cookies...
```

This is less of a problem than before (the article text follows cleanly)
but it wastes tokens and could confuse a model that doesn't know to skip
it.

**Fix**: The CSS/JS noise comes from `<style>` and `<script>` content
leaking into the text extraction. The extractor should strip these more
aggressively. The cookie consent text ("Funktionell", "Statistik",
"Marknadsfö...") is also noise that could be filtered.

### D. PDF extraction: page numbers and math notation
The 51 KiB bachelor thesis still has:
- Page numbers leaking into text ("2", "3", "4" between sections)
- Math notation mangled (`gt=α₀+α₁·IMMSTOCK+...` with subscripts broken)
- Table of contents dot leaders (`..........`)

This is a lower-priority issue since the text is still readable and
`find_in_page` works well, but it's worth noting that the 50k cutoff
still cuts mid-sentence.

**Fix options**:
- Strip standalone page number lines (single numbers 1-99)
- For math, either accept the loss or use a PDF library that preserves
  layout better
- The cutoff is a hard limit; `find_in_page` is the workaround

### E. `PublishedAt` still always null
No change. The extraction doesn't look for publication dates. HurBra.se
has a clear "Publicerad 30 augusti 2026" in the page, but it's not
extracted into the `PublishedAt` field.

**Fix**: Look for common date patterns near the article title, or parse
`<time>` elements, or check JSON-LD metadata.

## What works well (no changes needed)

- **`web_find_in_page`**: Still excellent. Full-document search beyond
  the 50k cutoff. "Granger" in the PDF returns 28 relevant lines.
  "Concluding" finds both the TOC entry and the actual section.
  "Göransson" in the HurBra article returns the relevant passage with
  context.
- **`web_search`**: SearXNG integration works. `site:`, `filetype:`,
  and quoted phrases are passed through. The description now reflects
  this.
- **Static HTML pages** (usopentennisinfo.com): Clean headings, clean
  text, no noise. The baseline case works well.
- **PDF text extraction**: Despite the page number and math issues, the
  text is complete and readable. 45 pages of a bachelor thesis all
  extracted, with the cutoff hitting at the right place.

## Summary table

| Issue | Status | Impact | Effort |
|-------|--------|--------|--------|
| Headings extraction | ✅ Fixed (static pages) | High | — |
| Headings on client-side pages | ⚠️ Leaks template placeholders | Medium | Low |
| RenderWarning for TBD | ✅ Fixed | High | — |
| HTTP 404 detection (HTTP fetcher) | ✅ Fixed | High | — |
| HTTP 404 detection (Playwright) | ❌ Still broken | High | Low |
| Search description | ✅ Fixed | Medium | — |
| CSS/JS noise in MainText | ⚠️ Reduced but still present | Medium | Low |
| PDF page numbers / math | ❌ Unchanged | Low | Medium |
| PublishedAt extraction | ❌ Unchanged | Medium | Medium |
| Fetcher metadata | ✅ New | Low | — |
