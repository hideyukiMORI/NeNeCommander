# Preview beta.3 CodeQL corrections — 2026-11-22

Issue #553 is a release correction within #522 / PR529. The November 22 resumed
milestone instruction keeps release first, then the recorded next-version Issues.
GitHub's all-state milestone query returned no records. Historical fixed model
assignments do not apply: investigation, implementation and verification are by
the current agent; design consultation is requested only when design is needed.

## Discovery and retained failures

The existing candidate is chore/522-preview-beta3 at 8dc51e5, with only four
Markdown files changed from deep-reviewed 9ca919d. Main is 08c2d9c; both were
clean. PR402 is merged and hide's screen acceptance is complete. The current
release remains unpublished. The existing input-isolation rule is retained.

Second deep review 44936588484 / job136944433359 completed successfully at
19:33:50 UTC on November 21. All19 script checks passed; CodeQL is separately
reported as an external step. The artifact14721856768 is1,511,214bytes and its
SHA256 is6bcdefd64ac2f977c4c2432c6f749bfedaaf976c9c70c162a6a8bc8b4acd6e67.
The complete archive, extracted reports, job log and API results are retained in
D:/NeNeCommander/evidence/release-beta3-20261122/second-deep-review.

CodeQL analysis2221789209 has3 results, although the default-branch open-alert
query returned0. The explicit candidate-branch query correctly returned open
alerts154,155,156. Workflow success is not zero-alert acceptance. The source
SARIF and both API responses are retained without dismissing any alert.

Read-only preparation encountered Git dubious ownership and sandbox denial of
the user's GitHub CLI configuration. Per-command safe.directory admitted only
the known checkout; the existing CLI authentication was used through reviewed
escalation. Initial connected issue-search arguments were invalid and were
corrected after reading the tool schema. Connected Issue creation returned403
Resource not accessible by integration; the existing gh CLI created#553.
One Windows rg path wildcard was invalid and was corrected with a glob filter;
one guessed ADR filename was absent and the actual indexed file was then read.
A sandbox Python command was unavailable; the configured user Python is reached
through the same reviewed execution boundary. None of these attempts changed
product behavior or security settings. No automatic approval rejection occurred.

The first App build failed with CA1861 after a constant array was passed directly
to Select. The failure remains in codeql-corrections/app-build.log. Its replacement
extracts the existing field-registration body into RegisterField; no suppression,
new shared mutable array, dependency or contract exception was introduced.

## Change and invariant

- KeyboardRepeatGuard.Observe uses a typed outcome switch for the existing
  palette execution, nonrepeating Bookmark action and native draft Enter cases.
  The same key enters the same existing set; release on a fresh initial event,
  native IME/popup handling and mapper ownership are unchanged.
- BookmarkInputOwnership calls RegisterField once for each of the same four
  names in the same order. Lookup, field ownership, composition subscriptions
  and each captured-field LostFocus callback retain their order and meaning.
- PaneSelectionSessionTests projects the same initial/cleared selections before
  executing the same copy/enumeration/containment assertions, in the same order.

The only execution paths remain KeyboardIntentMapper's existing repeat guard,
BookmarkInputOwnership's native control registration and PaneReducer's existing
selection contract. This changes no file-operation trust boundary, XAML, resource,
key mapping, setting, dependency, gate, threshold or waiver. #523 and#550 remain
separate next-version behavior fixes.

## Focused verification

From D:/NeNeCommander/worktrees/issue-522, exact command arrays and complete logs
are in codeql-corrections/commands.txt and run-focused*.ps1. Every selected restore
uses --locked-mode -p:Configuration=Release. Every build uses -c Release
--no-restore -m:2 -p:UseSharedCompilation=false.

