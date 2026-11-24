# Remembered window placement — Issue567 / ADR-0089

Date: 2026-11-23
Status: local implementation and selected verification complete; publication/main integration not yet claimed.

hide requested remembered position/size and off-screen startup correction at17:21 JST.
The exact Issue567 body/destination was subsequently approved and created/read back.
Worktree: D:/NeNeCommander/worktrees/issue-526, branch feat/567-window-placement,
base856cb6f (the separately recorded immediate-language change). Evidence root:
D:/NeNeCommander/evidence/window-placement-20261123. The live issue527 App and its
current settings are separate; all experimental files/processes are task-owned on D.

## Invariant and single mechanism

ADR-0089 extends the existing physical-pixel window adapter/planner and complete-value
SettingsSession/ISettingsStore queue. No new settings file, provider, dependency,
native product import, timer, global window setting or competing operation route exists.
SavedWindowPlacement admits only a validated positive normal rectangle and restored or
maximized state. WindowPlacementTracker retains the latest normal rectangle across
maximization, minimization and unavailable readings. Startup uses one pure planner:
largest attached-area overlap, then rectangle distance, then deterministic geometry
order; shrink/clamp entirely into the chosen work area. Without a saved rectangle,
center the framework initial size on its own display, shrinking if necessary.

The adapter moves the origin to the target display before applying the decided physical
size through its existing Apply route. It captures/detaches before normal-close awaits.
The coordinator queues at most one changed final placement and always drains settings,
including a failed/faulted write. Startup, corrections and ordinary Changed events do
not write. Null/same placement and rejected startup input do not cause an automatic
write. All six display/bookmark/sort update routes retain the complete placement value.

Schema6 requires windowPlacement=null or an exact left/top/width/height/state object.
Coordinates are Int32, dimensions positive, state restored/maximized. Missing, duplicate,
unknown, wrong types, fractions/out-of-range values and unsupported states fail closed.
Exact schemas1-5 gain absence in memory without migration writes. Existing malformed-
input, temporary identity, byte-limit and atomic-publication boundaries remain unchanged.
UserSettings.Create now requires an explicit nullable fourth argument; callers migrate
in the same change. Old binaries reject schema6, while current readers retain schemas1-5.

Stopping RenderSession and the direct RenderPanes progress path refuse late callbacks.
Already queued and direct file-list focus stop before touching controls. This does not
skip queue completion or hide typed persistence failures. No closing render reopens a
modal or rebuilds content after final-save notification.

## Selected verification

ADR-0061 selects planner/tracker, strict settings parser/store, queue/close ownership,
direct value consumers and the actual App host. The pure rules cover negative/disconnected
coordinates, ties, oversized and extreme accepted values, no placement, one-pixel areas,
maximized/minimized ordering and unavailable observations. No filesystem operation,
provider, dependency graph or user-file trust boundary changes. Existing unrelated
provider/operation evidence is reused. Full/deep CI, whole-suite coverage and mutation
are not selected merely for committing or publishing; release proof remains separate.

Commands below ran from the worktree. Test TEMP/TMP point to the task-owned
D:/NeNeCommander/outputs/window-placement-20261123/test-temp. Full stdout, failed
attempts, diagnostic sources and launch/exit records remain in the evidence root.

```powershell
dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -p:Configuration=Release --locked-mode
dotnet restore tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore

dotnet run --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build -- --filter 'FullyQualifiedName~WindowRestore|FullyQualifiedName~WindowPlacement|FullyQualifiedName~SettingsSession|FullyQualifiedName~UserSettings|FullyQualifiedName~InterfaceLanguage|FullyQualifiedName~KeyHintSettings|FullyQualifiedName~ColorSchemeSettings|FullyQualifiedName~ColorSchemePicker|FullyQualifiedName~PaneSortPersistence|FullyQualifiedName~CommanderSessionTests|FullyQualifiedName~CommanderSessionBookmarkRegistration|FullyQualifiedName~CommanderSessionPaletteDispatch'
dotnet run --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build -- --filter 'FullyQualifiedName~Settings|FullyQualifiedName~InterfaceLanguage|FullyQualifiedName~WindowPlacement'
dotnet run --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build -- --filter 'FullyQualifiedName~SettingsPresenterTests|FullyQualifiedName~SettingsSchemeViewStateTests|FullyQualifiedName~BookmarkPresentationBehaviorTests|FullyQualifiedName~WindowShutdownOwnerTests'

pwsh -NoProfile -File eng/conformance.ps1
pwsh -NoProfile -File eng/measure-complexity.ps1 -Mode Inspect
git diff --check
```

