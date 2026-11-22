# Post-move focus recovery — Issue #550

Date: 2026-11-22
Status: implemented and locally verified; the closing PR/completion record owns integration and cleanup.

## Scope and invariant

The accepted base is `0ffcac61f293ecf628733630d6e73f8675fc02bf`, after beta.3
publication and the cyclic focus change. hide reported F6 returning the source
pane to its first entry and requested a nearby surviving item.

`PaneSession` captures the old immutable state before its existing read. On a
successful Preserve read, `PaneReducer.RecoverRefreshFocus` extends the existing
focus recovery: preserve a present preferred identity; if the removed preferred
identity was the former focus, search forward in the old visible order and then
backward for a currently visible survivor. If no old identity survives, retain
the new listing's default focus; an empty visible set has no focus.

The shared `NearestVisible` scan now accepts the ordered entries and the current
visible-identity set. `HashSet.TryGetValue` returns the path owned by the new
listing, including provider-equivalent spelling. No old path reference becomes
new focus. Scans are bounded by the existing 10,000-entry limit and linear.

Explicit creation/rename targets retain their existing policy. Direct navigation,
history, failed/cancelled/stale reads and file-launch freezes keep their previous
contracts. Selection still clears only after successful refresh. Move, deletion,
copy and partial/cancelled outcomes all use the same actual refreshed listing;
no operation result predicts which paths still exist. DualPaneSession,
FileOperationGateway and provider execution are unchanged. No UI index selects
physical operation sources. No dependency, settings, key binding, trust boundary
or second transition mechanism is added (CMD-001/002/003, ARC-001/004/005, FS-001).
COMMAND_MODEL and KEYBOARD_MODEL state the general refresh contract explicitly.

## Trials and focused evidence

Permanent evidence: `D:/NeNeCommander/evidence/post-move-focus-20261122`.
Dedicated worktree: `D:/NeNeCommander/worktrees/issue-550`.

- The first test-only build failed with six diagnostics: an assumed outcome
  property, two collection-style findings, two unused assertion return values,
  and one missing public test parameter guard. They were corrected without
  product changes. Both original and corrected logs are retained; corrected
  baseline build has zero warnings/errors.
- On unchanged product hashes, the initial18 cases produce ten causal focus
  failures and eight passes, zero skips, exit2. The two baseline product hashes
  and `baseline-summary.json` establish that the fix was absent.
- After the two product-file changes,21 focused cases pass with zero failures
  or skips. They cover surviving focus, first/last and nonadjacent removals,
  fresh path ownership including case variation, all-old-items replaced, empty
  before/after, descending order, hidden successors, explicit targets, direct
  same-location navigation, failed/cancelled reads, late superseded reads,
  both F6 source sides, partial failure and cancellation after one deletion.
  The successful F6 fixture was refined to a destination without a same-name
  collision and proves passive focus stays on p even when a new b sorts first.
- The existing29-class Application scope runs244 cases:243 pass, one new cyclic
  test still expects the old first-item refresh default. It now expects the
  surviving predecessor and then proves next/previous cycling through both new
  endpoints. Only that changed test is rerun and passes. The243 unchanged
  successes are reused. Distinct accepted Application proof is265 cases, not a
  sum that double-counts the rerun. Product behavior did not change during this
  test-expectation correction.
- Relevant App/projection builds, ten Presentation classes, changed-file
  formatting and CS-013 inspection are recorded by `affected-commands.jsonl`.
  Final App and test builds have zero warnings/errors, Presentation52/52 passes
  with zero skips, and both formatting checks exit0. CS-013 exits1 with exactly
  the same five findings as the accepted cyclic-focus source; no changed file
  introduces a finding. `complexity-comparison.json` records exact equality.

## Exact commands

All commands run from the dedicated worktree. The restore/build/test logs retain
stdout/stderr and exit results. `run-affected-initial.ps1` preserves the first
244-case run; `run-affected.ps1` records the focused continuation. Scope JSONs
contain every selected class and the exact filter. The21 focused successes are
not repeated in the existing29-class run.

```powershell
dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore -maxcpucount:4
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~PaneRefreshFocusTests|FullyQualifiedName~DualPaneMoveFocusTests'
pwsh -NoProfile -File D:/NeNeCommander/evidence/post-move-focus-20261122/run-affected.ps1
```

The runner uses the same locked Release restore/build form for App and
Presentation.Tests, filtered `dotnet test --no-build --no-restore`, two changed
project `dotnet format whitespace --no-restore --verify-no-changes --include`
commands, and `pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect`.
Its machine-readable command record contains the complete argument arrays and
per-command start/end times and exit codes. The ordinary commit hook remains
`pwsh -NoProfile -File ./eng/check.ps1 -Mode Commit`.

## Limits and lifecycle

This is deterministic application and presentation evidence. No shared-desktop
input, native F6/keyboard focus confirmation, latency benchmark, user App stop
or settings write is performed. The owned native check proposed in the Issue
remains unperformed under TST-003; it is not replaced by a claim about the
managed tests. No new isolated environment is provisioned under ADR-0054.
#523 reactivation and #453/#491 native latency remain separate. Published beta.3
assets/tag are unchanged; this is next-version source work.

Successful focused evidence is reused only while relevant inputs remain equal.
No full CI, all-layer coverage/mutation, duplicate release proof or new CodeQL
upload is justified by this scoped reducer change. Final diff review, normal
Draft/Ready PR and exact-head squash precede cleanup. Preserve branches/commits
and permanent evidence, verify unique generated artifacts and live references,
and remove only the completed task's D worktree. The closing PR and
`completion.json` own actual integration and cleanup results.

Final source/test/dependency/tool inputs: 1079 SHA256 records in
`final-inputs.json`. Subsequent documentation and lifecycle steps reuse this proof
when the recorded inputs match. The source/diff was reviewed directly; no
additional agent was needed for this bounded reducer/session change.