```powershell
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj --locked-mode -p:Configuration=Release
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore -m:2 -p:UseSharedCompilation=false
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --locked-mode -p:Configuration=Release
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore -m:2 -p:UseSharedCompilation=false
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~BookmarkKeyboard|FullyQualifiedName~BookmarkDraftKeyboard|FullyQualifiedName~KeyboardPalette|FullyQualifiedName~KeyboardModal|FullyQualifiedName~KeyboardIntentMapper|FullyQualifiedName~KeyboardChord'
dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --locked-mode -p:Configuration=Release
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore -m:2 -p:UseSharedCompilation=false
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter FullyQualifiedName~PaneSelectionSessionTests
dotnet format whitespace NeNeCommander.slnx --no-restore --include src/NeNeCommander.Presentation.WinUI/Input/KeyboardRepeatGuard.cs src/NeNeCommander.App/Views/BookmarkInputOwnership.cs tests/NeNeCommander.Application.Tests/PaneSelectionSessionTests.cs --verify-no-changes
```

Final App/Presentation.Tests/Application.Tests builds have0 warnings/errors;
42 keyboard cases and3 selection cases pass,0 failed/skipped. Format exits0
with its workspace-load warning retained. Successful App restore was reused
after the CA1861 source correction. No new native/desktop observation is claimed.

## Release proof still pending at this checkpoint

ADR-0061 reuses the successful baseline's unchanged Domain/Infrastructure source,
tests, tooling, dependency and threat evidence. Refresh the affected Application
and Presentation coverage/mutation evidence and use the same official CodeQL
2.27.5 security-and-quality suite, build-mode none and canonical configuration
to analyze the complete final source. Retain tool digest, commands, complete
SARIF, source-input equality and final branch alert readback. This is supplemental
evidence, not a claim that a new all-layer deep workflow ran. The accepted
baseline must still be inside96 hours when the release is published.

PR acceptance, accepted-source packaging, payload/ZIP/PRI/notice verification,
public tag/assets readback and exact task cleanup follow their existing entry
points. See [release record](2026-11-22-preview-beta3.md) and
[handoff](../handoffs/2026-11-22-preview-beta3-handoff.md).

The existing complexity command `pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect`
completed with exit1 and5 findings (method-lines1, nesting1, parameters2,
type-lines1), not a clean-complexity claim. Its full result and log are retained.
No global gate wiring or baseline was changed. The final verification input
manifest contains1,075 non-Markdown tracked files.

## Supplemental proof progress

The corrected input manifest matches all1,075 recorded non-Markdown inputs.
Compared with9ca919d, exactly the three C# files described above differ outside
Markdown. Domain and Infrastructure.Windows source/tests, dependency locks,
SDK/tool pins, architecture/conformance rules and the adversarial registry are
unchanged. `codeql-corrections/proof-input-reuse.json` records this comparison.

The existing coverage-settings command, repeated only for the two affected test
projects, passes Application843/843 with branch-rate1.0 and Presentation352/352
with branch-rate0.9471228615863142 (minimum0.9); neither skips any case. Commands,
full Cobertura reports and summaries are retained in `codeql-corrections` through
`run-coverage.ps1`. The preserved baseline owns all unchanged layer proof.

CodeQL2.27.5 was fetched from the official GitHub release and its699,874,229-byte
bundle matched SHA2566d0befdb4a1d60906b05610e16ac9d00b3d083ab885bb005d934a3a43fae69b4.
The first version command was issued before archive extraction completed, so its
Java launch failed before any database or analysis began. That failure remains
in `codeql-version-before-extraction.log`. After the existing extraction process
completed, the same binary returned its expected version and the full analysis
started. No failed result was discarded or represented as a successful scan.

The source review confirms mutually exclusive typed outcome cases preserve the
repeat guard's three single-shot conditions; RegisterField preserves registration
order and each captured control; projecting the two immutable selections preserves
test assertion order. No native focus or UI performance proof is inferred.
Final mutation, CodeQL and publication results are still pending here.

### Verification isolation correction

The initial CodeQL database creation overlapped the Application mutation run.
Although all992 tracked C# sources were discovered, the buildless extractor also
searched built DLLs, and Stryker temporarily instruments test-output assemblies.
The incomplete scan therefore is not used as acceptance proof. At14:11 JST the
six observed owned CodeQL processes were stopped, with identities and reason in
`codeql-overlap-stop.json`; no product App or other project process was stopped.
`codeql-create-overlap-aborted.log` and the incomplete database remain retained.
The extraction's best-effort restore warnings also occur in the prior successful
CI, but that fact alone does not establish current analysis completeness.

