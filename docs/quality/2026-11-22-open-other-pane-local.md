# Opposite-pane current-location navigation — local proof

Issue: #547. Date: 2026-11-22. Base: `7ef9b9654bf26a74d2c673c6ff1d1d79195e65b2`.
Status: technical core/keyboard accepted locally; icon and native acceptance incomplete.
[ADR-0084](../adr/0084-open-current-location-in-other-pane.md) owns the refinement.

## Scope and invariant

Only `DualPaneSession` captures the active listed location and opposite side. Its
existing `NavigateAsync`/`PaneSession`/`PaneReducer` remain the sole read, history,
focus, selection, sorting and visibility path. The source snapshot and active side
stay unchanged. Same provider-aware identity is an exact no-op: Windows/UNC casing
is insensitive, WSL distribution aliases/case are normalized, WSL component case
remains distinct. No file mutation, Shell handoff, settings schema, dependency,
provider policy or new I/O mechanism is introduced.

The derived snapshot readiness requires a listed source and neither side reading
or launching, with no running/name/confirmation/conflict operation. Retained source
content after failure/cancellation/abandonment means its displayed successful location.
An absent/failed destination is allowed. Success applies the destination's existing
focus/selection/history/preferences; failed or cancelled reads preserve its content.

A closed Idle/Pending capture names only destination side and exact `PaneLoading`.
It owns no token/read. Existing operation/modal precedence and an active read take
Escape first. Otherwise only that identical passive read may receive the existing
`PaneRead.Abandon`. Stale/unrelated/newer reads are not cancelled. Conditional finally
clears only its own capture. Actual `AsyncWorkOwner` still refuses Tab/ordinary
commands during work and admits Escape to release the waiter before its provider.

Ctrl+E uses existing E translation, canonical bindings, repeat guard, labels, F1,
footer and palette. Raw E passes through; produced e/U+0005 with Control dispatches
once in normal contexts. Plain e, Bookmark editing and text/IME routes remain.
The palette row and selected-command header name the actual opposite destination.
Its Application selection is qualified by captured pane/scope identity and rechecked.

ADR-0061 selects affected builds, focused new cases and directly affected consumers.
The shared router, key table/repeat guard and palette projections require their
consumer tests; unchanged providers/Domain do not justify all-product coverage,
mutation or deep CI. No full gate/deep review/release proof is claimed or started.

## Trials and results

Raw local evidence is retained at `D:/NeNeCommander/evidence/open-other-pane-20261122/`.
Builds use the pinned SDK, Release, locked restore and warnings as errors.
Successful evidence is reused when its relevant implementation did not change.
Counts below overlap; they are not a summed unique-test total.

| Evidence log | Result and meaning |
|---|---|
| `baseline-build.log` / `baseline-build-corrected.log` | Unused using was removed; corrected build 0 warnings/errors. |
| `baseline-tests.log` | Only the intent declaration existed: 9 failures/3 passes establish old no-op behavior. |
| `implementation-build.log` / `implementation-build-corrected.log` | IDE0046 required the route's switch-expression form; corrected build 0 warnings/errors. |
| `implemented-core-tests.log` | 12/12: both directions, provider-aware identity, own destination history/order/hidden/focus, failure/cancellation. |
| `lifetime-build.log` / `lifetime-tests.log` | 0-warning/error build; 7/7: immediate Escape release, source selection, unrelated/newer passive read exclusion, active-read priority, changed activation, fault cleanup and caller token. |
| `host-build.log` / `host-build-corrected.log` / `host-tests.log` | A void Assert API was corrected; 2/2 actual work-owner/session cases, including A abandon → B capture → A finally → Escape abandons B. |
| `admission-build.log` / `admission-build-corrected.log` | Test setup wrongly offered LocationsLoading to an open-only cancel API; completed fake catalogs replace that cleanup. Unused using removed. |
| `admission-tests.log` | All 9 admission cases pass; 7 scope cases initially fail because the test assumed reference identity for newly projected SettingsSnapshot. Product behavior is unchanged by the test correction. |
| `operation-scope-build.log` / `operation-scope-tests.log` | Content equality fixes that test assumption; 13/13 scope and running/name/confirmation/conflict/completed/rejected operation cases pass. |
| `keyboard-baseline-build.log` / corrected log | CA1861 in a one-item assertion is corrected without an allocation; build 0 warnings/errors. |
| `keyboard-baseline-tests.log` | Before mapping/labels: 6 failures/2 passes establish missing Ctrl+E, repeat and help/footer behavior. |
| `keyboard-implemented-build.log` / `keyboard-consumer-tests-initial.log` | Build 0 warnings/errors; 41/44 pass. Three old declaration/catalog counts require explicit new command expectations. |
| `palette-application-build.log` / `palette-application-tests.log` | 14/14 catalog/dispatch cases, including 4 new opposite/same-location/stale-capture cases. |
| `presentation-consumer-build.log` / `presentation-consumer-tests.log` | 160/161 pass. Remaining NavigationSurface count is corrected from 27 to 28 (FileList is 47). The unchanged successes remain evidence. |
| `palette-header-baseline-build.log` / test log | Independent review finds the header still names the first row. Both newly added left/right selection/query cases fail before correction. |
| `palette-header-corrected-build.log` / `host-helper-repair.json` | A PowerShell readonly Host-name collision corrupts only the new host test file. The exact authored test is restored; no product file corruption. |
| `palette-header-final-build.log` / `palette-header-final-tests.log` | Build 0 warnings/errors; 33/33 affected palette/view/row, opposite target, host and binding cases pass. Header follows selected row; zero results retain the original default. Host late-result fake now completes inline before state comparison. |
| `application-boundary-build.log` / `application-boundary-tests.log` | Build 0 warnings/errors; 72/72 existing dual-pane operations, abandonment and palette-session boundaries pass. |
| `app-restore.log` / `app-build.log` | Locked Release restore and App build succeed, 0 warnings/errors. App is not launched. |
| `final-format-*` / `verify-format-*` | Changed files in Application, Presentation and their two test projects format and verify successfully. |
| `complexity-final-inspect.log` / `complexity-comparison.json` | Exit1: exactly the same five accepted findings, with path/line/value/limit equality; DualPaneSession is 270 logical lines. No new finding. |