- Three new schema6 acceptance cases fail against the unchanged old validator
  (baseline-document-tests.log), then pass in the final affected proof.
- Application122 succeeds. Infrastructure first runs266:253 succeed and13 fail only
  at old schema5 expected bytes/future-version6 assumptions/64KiB filler length.
  Update those expectations to schema6+null, future7 and23 fewer filler bytes; only
  the13 failed cases are rerun successfully. The distinct Infrastructure evidence is266,
  not279. No previously successful unchanged case is rerun for this correction.
- Presentation38 succeeds, including WindowShutdownOwner and changed UserSettings
  consumers. Final App and affected test builds have0 warnings/0 errors.
- The initial restore/build attempts exposed absent assets, missing using/imports,
  inaccessible WindowResizePlan construction and ordinary compiler/style failures.
  PlanNormalBounds projects through Application rather than widening its constructor.
  All failed logs are retained. A cp932 script-read failure occurred before edits;
  the retry explicitly used Python UTF-8. No suppression or failure masking was added.
- Final conformance passes112 unique rules. Complexity exits1 with exactly the prior
  five findings, including path/location/metric/value equality. The intermediate new
  42-line constructor finding was removed by extracting the existing caption composition;
  final complexity-comparison.json records equality, not a clean complexity claim.
- Changed C# whitespace verification uses dotnet format whitespace on each of the six
  affected project files, --no-restore --include <the changed absolute C# paths>
  --verify-no-changes. All exit0; the existing App workspace-load warning is retained.
  Initial ADR-index extra EOF whitespace was corrected. Final diff whitespace passes.

The exact thirteen-case correction command is retained in
infrastructure-corrected-command.txt. Its successful result is
infrastructure-corrected-13.log; the earlier script/old-test attempt remains
infrastructure-correction-attempt-1.log.

## Host failures, controlled comparisons and corrections

The App-source copy redirects settings and initial panes to owned D folders, omits
Activate and replaces actual focus calls with counters/no-ops. Its original settings
read/store/session/normal-close drain and final Window.Close remain in use. All launches
use the exact diagnostic executable and Start-Process -WindowStyle Hidden. Each check
rejects visibility/foreground ownership. These are unshown real controls, not physical
input or visible-screen acceptance. Fixture-only analyzer disabling does not affect the
product build. All substitutions and copied-source equality are separately recorded.

| Experiment | Observation and consequence |
|---|---|
| Original host, ordinary125% save/restart, corrupt and absent input | Expected geometry/bytes/writes pass and processes exit0. |
| Original host, extreme saved values corrected onto175% display | Geometry and final bytes pass before close, but process exits0xc000027b. Pre-close JSON status alone is not an overall pass. Event1000 identifies CoreMessagingXP; WER retains a stowed error. |
| Restart saved840x600 on175% from125% startup display | Actual1176x840 fails exact-size assertion. Moving origin before applying the physical rectangle yields exact840x600 and exit0. |
| Detach diagnostic Changed observer; detach caption observers; omit forced UpdateLayout; restore after composition | None fixes the extreme-size close failure. Those speculative changes are not in the product. Extra same-value reruns also exposed a fixture expectation of one write; the fixture now derives expected writes from actual value change. |
| Minimal empty unshown WinUI window, full size then small, and full-size close | Both exit0. This does not prove the complete App safe. |
| Local debugger built with installed compiler/System32 dbgeng and Microsoft header | The owned process does not reproduce the stowed failure under the debugger. A wrong-thread diagnostic is observed, not promoted to a proven root cause; no exception dump is produced. Header, source/hash and logs remain local, with no product dependency. Initial shell quoting and callback-class naming errors are retained. |
| Parent856cb6f App with matching hidden full-to-small geometry and normal close | Exit0 with settings byte-identical, separating the new final-save host behavior from parent behavior. |
| Stop RenderSession from the final-save observer after _stopping | Same extreme geometry saves and exits0. The observed closing-content reconstruction trigger is corrected without changing queue/failure semantics. |
| Independent review of stopping behavior | Finds direct OperationProgressed bypass. A real post-stop callback probe fails with exit2 before repair; RenderPanes and pending/direct focus guards make it pass. No native crash claim is inferred from that callback probe alone. |