Run the same full CodeQL source analysis only after both mutation processes have
completed and restored their test outputs. Preserve the successful baseline and
current mutation; do not repeat them merely because the CodeQL execution order
was corrected. The incomplete parallel analysis is neither success nor a source
security finding, and is not uploaded to GitHub.

### Application mutation result

The Application mutation run completed at14:15:06 JST with exit0 and96.41%
against the unchanged95% threshold. The2,919 mutants comprise1,901 Killed,
33 Timeout,70 Survived,2 NoCoverage,414 CompileError and499 Ignored by the existing
block-covered optimization. Repeated runner sessions remain visible in the full
log. They are not hidden or described as individual assertion kills.

The comparison first encountered different CI/local absolute source roots; the
corrected `compare-application-mutation.ps1` normalizes the source-relative paths
and confirms all2,919 identities by path, ID, location and replacement. It retains
all10 status changes. Nine change only Killed/Timeout classification. The one
other change is unchanged SettingsSession.cs line321 return-removal mutant2681:
Timeout in CI, Survived locally. This return follows setting the stopped state;
its surrounding loop also exits when that state is true. Neither this source nor
its tests changed in#553, and the selection-test projection does not cover that
settings lifecycle. This is not evidence of a new product failure or a proven
regression from the refactor. The current96.41% is reported without substituting
the prior96.46%, and no threshold or exception is changed.

A resource snapshot during diagnosis found39,136MiB available of65,400MiB and96%
overall CPU utilization. It establishes no causal explanation of the retries.
Presentation mutation and the sequential CodeQL analysis remain pending.

The connected GitHub PR-update API also returned403 Resource not accessible by
integration; the existing authenticated gh CLI updated the Draft PR description
successfully. No automatic approval rejection occurred.

### Presentation mutation and restored inputs

Presentation mutation completed at14:23:39 JST with exit0 and94.10% against the
unchanged90% threshold. The2,057 mutants comprise1,485 Killed,30 Timeout,
79 Survived,16 NoCoverage,172 CompileError and275 Ignored. The existing Stryker
safe-mode handling of an unidentified WindowAdjustmentKeyHintPresenter mutant
compile error is retained in the log; it is not a successful behavior assertion.

`compare-presentation-mutation.ps1` matches all2,037 mutants in unchanged source
by relative path, location, mutator and replacement. Its only3 status changes are
Killed/Timeout. The changed KeyboardRepeatGuard contains20 mutants; its3 surviving
replacement/mutator pairs match those in the accepted baseline exactly, without
using shifted ordinal IDs as identity. The current94.10% is reported rather than
the prior94.11% (the refactor changes the mutation population).

Both mutation summaries were required before the next CodeQL invocation. One
preflight path assumed Presentation used net10.0; it stopped before invoking
CodeQL. The actual project declares net10.0-windows10.0.26100.0. The incorrect
script and correction record are retained, and the corrected preflight proves
that both test-output DLLs match their ordinary production-build DLLs and all
1,075 frozen source inputs still match. `post-mutation-inputs.json` contains the
hashes. Only then did the fresh `codeql-database-isolated` scan begin.

Read-only next-version preparation reviewed Issues523/524/550. The current
activation event is already wired to OnActivated and the guarded file-list focus
queue, so its presence alone does not reproduce or explain523. PaneReducer owns
524's existing clamp and550's refresh focus recovery; no second command path is
planned. The proposed524 proof covers empty/singleton/visible-only lists, refresh,
independent panes and unchanged paging/selection. No next-version source edit or
new native input was performed during the release proof.

## Final local proof and publication hold — 15:06 JST

The isolated CodeQL extraction and all171 queries completed. The original SARIF
reports executionSuccessful=true and0 findings. Exact comparison confirms the
same164 finding rules, all3 query-pack versions, category/language:csharp and
SHA256 equality of all992 tracked C# sources in the database source archive.
The SARIF is3,548,275bytes, SHA256
`d255f59d3bc8ab1885deae032c446da04ac23551f5857a5a57b00ae23b9e9e64`.
No result, diagnostic, threshold or path exclusion was removed.

