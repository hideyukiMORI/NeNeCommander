# Equal sort projection reuse — 2026-11-22

Issue: #519. Base: main7f0e6d3.

## Invariant and canonical mechanism

PaneState.WithSortOrder reuses its owned ordered and visible sequences when the
incoming PaneSortOrder is value-equal. Only that projection step returns early.
PaneReducer.ApplySortOrder still repairs focus and publishes its transition,
including a newly supplied preferred focus after a read. Changed orders still
use EntryOrdering.Apply. Selection, hidden-item policy, navigation/history,
provider identities and fresh-listing ownership retain their existing owners.

The behavior change is a four-line equality guard, with an updated ownership
comment. Current navigation already builds the default projection before
applying the carried order; equal-order navigation therefore avoids a repeated
sort and visibility copy. A refreshed listing creates a new state and never
reuses the previous listing's entries.

The concrete equal-order visible sequence now retains reference identity.
The reducer still publishes a new state. Fresh value-equal order instances may
resolve to the state's previous order instance; public value equality remains.
No external cast, dependency, schema, provider, UI event or new execution path.

## Focused proof

- Initial new-test build stopped on one unused System using. After removing it,
  the unchanged product builds with zero warnings/errors and runs eight cases:
  six fail on projection reuse, two changed-direction/refresh controls pass.
- Final Application Release build and all218 selected cases pass, including
  equal-order visible/hidden/missing/null focus, independently parsed Windows
  identities, nondefault/empty order, selection/history, new refresh entries,
  navigation, abandonment, natural sorting and the existing null guard.
- Presentation Release build and all36 direct row/selection/reuse/automation
  projection cases pass.
- Locked App restore and App/consumer Release build: zero warnings/errors.
- Changed-file whitespace exits0 without warnings.
- CS-013 Inspect exits1 for the identical26 full finding objects from accepted
  #510 evidence. No new finding; no repository-wide complexity pass is claimed.
- Self-review confirms the guard is below the reducer, uses value equality,
  preserves focus recovery and never caches across states or new reads.
  No independent-agent review is claimed.

Ordinary focused scope covers the changed projection and direct state/display
consumers. Full/deep CI, broad coverage/mutation, native UI and release proof
are not selected. Rules: CMD-002, CS-007/013/015, TST-002/003, ADR-0053/0056/0061.

Exact arguments and exit codes are under
D:/NeNeCommander/evidence/equal-sort-projection-20261122 in red-commands.json,
red-corrected-commands.json and focused-commands.json.

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/equal-sort-projection-20261122/run-red-corrected.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/equal-sort-projection-20261122/run-focused.ps1
python D:/NeNeCommander/evidence/equal-sort-projection-20261122/validate-comparison.py
```

These are historical runs, not instructions to overwrite red evidence with the
new product. Normal commits retain eng/check.ps1 -Mode Commit.

## Fixed managed comparison

comparison-plan.md predates measurement. Old A is the red-test build; B differs
only in PaneState. One identical Release runner executes A1/B1/B2/A2 in four fresh,
serial processes with tiered compilation disabled. It binds the internal reducer
once to a typed delegate before timing. There is no reflection invocation in a
measured loop, native App/input or filesystem provider.

Four sizes0/2/100/10000, three order cases (equal default, equal extension
descending, changed direction), and absent/present preferred focus produce24
scenarios per phase. Each has10 excluded warmup calls followed by five20-call
batches. All576 rows validate:96 warmup,480 measured,9600 measured calls.
No row is discarded or repeated. Every output preserves exact entry objects,
correct focus path, selection ownership and the expected projection reuse.
All1035 source/build/test/tool/helper/DLL input hashes remain unchanged.
Full results, including small cases and unfavorable variation, are retained in
comparison-results.md and raw A1/B1/B2/A2 JSON.

For the predeclared primary10000/equal-default/no-preferred-focus case:
A batch means span9.215600–9.910630ms/call; B spans0.045935–0.059680ms/call.
Allocation is401096bytes/call in every A batch and96 in every B batch.
The remaining scan for an absent focus is included; this is not constant-time
proof or a whole-navigation benchmark.

At10000 with a preferred focus, A spans9.772890–10.482325ms/call and B
0.845780–1.157810ms/call; B still allocates673240–673645.2bytes/call.
Existing focus recovery remains substantial and is not optimized by this issue.
Changed-direction10000/no-focus costs remain5.435405–7.031030ms/call in A and
5.032125–5.828455 in B, with overlapping ranges; no changed-order speedup is claimed.

These are managed reducer-loop batch means and observed thread allocations on
this machine. They are not native UI latency, per-call percentiles or whole-app
speedups. Earlier exploratory baseline used different fixture/runtime conditions
and is not compared numerically with this fixed series.

## Integration and retained boundaries

The closing PR and permanent completion.json own actual main integration,
artifact verification and exact task cleanup. Retain issue519 worktree/output
until those steps finish. Branches, commits and permanent evidence remain.
PR402 hide screen acceptance, native #491/#453 proof, Draft AppSDK PR517 and
Issue518's operation-source correction remain distinct. No published App/release.