The independent read-only review first corrected the design's host-admission and path-
identity assumptions. Technical ADR acceptance followed. Implementation review then
required the selected palette header fix and a precise late-result proof. Both were
corrected and read back at 18:52 JST with no remaining technical required correction.
This acceptance does not accept the missing icon, native behavior or publication.


The first normal commit attempt is retained as `commit-initial.log`: CS-002 rejects
boolean mode parameters in five new test files. Existing `PaneSide`, `UserIntent`
and `KeyboardContext` values now name the cases through explicit public tests and
private typed helpers. Assertions and product code are unchanged; no magic-string
replacement or rule suppression is used. `typed-case-conformance.log` passes all
112 normative rules. The changed Application cases pass19/19, and the changed
Presentation cases pass12/12 (`typed-case-presentation-tests.log`). Their builds
and final formatting/complexity/input comparisons are retained with the same prefix.
`input-hashes-before-cs002.json` preserves the earlier checkpoint; exactly these five
test inputs differ from it; all production/tooling inputs remain identical. The final manifest supersedes that checkpoint.
## Reproducible commands

All commands run from the dedicated worktree. Each build/test invocation's output
is preserved under the log names above, including unsuccessful attempts.

```powershell
dotnet restore <project.csproj> --locked-mode --disable-parallel -p:Configuration=Release
dotnet build <project.csproj> -c Release --no-restore -maxcpucount:4
dotnet test --project <test-project.csproj> -c Release --no-build --no-restore -- --filter '<filter>'
dotnet format whitespace <project.csproj> --no-restore --verify-no-changes --include <changed-cs-files>
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
```

Projects: `tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj`,
`tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj`,
`src/NeNeCommander.App/NeNeCommander.App.csproj`. Formatting also selects the owning
`src/NeNeCommander.Application` and `src/NeNeCommander.Presentation.WinUI` projects.
Only whitespace and an XML summary were edited after successful executable checks; no behavior changed. The complete argument arrays, filters, outcomes and final source fingerprints are retained
in `verification-summary.json` and `input-hashes.json`. Normal commit hooks run
`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`; their actual result belongs to
`commit.log`, not an inference from product tests.

## Remaining work and approval holds

The directional active-header icon, tooltip/UIA direction and native key/click/focus,
narrow/high-contrast/DPI checks are not implemented/accepted. There is no isolated
native session; no shared-desktop input or user App/settings change is performed.
Managed tests do not establish native dispatch or screen acceptance. No beta.3
replacement, release, main integration, PR readiness or Issue closure is claimed.

Automatic approval rejected the 2,593-byte authored Claude design brief at 18:09 JST;
the exact payload/destination question remains unanswered. `design-brief.txt` and
`design-transmission-hold.json` retain that hold. The earlier #542 public source push
was separately rejected at 17:20 JST and its question remains pending. No source
push or alternative external consultation is attempted to bypass either rejection.
The SARIF approval at 15:24 JST applied to that concrete release payload only.

Retain this unfinished worktree, build output, branch and local evidence. Cleanup is
not yet due: icon/native acceptance, authorized publication, main integration and
permanent completion proof are incomplete. This is not a rejected cleanup action.
#525/#542/#548 local candidates, #523, #453/#491 and AppSDK PR517 remain separate.
