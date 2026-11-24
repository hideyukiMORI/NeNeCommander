# Opposite-pane current-location navigation — local proof

Issue: #547. Date: 2026-11-22. Base: `7ef9b9654bf26a74d2c673c6ff1d1d79195e65b2`.
Status: technical core/keyboard and header implementation accepted locally; native acceptance incomplete.
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

## Historical remaining work and approval holds — before 21:34 JST

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


## Header integration update — 22:10 JST

hide approved the exact Claude brief at21:34 JST. A one-turn, tool-free CLI
consultation succeeded and the original JSON/advice/invocation are retained. It
is not a /design-generated asset. #542 publication was separately approved and
completed as PR557/main b82aa1e. These facts supersede the historical holds above.

Merge907d0c0 imports that accepted main. PROJECT_STATE and both resource catalogs
had additive conflicts; both sides were retained and catalog uniqueness checked.
The merge's normal commit hook passed112 rules and24 security cases. Existing
count, keyboard, navigation and lifetime proof is reused for unchanged inputs.

The [header handoff](../design/2026-11-22-other-pane-header-handoff.md) adopts the
24-DIP target,16-DIP folder/arrow,4-DIP adjacent gaps, reserved passive slot,
non-focus click and complete directional names. Same-location remains enabled
and idempotent under ADR-0084. PaneNavigationAction projects the existing
DualPaneSnapshot readiness and canonical binding label; it owns no admission,
path, read or state. CommanderPaneView renders it; one click forwarder emits the
existing intent through AsyncWorkOwner and CommanderSession. Default Button states
remain; no custom template, opacity or extra focus-restoration path is added.

| Evidence | Result |
|---|---|
| header-main-merge.log / header-merge-resolution.json / header-merge-commit.log | Additive conflicts resolved without replacing either feature; normal hooks pass. |
| header-presentation-build.log / header-app-build.log | Affected Release builds succeed,0 warnings/errors. |
| header-presentation-tests.log | 7/8 pass; the new equal-location case mistakenly requires reference identity of the aggregate DualPaneSnapshot, whose existing Current getter constructs a new aggregate. |
| header-presentation-corrected-build.log / header-presentation-corrected-test.log | Test now requires unchanged left/right/operation/activation references, one read per side and unchanged selection counts. Only this changed case reruns and passes1/1; production is unchanged. Eight distinct final cases pass. |
| header-format-* / header-format-verify-* | Three affected projects' changed C# format and verify successfully. App workspace loading warnings are retained; exit0. |
| header-complexity.json / header-complexity-comparison.json | Inspect exits1 with exactly the original five findings, including identical paths/lines/metrics/values/limits. No new finding. |
| header-resource-layout-check.json | Both catalogs have unique keys, both complete direction formats reference one canonical-key placeholder, merged count keys remain. Both buttons have the same forwarder, reserved slots and conservative initial state; style is nonfocusable and retains the default template. |
| header-glyph-offscreen.png / render-header-glyph.ps1 | Authored geometry rendered without a window at16/32/48/64 pixels in both directions on black/white. Visual inspection finds folder/arrow shapes distinct; this is geometry evidence only, not WinUI/native/high-contrast proof. |
| header-independent-review.json | Read-only independent review at22:07:45 finds no required correction in the added header; native limits remain explicit. |

The first path lookup used the wrong source directory; an rg wildcard path and an
unelevated worktree git read also failed without mutations. Resolved paths and
repository-scoped/elevated git were then used. These setup failures are not product
verification. The first new test failure is retained rather than omitted.

Exact additional executable commands (worktree root):

```powershell
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore -maxcpucount:4
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~PaneNavigationActionTests'
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~CreateAsyncWhenLocationsMatchKeepsEnabledNoOpAndSelectionCounts'
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore -maxcpucount:4
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
```

Changed-file whitespace checks use the existing command form above for
PaneNavigationAction.cs, PaneNavigationActionTests.cs, CommanderPaneView.cs and
CommanderWindow.xaml.cs. Subsequent XAML editing is indentation only; successful
compilation remains applicable. No full suite, coverage, mutation, CI, App launch
or shared-desktop input was selected. No new security/provider boundary exists.

The header-local final input manifest supplements the earlier core manifest and
records exactly which inputs changed through the count merge/header addition.
Native key/click/focus, UIA, narrow/high-contrast/DPI and actual screen acceptance
remain pending. Source PR/integration and task cleanup remain incomplete. The
usual App/settings, beta.3 and unrelated retained worktrees remain untouched.


## Isolated actual-control continuation — 2026-11-24

The unchanged accepted source65a6fb2 is still the head of open Draft PR558.
The GitHub readback reports no merge and a conflicting base; no remote write,
main integration or beta.4 inclusion occurs. All1092 earlier header proof inputs
match exactly (`reused-inputs-01.json`), so successful managed/core proof is reused.
ADR-0061 selects the previously missing actual-control boundary only.