This is not error-free extraction. The internal extractor records116 errors in
unchanged RecordClosureProbe(27), CommanderApplication.xaml.cs(2) and
CommanderWindow.xaml.cs(87). Raw SARIF additionally retains52 compilation-error
notifications and10 file-level extraction-error diagnostics, covering unchanged
source-generated P/Invoke, XAML and combined-tool compilation limitations.
`codeql-extractor.log`, `codeql-extraction-limits.json` and the complete original
SARIF preserve them. The prior CI's internal errors are not claimed identical:
its1811 discovered source files/8 unresolved types differ from the current992/7.

The official DatabaseQualityDiagnostics query reports no low-quality diagnostic.
Call-target resolution is99.95238307813639% and expression-type resolution is
99.97864038959929%, against that query's existing85% thresholds. All992 files
receive the successful-extraction diagnostic. Each of the3 changed files has
that diagnostic and no error/warning notification. A separate read-only query
against the completed database, `semantic-inspection/ChangedSemantics.ql`, checks
all18 callable bodies in those files:718 expressions,0 unknown types and101
method calls with101 resolved targets. It includes Observe, RegisterField,
its LostFocus callback and the selection projection lambda. It neither changes
nor replaces the canonical rule suite. Commands, QL source, BQRS and decoded
results are retained in `semantic-inspection`; no database extraction was repeated.

Seven existing ADV-013 keyboard cases pass three consecutive runs,0 failed or
skipped each. `run-adversarial-keyboard.ps1` records this command three times:

```powershell
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'TestCategory=Adversarial&FullyQualifiedName~Keyboard'
```

`post-codeql-inputs.json` and `post-adversarial-inputs.json` confirm all1,075 frozen
non-Markdown inputs still match. Independent read-only review finds no required
source correction or remaining local proof blocker. It accepts ADR-0061 scoped
reuse while retaining the extraction limitations and remote/publication conditions.
No reviewer build, test, mutation or analysis was run.

### Recording-tool failures retained

The initial completion summary failed under PowerShell StrictMode when array
member enumeration read an empty SARIF results collection. The same expression
reproduced the failure; explicit single-run indexing fixed only the readback.
The rule comparison then encountered extensions without a rules property and
the GitHub API's normalized placement of rules under extensions versus the raw
CLI's driver. The final comparison collects both standard locations and matches
all164 IDs. Original scripts and correction records remain; query execution was
not repeated. One optional log read used a nonexistent filename; one live query
log read was locked. The first semantic query command used an invalid native
argument expression and stopped before query execution; the recorded argument
array corrected it. A prepared test-log parser was corrected for Japanese output
before the tests ran. None of these failures altered product inputs or thresholds.

### Automatic approval blocks the required upload

At15:00–15:04, automatic approval review rejected the original SARIF upload to
this repository's code-scanning endpoint twice, before process execution. The
second request supplied concrete evidence: connected repository1354826314 is
public, owned by hideyukiMORI, matches origin and grants the current connection
admin/push permission; SECURITY_MODEL SEC-008 explicitly requires CodeQL result
upload and ADR-0031 requires candidate/default-branch analysis readback.
The review still required hide's explicit approval for this exact public payload
because diagnostic messages contain local paths. No token was read and no upload
was attempted by either rejected process. No alternate route was used.

The payload contains992 relative source-file references and no artifact source
contents;1,010 diagnostic messages contain absolute local paths. The full
original payload remains unchanged. hide has been asked to authorize this
3.55MB result for `hideyukiMORI/NeNeCommander`, for the candidate and subsequent
accepted main. An unanswered question is not approval. `upload-approval-block.json`
records the rejection and supporting evidence. This is an upload hold, not a
cleanup rejection or a product-test failure.

PR529 remains Draft; remote alerts154–156 have not been refreshed. Required
remaining steps are authorized upload, successful processing and zero candidate
alerts, final documentation/PR acceptance and squash merge, equivalent accepted
main analysis/readback, clean-source packaging, all payload/ZIP/PRI/notice checks,
public tag/assets readback and exact task cleanup. Recheck96-hour freshness at
publication. No release, packaging or cleanup completion is claimed.