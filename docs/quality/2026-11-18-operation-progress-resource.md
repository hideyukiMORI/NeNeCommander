# Operation progress resource correction — Issue #448

## Report and invariant

hide reported on 2026-11-18 that Commander felt slow and exited after a few seconds
when two files were selected with Space and moved with F6. The supplied scenario
was Downloads to a project directory under a user-owned Works directory, with one
SQL and one TXT file. The exact executable, version and machine are not yet known.

The canonical mechanism is unchanged: keyboard intent → `DualPaneSession` →
`FileOperationGateway`, with `DualPanePresenter` and `OperationBarView` rendering
the existing operation progress. Every resource requested by the view must exist.
Rules: ARC-001, CMD-001/003, CS-017/023, QLT-007/015 and TST-011/012. ADR-0061
selects the App build and directly affected resource/progress/move boundaries;
no provider, identity, deletion, settings, dependency or schema contract changes.

## Diagnosis

At main `05d673e` and public Preview source `46f1a82`, the running-operation view
requested `_operationProgressSeparator`. Both shipped languages define
`OperationProgressSeparator` instead. The error originated in the earlier view
split, #330/PR331. `DualPaneSession` sets `OperationRunning` before waiting for
the gateway, and `CommanderWindow.RenderAfterAsync` renders the current state
before awaiting that task. A rendering exception faults the owned work and
`CommanderApplication.ReportDefect` rethrows it. Later progress rendering reaches
the same call. The defect is not specific to two files or these extensions.

The public package's actual `ResourceLoader`, projection DLL and PRI were used
in a separate PowerShell process. The old key threw `COMException` **0x80073B17**
(NamedResource not found); the correct key returned the existing ` / ` value.
This proves the resource exception, not an observed reproduction of hide's entire
desktop crash. The first diagnostic command had a PowerShell pipeline syntax
error before executing; the saved script corrected that form and ran successfully.

The initial read-only host inquiry found no Commander Application 1000/1001/1026
event in the preceding three hours; the newest matching dump/WER records were
from November 15. McAfee and Windows Defender registration was observed, but no
interference evidence was found. No security configuration was changed. These
observations do not establish the environment in which hide encountered the crash.

## Change and regression evidence

The production change is one resource-key literal in `OperationBarView`.
No exception is swallowed and no alternate progress or operation path is added.
[ADR-0081](../adr/0081-check-literal-localization-keys.md) records the added gate;
its policy record was added during final review before integration.
The CS-023 check in `eng/conformance.ps1` compares direct, simple identifier
literals passed to `GetString` against each shipped `.resw` catalog with ordinal
case. It is a constrained source scan, not a semantic C# parser; dynamic keys,
aliases, escaped strings and comment parsing are outside its proof.

The new check failed on the unchanged old production call with exactly two
CS-023 violations, one per language. After correction,
`pwsh -NoProfile -File eng/prove-gates.ps1 -Rule CS-023` passed its valid baseline,
old-key, wrong-case and missing-English-key fixtures. A final run added the
absent-catalog fixture and passed **all five**. The source-file collection is
explicitly an array so zero-file input cannot cause a strict-mode property error.
The full proof entry also calls those fixtures. `docs/quality/GATE_PROOFS.md` records the new gate, following
the independent read-only review's one required correction.

`WindowsLocalMoveTests` adds a batch of two test-owned SQL/TXT files with distinct
contents and a nested destination. It checks both atomic effects, both missing
sources and both preserved target contents through the real Windows adapter and
gateway. It does not send desktop input. Existing `DualPaneMoveSessionTests`
cover explicit two-item selection; `DualPaneProgressPresenterTests` cover the
running and progress snapshots consumed by the view.

## Verification and remaining limits

Permanent evidence: `D:/NeNeCommander/evidence/issue-448/`.
The runtime probe is `probe-resource.ps1` / `probe-resource.log`; the failing
old-source check is `conformance-red.log`; focused gate proofs are in
`resource-gate-proofs.log` and the final five-fixture run is in
`resource-gate-proofs-final.log`. `verify.ps1` records exact selected dotnet commands,
exit codes, logs and input hashes. Locked Release restores and the App plus
three selected test-project builds passed with zero warnings/errors. The selected
tests all passed: Windows local move **7**, dual-pane move session **5**, and
progress presenter **3**; zero failures/skips. All captured inputs were unchanged
through execution. A later XML-comment correction removed the inaccurate word
"Unicode" from the new test description; the fixture uses ASCII names. Executable
test and product inputs are unchanged, so this correction reuses those results.

Exact invocation pattern: `dotnet build <project> -c Release --no-restore -m:2`
and `dotnet test --project <test-project> -c Release --no-build --no-restore
--filter FullyQualifiedName~<fixture>`, with minimum expected counts 7/5/3 and
TRX reporting. `commands.json` records complete arguments, times and exit codes.
No full gate, mutation, coverage or CI is selected for this resource-literal fix.
Both changed C# files passed whitespace verification without edits. The first
App formatter invocation reported a design-time workspace warning despite exit0;
the diagnostic invocation with `Configuration=Release` passed with no such warning
and changed no files. This additional invocation investigated that specific warning.
Normal commit checks passed: conformance (112 rules), security (24 registered
adversarial cases), Commit mode and the commit-message validator. Documentation-only
completion/ADR updates reuse the unchanged product and focused gate evidence.

No live UI sequence, general latency measurement, antivirus attribution, or
claim of zero partially completed file effects is made. The gateway is started
before the initial render. Issue #94 and PR402's separate screen acceptance
remain open. Waivers: none.

## Integration and reflection

PR449 merged to `6b0d2316101614ab8bc1cb4a979816e5a98416eb`. The normal App
Release restore/build passed with zero warnings/errors and its ProductVersion
ends in that commit. Main is clean and its tree equals the tested task branch.
The app was not launched; the public beta.1 archive was not replaced.
The extra task worktree was removed after source/ignored-file/link/process
checks. All 11 output files were preserved and hash-verified; automatic approval
rejected output deletion before process launch (`blocked by policy`). No retry
was attempted. The prepared user cleanup command owns this new pending target.
`normal-after.json`, `cleanup-pending.json` and `completion.json` retain exact
paths, hashes, publication limits and cleanup status.