The official AppWindow Move/MoveAndResize APIs define screen-coordinate operations;
the measured cross-DPI sequencing result, not an undocumented scale guess, determines
the correction. ADR-0089 links the API references. Read-only Nib main64d612e and
integration732ce712 showed centering in EditorWindowLifecycle, not the assumed placement
persistence implementation; no Nib files were changed.

## Final unshown App proof and artifact identity

Final2 is rebuilt from the corrected product source, removing the diagnostic caption,
initialization-order and minimal-window variants. The natural final settings callback
increments a counter only inside the real stopping guard. The direct operation and
pending/direct focus probe counts rendering/focus attempts while native focus stays
replaced. It cannot turn a real focus attempt into shared-desktop input.

Final fixture command:

```powershell
dotnet build D:/NeNeCommander/outputs/window-placement-20261123/control-fixture/App/NeNeCommander.App.csproj -c Release --no-restore --no-dependencies
```

It exits0 with0 warnings/errors. Six sequential launches all exit0:

| Case | Writes | Result |
|---|---:|---|
| schema5 first save | 1 | Centered initial rectangle, owned resize, schema6 final placement; other preferences preserved. |
| ordinary restart | 0 | Exact840x600 saved rectangle and bytes retained. |
| extreme/off-screen save | 1 | Full selected work-area correction at175%, then840x600 saved without redraw during close. |
| corrected175% restart | 0 | Exact saved physical rectangle and bytes; no DPI enlargement. |
| malformed placement | 0 | Rejected bytes remain intact; no automatic repair on close. |
| absent settings | 1 | Default preferences plus final rectangle written once. |

All six preserve unshown/foreground invariants and pass the late operation/focus probe.
Every changed final write observes one suppressed stopping render; unchanged/corrupt
cases observe none. launch-final2-summary.json and controls-final2-*.json retain
process IDs, actual scale, dimensions, checks, activation lists and settings hashes.

Final-artifact parity compares171 App source files after only the declared diagnostic
substitutions,132 XBF,5 PRI and4 dependent product DLLs: no mismatch. The1243 relevant
source/test/tool/dependency inputs are frozen in final-input-hashes.json. These hashes
permit reuse across commit/push without rerunning unchanged successful tests.

Independent read-only review initially accepted the core placement/schema/queue logic.
The host correction review reported the one direct-progress omission; the19:34:58
readback accepts its repair with no further required correction. It ran no additional
tests, UI, file edits or external review send.

## Remaining limits and lifecycle

No native visible maximize/minimize/restore, physical multi-monitor movement, display
removal, Windows contrast/speech or hide screen acceptance is claimed by the unshown
fixture. Pure state/planner proof covers its declared logic; actual environment proof
remains distinct. No release/beta.3 replacement or #523/#453/#491 resolution is claimed.
Normal close drains; kill/crash/OS session end retain ADR-0071 limits.

Current user App/settings remain outside these fixtures. Pending sources, branch commits,
control-fixture, baseline-856cb6f and diagnostic outputs remain under the task's D folders
until integration/required reflection/artifact preservation are complete. No new cleanup
auto-review rejection or hide cleanup request exists. Publication will use concrete
branch/PR payloads; earlier approval of Issue567 is not reused for an unrelated payload.
