# Code Cleanup Review - 2026-09-21

Read-only review of `src/` and `tests/` (tools and data excluded; they are
private local workspace content). Build is clean (0 warnings), no unused
usings (IDE0005 is enforced as an error), no TODO/FIXME markers, no orphaned
Razor partials, and a method-level reference sweep found no dead methods.
Findings below are ordered by confidence and payoff.

## F1. MCP project files use SESport.AI namespaces, and MCP tests live in
the AI test project

- Files: all 24 files in `src/SESport.MCP/WebPages/` and all 5 files in
  `src/SESport.MCP/WebSearch/` declare `namespace SESport.AI.WebPages` or
  `SESport.AI.WebSearch` (e.g.
  `src/SESport.MCP/WebPages/WebPageContentClient.cs:3`).
  Tests in `tests/SESport.AI.Tests/AI/` exercise these MCP types:
  `CachedWebSearchClientTests.cs`, `SearxngWebSearchClientTests.cs`,
  `WebPageContentCacheTests.cs`, `WebPageContentClientLiveTests.cs`,
  `WebPageContentClientTests.cs`, `WebPagePipelineTests.cs`,
  `WebSearchCacheTests.cs`, and
  `WebSearchRateLimitOptionsTests.cs` (this one tests the Core type
  `WebSearchRateLimitOptions`).
- Why: the namespaces are a leftover from when this code lived in the AI
  project. The mismatch is what allowed the tests to end up in
  `SESport.AI.Tests`; the dedicated `SESport.MCP.Tests` project currently
  holds only 2 of the ~10 MCP test files. Readers of `SESport.AI` see
  namespaces that describe code in another project.
- Action: rename the namespaces to `SESport.MCP.WebPages` /
  `SESport.MCP.WebSearch`, update `using` directives, move the eight test
  files into `tests/SESport.MCP.Tests`, and align their namespaces with
  that project.
- Risk: broad but mechanical; the rename touches many usings and test
  namespaces. Verify with `dotnet build` and `dotnet test` afterwards.

## F2. Legacy Llama/OpenRouter AI clients are obsolete, unregistered, and
tested

- Files: `src/SESport.AI/Clients/Legacy/` (whole tree:
  `LlamaServerClient.cs` ~2070 lines, `OpenRouterClient.cs`, 14 files under
  `Llama/`, plus `Schemas/conditional-tools.json` and
  `Schemas/tools-web.json`), and tests
  `LlamaRequestFactoryTests.cs`, `LlamaReportSubmissionTests.cs`,
  `LlamaServerClientTemperatureTests.cs`, plus large Llama portions of
  `AiProviderClientTests.cs` (236 Llama references) and
  `AiProviderApiKeySourceTests.cs`.
- Why: both clients are marked `[Obsolete]`, and
  `AiServiceCollectionExtensions.cs` registers only `CodexCliClient`,
  `OpenCodeCliClient`, and `GoogleTranslateClient`. Nothing outside the
  clients' own files, the tests, and the AI project README references
  them, and the two JSON schemas under `Clients/Legacy/Schemas/` are not
  referenced by any code or csproj item.
- Action: confirm the "archived configurations" rationale is still valid;
  if not, delete the `Clients/Legacy` tree, the schema files, and the
  corresponding tests.
- Uncertainty: the AI README explicitly documents these as retained
  compatibility adapters for archived configurations. If archived database
  configurations are meant to keep working (or at least keep being
  explainable), this is intentional and should stay. Owner decision.

## F3. Empty leftover directories under src/SESport.MCP/Legacy

- Files: `src/SESport.MCP/Legacy/Clients`, `src/SESport.MCP/Legacy/Llama`,
  `src/SESport.MCP/Legacy/Schemas` (all empty, zero tracked files).
- Why: residue from a previous removal; empty directories are not tracked
  by Git, so they only exist in this working tree and mislead anyone
  reading the folder layout.
- Action: `rmdir -p` the empty tree.
- Risk: none.

## F4. Nullable reader helpers duplicated between PostgresHelpers and
ActivityQueryRepository

- Files: `src/SESport.Data/PostgresHelpers.cs` (`ReadNullableString`) vs
  `src/SESport.Data/Activities/ActivityQueryRepository.cs:1556`
  (`ReadString`, identical body), plus local `ReadTimeOnly` (line 1634),
  `ReadDateTimeOffset` (line 1644), and `ReadGuidArray` (line 1654).
