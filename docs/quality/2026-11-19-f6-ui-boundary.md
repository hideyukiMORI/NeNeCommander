# F6 native host boundary observation — 2026-11-19

Status: diagnostic observation complete; documentation integration and cleanup pending.

[Issue #479](https://github.com/hideyukiMORI/NeNeCommander/issues/479) extends the still-open
[#453](https://github.com/hideyukiMORI/NeNeCommander/issues/453) investigation. It observes
accepted main `1cc5d75819b8044349c0353e36e28c21c57cba50` in a dedicated diagnostic export.
It changes no repository product source, dependency, setting schema, release, or quality gate.
Permanent evidence is `D:/NeNeCommander/evidence/f6-ui-boundary-20261119/`.

## Invariant and exact endpoints

Native F6 still follows OnKeyDown → KeyboardIntentMapper → CommanderSession → DualPaneSession
→ FileOperationGateway → existing providers → both pane reads → existing RenderSession.
Identity, deletion-capability purpose, transfer receipts, operation ordering, scheduling,
cancellation, and view rendering remain the accepted implementations. ARC-001/005/007/010,
CMD-002/003/009, SEC-010 and QLT-015/ADR-0061 apply. No waiver is used.

The exported 1,346 tracked files differ in only CommanderWindow.xaml.cs, where the preserved
`diagnostic.patch` inserts observational calls and carries a sample token through the existing
RenderAfterAsync call. Two additional diagnostic-owned files contain the injected clock,
fixed six-sample recorder and output sink. All 1,348 resulting source/configuration/document
hashes are frozen before native collection and match afterward. Repository product files
remain unchanged. No Rendering/Rendered subscription, timer, new execution route or dependency
is added. Compiler/analyzer rules and warnings-as-errors are retained.

One Stopwatch clock, frequency 10,000,000 ticks/second, records:

1. Entry to OnKeyDown when the received key is F6, before the ordinary context/translation work.
2. Mapped Move immediately before the existing TryStartIntent call. This marker alone does not
   prove admission; the correlated completion and snapshot facts establish execution.
3. Return from the existing RenderAfterAsync await, after operation and both pane refreshes.
4. Return from its existing final RenderSession call, before diagnostic result facts are read.

The last endpoint is completion of view property assignment. It is not layout, a correlated
XAML frame, compositor presentation, physical display, or physical-key latency. The middle
interval includes the existing initial/progress rendering, operation/read work, scheduling,
and await continuation; it is not a pure provider interval or an inner-cause attribution.
Microsoft documents [Rendered](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.compositiontarget.rendered)
as uncorrelated to an individual UI element, while
[Rendering](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.compositiontarget.rendering)
can cause per-frame UI work. Neither API is used here. The research initially consulted
versioned/default documentation rather than the exact repository package version; no
exact-version rendering semantics are inferred from it.

## Fixed native collection and results

One excluded warmup followed by exactly five measurements was declared before collection.
One owned native app, PID25580/window70127390, moved fresh 41-byte TXT and 43-byte SQL pairs
from each dedicated D-drive left directory to its empty right directory. Ctrl+L/Tab navigate;
Space/Down/Space/Up select the two files; ordinary native F6 performs each move. No extra or
replacement sample was added after seeing results. Each case has success, two effects,
zero untransferred items, left0/right2, both pane activities idle, active-left and both hashes.

| Sample | Receipt to mapped Move ms | Move to await return ms | Final view assignment ms | Receipt to final assignment ms |
| --- | ---: | ---: | ---: | ---: |
| warmup-00 (excluded) | 0.2043 | 81.4483 | 4.9853 | 86.6379 |
| sample-01 | 0.0936 | 20.5617 | 2.6400 | 23.2953 |
| sample-02 | 0.0817 | 25.1301 | 2.8948 | 28.1066 |
| sample-03 | 0.0778 | 27.6497 | 3.3900 | 31.1175 |
| sample-04 | 0.0683 | 44.7258 | 2.6067 | 47.4008 |
| sample-05 | 0.0351 | 295.0270 | 2.7456 | 297.8077 |

The long fifth measurement is retained. Its 295.0270ms middle interval does not identify
which operation, rendering, OS/JIT/cache/background or observer contribution caused it.
Do not discard it, call it a percentile, or subtract the earlier non-UI #470/#473/#476 values.
This observation does not establish the cause or resolution of hide's reported pause.

The recorder has zero ordering errors/extra receipts and six complete four-marker samples.
Independent integer/Decimal arithmetic in `analyze.py` verifies 24 raw timestamps, 18 component
intervals, six exact totals, their chronological non-overlap, all eight result facts per sample,
six injected F6 actions, all12 final hashes and the frozen source/binary hashes. The first
sample is excluded by the prior plan, not by its value. `raw.json` and `analysis.json` retain
every value. Complete and intermediate native AX/images are preserved.

## Observer and evidence limits

The diagnostic App buffers marks without logging I/O between its markers and saves numeric
records only after the ordinary close/drain. However, the external CUA controller persisted
its key-action JSON immediately after sending F6 and then requested AX/screenshots. There is
no shared-clock proof that this external I/O/automation fell outside the measured interval.
The broad literal external-I/O-free condition is therefore **not established**. These are
instrumented descriptive observations, not an isolated performance baseline. The unknown
observer contribution is included and is neither measured nor subtracted. No replacement
run is added to repair this limitation after seeing the values.

Independent read-only review found no required marker/order code correction. It required this
external-observer limitation to be explicit in the structured analysis as well as prose.
`record-observer-limit.py` adds it to `analysis.json`, retaining `analysis-initial.json` and
all identical numerical rows; the analysis script is aligned for reproducibility. No new
measurement or product test accompanies that recording correction.

Immediate readbacks of samples02/04/05 captured incomplete row realization or preceding status;
later saved readbacks show both destination rows and completion. Some AX focus/address fields
also lag native transitions. Screenshots and settled states supplement those reads, but none
is a timestamped completion-frame measurement. The record does not claim physical held-key,
cold/large-tree/cross-volume/UNC/WSL, unattended display, latency-distribution or AV evidence.
The final snapshot facts and filesystem postconditions are verified independently of UIA lag.

## Verification, trials and preservation

The package-free recorder self-check passes exact ordered six-sample ticks and ignored non-F6,
overflow, incomplete, overlap, stale-token, out-of-order, duplicate and non-increasing-clock
cases. The first App build failed only IDE0032 and IDE0037 in the diagnostic helpers. An
auto-property and anonymous-member inference corrected those two style findings without
suppression or changed markers. Initial sources, manifest and failed log remain preserved.
The corrected self-check and locked-restored Release App build pass with zero warnings/errors.
No product test, full CI, coverage or mutation rerun is needed for this documentation change.

Exact successful driver commands (each exit0):

```powershell
python D:/NeNeCommander/evidence/f6-ui-boundary-20261119/prepare.py
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-ui-boundary-20261119/build.ps1 -Part SelfCheck
python D:/NeNeCommander/evidence/f6-ui-boundary-20261119/freeze.py
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-ui-boundary-20261119/build.ps1 -Part SelfCheck -Attempt '-corrected'
pwsh -NoProfile -File D:/NeNeCommander/evidence/f6-ui-boundary-20261119/build.ps1 -Part AppBuild -Attempt '-corrected'
python D:/NeNeCommander/evidence/f6-ui-boundary-20261119/prepare-fixtures.py
python D:/NeNeCommander/evidence/f6-ui-boundary-20261119/verify-fixture.py 0
python D:/NeNeCommander/evidence/f6-ui-boundary-20261119/verify-fixture.py 1
python D:/NeNeCommander/evidence/f6-ui-boundary-20261119/verify-fixture.py 2
python D:/NeNeCommander/evidence/f6-ui-boundary-20261119/verify-fixture.py 3
python D:/NeNeCommander/evidence/f6-ui-boundary-20261119/verify-fixture.py 4
python D:/NeNeCommander/evidence/f6-ui-boundary-20261119/verify-fixture.py 5
python D:/NeNeCommander/evidence/f6-ui-boundary-20261119/analyze.py
python D:/NeNeCommander/evidence/f6-ui-boundary-20261119/preserve.py
```

The initial `build.ps1 -Part App` exits1 after successful `dotnet restore
src/NeNeCommander.App/NeNeCommander.App.csproj --locked-mode` and the two-style-error
`dotnet build ... -c Release --no-restore -m:1 -nr:false`. The corrected AppBuild part reuses
that restore. Command arguments, timestamps and exits are in `build-*-commands.json` and logs.
Two read-only lookup attempts used absent guessed App paths; they caused no source or fixture
effect. Subsequent source discovery used `rg --files`. All collection attempts are retained.

At17:51 JST the owned window/process exited through Alt+F4 and normal PID38360/window200412
remained. Original218-byte settings match exactly, SHA256
`ABE305E4BC6C35A1325B2EED2389DD7D766E75D6B74C5236950E071C2563DDA5`; no restoration was needed.
All1,950 output files/187,569,304 bytes, including source, build products, fixtures and raw
records, are preserved in `diagnostic-output.zip` with independent size/SHA256 verification.
`preservation-manifest.json` records every file and the archive hash. Normal app and public
beta.2 remain unchanged. Draft PR402 at63f4769 still requires hide's separate screen acceptance.

Only documentation integrates from `D:/NeNeCommander/worktrees/issue-479`. Its temporary
diagnostic output is `D:/NeNeCommander/outputs/f6-ui-boundary-20261119`. Accepted integration,
clean/untracked/ignored checks, live-reference/link checks and archive verification precede
their removal. Four older automatically rejected cleanup targets are not retried.
See the [progress report](../reports/2026-11-19-f6-ui-boundary-progress-report.md) and
[handoff](../handoffs/2026-11-19-f6-ui-boundary-handoff.md).
