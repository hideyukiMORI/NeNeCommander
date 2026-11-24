# Pointer interaction checkpoint — 2026-11-24

Status: local technical checkpoint; ongoing milestone work is not stopped.

Issue530 lives in `D:/NeNeCommander/worktrees/issue-530`, branch
`feat/530-pointer-command-entry`, based on mainaf17755. The existing release
candidate is a separate worktree/payload and is not modified by this feature.

## Invariant and mechanism

Only the existing CommanderSession/DualPaneSession/PaneSession/PaneReducer owners
accept selection, active-side and opening changes. PanePointerMapper translates
framework facts, requires exact rendered ownership and records actual admitted
before/after transitions. Native ListView selection is reconciled from Application
state. Header and empty-surface receipts preserve model references. Only accepted
synchronous pointer input returns focus; Open completion, stale requests, modal/
busy ownership and stopping cannot do so. F10 enters the three existing global
command buttons; file-list Tab keeps pane switching. See ADR0090.

## Evidence

- Application:980/982 success,0skip,100% branch coverage. The other two tests are
  unchanged-base fixture failures (command count23/24 and old UserSettings reflection
  argument count), not suppressed or repaired in this feature.
- Presentation:436 distinct cases successful across consumer02 and the corrected
  seven-case focus class; six cases overlap. Branch coverage96.827495%.
- Normal App build02:0warnings/errors. Record closure02:145 abstract/134 visible
  roots,0failure. Conformance06:112 rules. Complexity04:four unchanged findings.
- Isolated actual controls:93checks each in en-US/ja-JP/zh-Hans at125%, with layout
  after800x600 and1200x800 resize requests. Latest-source stop probe:96checks and
  normal exit0 after its earlier same-probe failure. Input desktop stays Default.
- Source correspondence:1267 frozen proof inputs;175 App inputs unchanged at
  final diagnostic synchronization; all four production dependency DLLs match.
  The final test-only CRLF correction has normalized-text equality recorded.
- Read-only independent review: no required source correction. Existing proof is
  reused only for unchanged implementation/resources; the latest stop guard has
  its own successful probe rather than borrowing the earlier locale results.

Exact commands, scripts, hashes, failed attempts and limits are in
[the quality record](../quality/2026-11-24-pointer-entry.md) and
`D:/NeNeCommander/evidence/pointer-entry-20261124/`. The principal drivers are
`run-owning-consumers-01.ps1`, `run-owning-consumers-02.ps1`,
`run-current-controls-04.ps1`, `prepare-control-fixture-07.ps1`,
`run-isolated-controls-01.ps1` and `run-final-source-checks-01.ps1`.
The two failing owning tests mean the full canonical gate is not claimed green.

## Outstanding boundaries

The two visual proposals are comparison material, not approved final styling.
The background consultation returns no confirmed Claude Canvas URL. Preserve the
conversation preview and original proposal in the task output. Physical click/
double-click sequencing, actual key repeat/IME, caption drag/system-button hit
testing, screen-reader speech and visible theme/high-contrast acceptance remain
unproved. Direct command/automation-provider invocation and native focus traversal
on a non-input desktop must not be described as hide's physical acceptance.

Public source, PR creation, integration and release remain separate. Do not retry
the previously rejected frozen beta.4 publication through this feature or add
these changes to that payload. Its unanswered approval and running remaining
mutation proof are tracked in the release worktree/evidence ledger.

Retain this unfinished worktree and
`D:/NeNeCommander/outputs/pointer-entry-20261124/`. They contain uncommitted work,
the local diagnostic fixtures and the visual proposal; main integration and
visible acceptance have not occurred. No task cleanup is yet eligible.