The dedicated copy at `D:/NeNeCommander/outputs/open-other-pane-20261124/control-fixture-01/`
contains46 original App inputs, four unchanged production dependency DLLs and
private schema4 settings/owned folders. Diagnostic substitutions are limited to
composition paths/settings/lifecycle, project references and observation of the
original host intent/admission. Product focus calls, XAML, button handler,
mapper/session/reducer and resource resolution remain unchanged. The launcher
requires its named non-input desktop, never switches desktops or injects shared
input, and owns only its exact child process handle.

| Trial | Result |
|---|---|
| Diagnostic build01 | C# path escaping fails; original copy/log retained. |
| Build02 / launch1 | Build0warnings/errors; startup exits0xC000027B before a managed receipt. |
| Build03 / launch2 | Build0warnings/errors; added startup trace reaches the desktop guard and stops at the diagnostic UWP language override. |
| Build04 / launch3 | A newline-dependent attempted removal did not remove that override; the repeated failure and source are retained, not accepted. |
| Build05 / launch4 | Exact-line removal verified first. Product resource resolution is retained. Forty-one assertions pass, then stage12 times out while expecting control-E navigation. |
| Build06 / launch5 | The diagnostic preparation waits for its exact existing navigation Task, and records the original AsyncWorkOwner admission result. All49 checks/15 stages pass, process exit0, observed125percent scale. No product correction is made. |

The final probe proves actual ButtonAutomationPeer invocation in both directions,
exact source snapshot/selection/focus-control preservation, same-location exact
no-op, active-only visibility, disabled action during name entry and refusal of a
stale direct intent while that modal owns input. Both Japanese UIA names and
tooltips contain the canonical Ctrl+E label. The24-DIP target fits its header at
the initial window size and after a680-DIP outer-width resize. The keyboard segment
calls the existing character-data translator and mapper: initial U+0005 dispatches
once, repeated input is consumed, and fresh e with Control dispatches once again.
This is not physical Control-key, mouse-hit, IME, screen-reader or visible proof.
The total3126.1519ms is fixture duration, not operation latency.

The final normal settings file remains623bytes, with unchanged timestamp and
SHA-256561932A6530A6317499BAB59AD0735C8469869A15C6E6DA9BE3507B37AF557CE.
The input desktop is Default before/after. Six owned folder files retain the exact
expected paths/content. The successful child exits normally; prior failed children
also have terminal receipts. No ordinary App or real Explorer is invoked.

Raw evidence is under `D:/NeNeCommander/evidence/open-other-pane-20261124/`.
`final-parity-01.json` reconciles46App inputs/three declared substitutions/fourDLLs.
Fourteen of16XBF/PRI files match the earlier normal App byte-for-byte. The two
mismatches are fully accounted for in `resource-parity-reconciled-01.json`: the
32714-byte CommanderWindow XBF differs only in its64-character source-checksum
field, which matches the exact current XAML; every other byte is identical. SDK
MakePri detailed dumps are12032483bytes each and differ only in that embedded XBF.
All localized candidates and the compiled XAML body therefore match. Raw files are
retained; this is an explicit reconciliation, not a fabricated all-byte-equal claim.

Exact commands, from the diagnostic root unless an absolute path is shown:

```powershell
dotnet restore App/NeNeCommander.App.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build App/NeNeCommander.App.csproj -c Release --no-restore -maxcpucount:2
pwsh -NoProfile -File D:/NeNeCommander/evidence/open-other-pane-20261124/run-controls-02.ps1 -Locale system -Attempt 5 -Scenario standard
& 'C:/Program Files (x86)/Windows Kits/10/bin/10.0.26100.0/x64/makepri.exe' dump /if <product-or-fixture-NeNeCommander.App.pri> /of <corresponding-evidence-xml> /dt detailed
```

Full command/argument paths and results are in the scripts, build logs and launch
receipts. Diagnostic-only analyzer disabling does not change production build rules.
This continuation does not add full suites, coverage, mutation or CI. The subsequent
repository edit is documentation only and selects text/link/diff review plus the
normal lightweight commit hook, not another product build/test.

An initial attempt to create the journal was automatically rejected because its
Set-Content could overwrite prior evidence. Read-only absence checks and an append-
only journal write resolved that operation; no evidence was deleted or bypassed.
Physical key/mouse/IME, English native labels, other DPI/high contrast, screen-reader
speech and hide's visible acceptance remain separate. PR558 stays Draft; publication
of these later records, base integration and task cleanup are unfinished. Retain the
worktree/output/evidence; there is no cleanup rejection or cleanup task due here.
