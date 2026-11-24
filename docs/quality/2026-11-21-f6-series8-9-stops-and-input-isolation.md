# F6 Series8/9 stops and input isolation — 2026-11-21

Issue #491 / PR #492 remains incomplete and Draft. This is a documentation-only
record of existing diagnostic results and hide's corrected testing constraint.
It does not accept an ABBA comparison, change product behavior, or close #453.
Public updates still await the previously requested destination approval.

The invariant is unchanged: a native F6 observation follows the existing command,
session, gateway, provider and rendering path. Its sole TrialLedger admission
cannot be retried after uncertain input. Historical A/B diagnostic inputs remain
frozen. The user restated on November 21 that testing must run in the background
without Computer Use on the shared desktop; that constraint now also appears in
[TST-003](../TEST_STRATEGY.md#tst-003--tests-are-deterministic).

## Series8: preparation stopped before launch

The separately prepared Series8 stopped when its refreeze helper read a UTF-8
plan using implicit cp932 decoding. The failure preceded plan/manifest mutation,
App launch, observer launch and native input. Its stopped-preparation record
reports zero for all three launch/input counts and disallows retry. Its fixed
inputs, settings and 16 unused fixture pairs remain preserved. The successful
after-prelaunch-stop freeze record retains 35 helper and 226 historical fixture
inputs. This series was not resumed.

## Series9: incomplete final block

The fixed plan required A1/B1/B2/A2, one new App per block, and one excluded warmup
plus three measurement positions per block. A1, B1 and B2 each completed four
acknowledged moves. A2 completed its warmup and first two measurement positions.
Their saved preparation, matching READY/DONE, observer exit0, outcomes and hashes
remain available. The three complete blocks retain their successful original
full-block analyses; those analyses were not rerun.

At 23:00:27 JST the last A2/3 Computer Use F6 callback returned a physical-Escape
stop. The saved controller records stopped=true, pending3, completedBlocks3,
active A2 completed3, 16 attempted input callbacks and 15 acknowledgments. An
attempted callback is not proof that a key reached the App. No replacement input
or continuation occurred. The last observer retained READY3 only, then timed out
with `No completion for sample 3` at 23:01:12 JST and actual exit -532462766.

After hide's correction, all further work used file/process APIs without shared
keyboard, mouse or focus input. The owned A2 App was identified by PID1580,
creation time and executable path. An initial pre-close check stopped before any
close request because CIM exposed microsecond precision while the process handle
retained an additional eight 100-nanosecond ticks. An independent read established
both exact timestamps and the same executable; the final check matched both
identities exactly. The sole Process.CloseMainWindow request at 23:11:36 JST
returned true, exited normally and was followed by confirmed process absence.
The observer was already absent. No forced termination or global input was used.

Normal-close raw output contains exactly three A2 samples, no errors or extra
receipts, three closed I/O rows, 27 callbacks and nine closed render spans. All
four blocks together retain 15 completed outcomes, 135 callbacks and 45 render
spans. They include four excluded warmups and 11 completed measurement positions.
The last fixture remains left2/right0; the other 15 are left0/right2. All 32 file
hashes match. Later pristine state does not remove the uncertain-input stop or
authorize replay. Accepted comparison samples remain zero.

## Preservation and verification

Permanent evidence is under
D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/series9-20261121.
The matching D output root retains all original binaries, fixtures and raw files.

The stopped audit verified saved preparation/DONE event prefixes, all 15 exact
handshakes and successful observer exits, the final READY-only timeout, raw counts,
32 fixture hashes and the unchanged input record. It is a preservation audit,
not a weakened full-block analyzer or a performance acceptance test. The frozen
four-sample analyzer was not modified or used to accept the partial A2 block.

The after-stop freeze succeeded for each variant's 1,366 source and 355 binary
inputs, 35 helper inputs, 258 historical fixture files and original 428-byte
settings/hash. All 428 transfer files, totaling 21,936,205 bytes, were copied to
the permanent transfer archive with complete path, size and SHA256 verification.
The final A2 raw file was also copied byte-for-byte. Original files remain.

These commands completed with exit0; the first had one earlier identity-check
failure before its sole close request, as described above:

    pwsh -NoProfile -File C:/Users/info/WORKS/NeNeCommander/artifacts/issue491-prepared-bridge-20261121/close-series9-app.ps1
    python D:/NeNeCommander/evidence/f6-continuation-comparison-20261119/series9-20261121/verify-freeze.py after-stop
    python C:/Users/info/WORKS/NeNeCommander/artifacts/issue491-prepared-bridge-20261121/archive-stopped-series9.py

These are historical attempts with CreateNew outputs, not instructions to rerun
or overwrite them. Their scripts are preserved in stopped-audit alongside the
result records. Repository verification is text/link/diff review plus ordinary
Commit hooks. No product build/test, full gate, mutation or hosted CI was started.

## Input constraint and remaining work

The shared-desktop Computer Use campaign did not honor hide's testing preference.
It must not be repeated. Python alone is not input isolation: SendInput, simulated
global shortcuts, pointer movement and focus-dependent automation still share
hide's desktop. Background tests use existing deterministic test runners or
interfaces without global input. Native keyboard/focus proof requires a separately
isolated desktop/session or VM, with its environment recorded before collection.
No such native environment is claimed ready by this record. Direct command/API
tests cannot stand in for F6 routing, native focus or rendered-display proof.

No new campaign is scheduled here. All Series1-9 remain stopped and must not be
pooled or resumed. The unfinished worktree and outputs remain pending acceptance,
publication and integration; no cleanup deletion was attempted. PR402 screen
acceptance, AppSDK PR498, #453 and release are separate outstanding work.