- Why: `ActivityQueryRepository.ReadString` is a byte-for-byte duplicate of
  `PostgresHelpers.ReadNullableString` and has 39 call sites, while 65 call
  sites use the shared helper. `ReadTimeOnly` and friends are `internal`
  and reused cross-file through
  `ActivityQueryRepository.ReadTimeOnly(...)`
  (e.g. `ActivityGroupQueryRepository.cs:267`), which makes the repository
  class a de facto home for shared Data helpers.
- Action: delete the local `ReadString` (switch call sites to
  `PostgresHelpers.ReadNullableString`) and move `ReadTimeOnly`,
  `ReadDateTimeOffset`, and `ReadGuidArray` into `PostgresHelpers`.
- Risk: low; pure refactor, verified by the Data test project.

## F5. One-line pass-through wrapper in WebPageContentFetchSupport

- File: `src/SESport.MCP/WebPages/WebPageContentFetchSupport.cs:241`
  (`NormalizeGluedTableCellText` just forwards to
  `WebPageTextNormalization.NormalizeGluedTableCellText`).
- Why: the only caller is `NormalizeText` in the same file (line 226);
  every other call site in the repo already calls the Core method
  directly, so the wrapper is a needless extra step.
- Action: remove the wrapper and call the Core method at line 226.
- Risk: none.

## F6. Facade sub-repositories are public though only AdminRepository uses
them

- Files: `src/SESport.Data/Admin/AdminReferenceRepository.cs`,
  `src/SESport.Data/Entities/EntityRepository.cs`,
  `src/SESport.Data/Entities/EntityMergeRepository.cs`.
- Why: `AdminRepository` is a pass-through facade and the only consumer of
  these three classes (35 external files use `AdminRepository`; zero use
  the sub-repositories directly). Leaving them `public` invites bypassing
  the facade. (`BroadcastChannelLinkRepository` is used directly by Web,
  so it stays public.)
- Action: make the three classes `internal` and update any test references
  (none found outside Data).
- Risk: low; verify the test projects do not reference them directly.

## F7. Large classes with mixed responsibilities (split candidates)

- Files:
  - `src/SESport.MCP/WebPages/WebPageContentFetchSupport.cs` (2140 lines,
    static grab-bag of normalization, cutoff, PDF, user-agent, and
    boilerplate helpers; after F1/F5 it becomes the largest non-legacy
    file)
  - `src/SESport.AI/Clients/Legacy/LlamaServerClient.cs` (2068 lines;
    legacy - see F2)
  - `src/SESport.Data/Activities/ActivityQueryRepository.cs` (1716 lines;
    mixes list queries, edit reads, merge-candidate search, and lookup
    options)
  - `src/SESport.MCP/WebPages/WebPageFetchOrchestrator.cs` (1569 lines)
  - `src/SESport.Web/Pages/Admin/Runs/AiRunToolTracePresenter.cs`
    (1111 lines) and `Details.cshtml.cs` (865 lines)
- Why: readability/maintainability; each file intermixes several distinct
  concerns that are independently testable.
- Action: only if a concrete change is planned for one of them - e.g.
  extract the edit/merge reads out of `ActivityQueryRepository`, or group
  `WebPageContentFetchSupport` helpers by concern. No forced split.
- Risk: pure moves; low, but no functional payoff on its own.

## F8. Infrastructure files at the SESport.Data project root

- Files: `src/SESport.Data/PostgreSqlJson.cs`,
  `src/SESport.Data/PostgresDataSourceFactory.cs`,
  `src/SESport.Data/PostgresHelpers.cs`,
  `src/SESport.Data/EntityLinkEntityNotFoundException.cs`.
- Why: everything else in the project is organized into domain folders
  (`Activities/`, `Admin/`, ...); these four live loose at the root.
- Action: group them into a folder (e.g. `Infrastructure/` plus
  `Entities/` for the exception) when touching them anyway.
- Risk: none (namespace-only churn); optional.

## Verified clean

- No unused usings (IDE0005 severity=error passes), no compiler warnings.
- Method-level sweep of `src/`: no unreferenced public or private methods.
- All `_*.cshtml` partials under `SESport.Web` are referenced.
- No TODO/FIXME/HACK markers.
- Single-implementation interfaces (`IAiJobRunner`, `IAiPromptRenderer`,
  `IEntityImageReplacementService`, etc.) are faked in tests, so they are
  deliberate seams rather than dead indirection.
