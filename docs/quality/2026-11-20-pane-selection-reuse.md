# 2026-11-20 Owned pane selection — Issue #504

Status: accepted and integrated through PR505; exact task worktree/output cleanup complete.

## Invariant and canonical mechanism

PaneReducer remains the sole owner of focus/selection decisions; PaneState.Transition
remains the sole selection-storage boundary. On base main097bc22 every transition
copies selection into a List and ReadOnlyCollection, even when unchanged. The initial
sharing-only proposal was stopped: the retained public-API probe demonstrates that
non-generic ICollection.SyncRoot exposes that List. Clearing it mutates the selected
state and its record copy; adding through an Escape-empty selection also changes the
snapshot. Existing movement's defensive copy isolated its new state. The original
proposal was never used for performance collection.

The final private nested SelectionSnapshot owns a copied List, exposes IReadOnlyList
and readonly generic ICollection read/copy paths, and rejects Add/Clear/Remove. It
has no non-generic ICollection/SyncRoot route. Initial empty selections use the same
owner. Transition reuses only ReferenceEquals(selection, Selection); foreign Lists,
equal-content collections and readonly wrappers are always copied. Every movement
still produces a new PaneState. No second route, cache, dependency, public property
type, provider semantics, settings or native UI behavior is introduced.

Clamped movement and round trips may now compare record-equal/hash-equal to the old
state when every payload is unchanged, while object references remain distinct.
This is an intentional observable equality difference, not an equality-preservation
claim. Source review and explicit tests establish PaneSession reference publication
and Presentation selected-focus updates. Other readonly collections are outside this
bounded change; this is not a general snapshot-immutability audit.

## Selected proof and retained attempts

ADR-0061 ordinary internal implementation scope covers Application transitions and
direct Presentation/operation consumers. ARC-001/004/008, CMD-002,
CS-007/008/012/013/015, QLT-005/009/015 and TST-001/003 apply. No waiver. This does not
add a public command or provider/trust boundary, so focused deterministic ownership,
consumer and conformance evidence establishes the impact. No full/deep CI, coverage,
all-layer mutation, native UI, filesystem mutation or release was selected.

Permanent evidence: `D:/NeNeCommander/evidence/pane-selection-reuse-20261120`.
Task worktree/output: `D:/NeNeCommander/worktrees/issue-504` and
`D:/NeNeCommander/outputs/issue-504`. All commands, logs and failed attempts remain.

- The first baseline test build failed three analyzer rules (IDE0004/0200/0305).
  Corrected tests built with zero warnings/errors; old source then failed 7 of 12
  ownership/reuse cases, with 5 passing. This is the causal red, not the compile failure.
- The first candidate build failed IDE0046. Transition was changed to the equivalent
  conditional expression; no suppression. Final Application/Presentation Release
  builds have zero warnings/errors. The initial source copies and both failures remain.
- Final initial focused tests: Application129 and Presentation29 passed, no failures
  or skips. They cover empty/selected movement, paging/clamps/sort, foreign aliases,
  generic mutation refusal/CopyTo/index/enumeration, record copy/equality,
  hidden/toggle/Escape, session publication and selected-focus display.
- Independent implementation review had no required implementation correction. Its
  evidence check identified an unrun selected Copy case and missing selected Delete
  case. The added Delete test freezes two selected sources while focus is on a third,
  waits for confirmation, executes exactly those two deletions and preserves the old
  selection. The exact existing selected Copy progress case now runs too. The follow-up
  filter passed4 (including2 already-covered session cases):131 distinct Application
  cases in total. Follow-up Release build and changed-file format pass.
- Final complexity inspection exits1 with the identical26 existing findings; no new
  finding. The added Delete method has35 logical lines. This is not a full CS-013 pass.
- The first normal Commit hook rejected the foreign-input test's bool mode parameter
  under CS-002. Its two cases were split into explicit raw/wrapped methods without
  changing assertions or production code. Only these2 cases, their Release build and
  changed-file format were repeated and passed. The old source is preserved and its
  hash equals the campaign freeze. All1003 campaign inputs still match except that
  test-form file; both final production DLLs equal measured B. Measurement and other
  unchanged behavior proof are reused; post-measurement-test-form.json records the
  exact exception/new hash and commands are in test-form-commands.json.
- Successful product proof is reused for subsequent documentation-only changes.

Exact entry commands (individual dotnet arguments, start/end times and exits are in
baseline-corrected-commands.json, focused-corrected-commands.json,
operation-boundary-commands.json, format-commands.json and final-complexity-command.json):

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/pane-selection-reuse-20261120/run-baseline-corrected.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/pane-selection-reuse-20261120/run-focused-corrected.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/pane-selection-reuse-20261120/run-operation-boundary.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/pane-selection-reuse-20261120/run-test-form.ps1
dotnet format whitespace tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --include src/NeNeCommander.Application/Panes/PaneState.cs tests/NeNeCommander.Application.Tests/PaneSelectionOwnershipTests.cs tests/NeNeCommander.Application.Tests/PaneSelectionSessionTests.cs --verify-no-changes --no-restore
dotnet format whitespace tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --include tests/NeNeCommander.Presentation.WinUI.Tests/PaneSelectionPresentationTests.cs --verify-no-changes --no-restore
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
```

The old-source command identifies retained historical proof; do not run it against
current candidate source expecting the same red. Ordinary Commit checks and final
link/diff review are recorded below when complete.

## Fixed comparison and limits

comparison-plan-v2.md predates collection. A is unchanged097bc22 and B is the final
PaneState implementation, built in the same worktree with matching pinned Release
settings. Of340 baseline production/config inputs only PaneState.cs differs; Domain
DLLs match. One package-free probe references A's unchanged public API. Its identical
DLL/deps/runtimeconfig is copied to both sides without replacing B's Application.
The freeze covers1003 production/test/tool/config inputs,10 distribution files and
5 helper/plan files. Every block verifies all before/after, including final integrity.

A1/B1/B2/A2 ran once each in four new processes. No replacement or removed outlier.
Each block uses1000 in-memory rows, selection counts0/2/128, start/end focus, then
movement and movement-plus-two-selection-sets boundaries. Each of12 scenarios keeps
one predeclared1000-call warmup and five measured1000-call batches. All288 batches,
240 measured/48 warmups and48 initializations pass sequence/count/semantic validation.
Raw ticks/bytes, every batch and all setup values are retained in JSONL and CSV.

The movement boundary includes Apply, the loop and fixed focus/count/visible-list
checks plus full ordered reference checks of both original/current selections. The
second adds two canonical-identity HashSet constructors and count checks. It is a
consumer primitive, not a complete Presenter. Setup includes creating/parsing1000
entries, constructing the pane, repeated selections and endpoint placement. No forced
GC, affinity, priority or runtime tuning, and no claim of isolated instruction time.

Primary128-selection/start/movement allocation is exactly1200bytes/call in all
measured A batches and96 in all B batches:1104bytes/call (92%) lower in this boundary.
The following allocation ranges include every measured batch and both endpoints:

| Boundary | Selected | A bytes/call | B bytes/call |
| --- | ---: | ---: | ---: |
| Movement plus checks | 0 | 152 | 96 |
| Movement plus checks | 2 | 192 | 96 |
| Movement plus checks | 128 | 1200 | 96 |
| Above plus two selection sets | 0 | 280–292.336 | 224 |
| Above plus two selection sets | 2 | 624 | 528 |
| Above plus two selection sets | 128 | 6752 | 5648 |

The A2 zero-selection/start/set batch with292.336bytes/call is retained; the extra
allocation's cause is unestablished. Consumer allocation savings remain visible;
there is no hidden HashSet capacity-growth cost in these measured scenarios.

Primary per-block timing ranges (microseconds/call, min/max of five batch means):

| A1 | B1 | B2 | A2 |
| ---: | ---: | ---: | ---: |
| 1.7878–3.9872 | 1.3194–1.3837 | 1.3451–1.4186 | 1.8049–19.9505 |

All48 per-block/scenario ranges are in comparison-analysis.md/JSON and
comparison-measured-ranges.csv. Timings are mixed elsewhere, with large retained
values. For example,128/end/two-sets ranges are27.6592–30.6521 (A1),
36.5816–46.4925 (B1),36.6557–38.9517 (B2),77.3089–91.1679 (A2). This is not a
universal speedup, percentile, statistical comparison or production guarantee.

Setup tradeoffs remain explicit: each2-selection setup allocated1,501,216bytes in A
and1,524,992 in B (+23,776). Primary128/start/movement setup allocated1,894,840 in A
and1,841,504 in B; its timing ranges overlap/reverse. Different scenarios and initial
JIT/environment effects are included, and the source of the extra setup allocation
has not been isolated. No global setup-allocation improvement is claimed.

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/pane-selection-reuse-20261120/prepare-comparison.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/pane-selection-reuse-20261120/collect-comparison.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/pane-selection-reuse-20261120/analyze-comparison.ps1
```

All comparison build/collection commands exit0; build warnings/errors are0. The
collector refuses existing block outputs, so these are evidence identifiers, not
instructions to replace samples. No UI/input/display/filesystem/cross-provider
latency claim, pooling with501/incomplete491, or resolution of453. PR402 still needs
hide's screen acceptance; no Preview/public release or normal-app replacement.

## Final review, integration and cleanup

Independent final review accepts implementation and independently recalculates all288 batches,48 initializations and48 ranges with complete agreement. All frozen inputs/distributions/helper hashes match. The requested explicit2-selection setup increase is recorded in both analysis and this quality record. No required product correction or remeasurement remains. Preserve the exact task
worktree/output until normal PR acceptance/main integration and full artifact
path/size/hash archival plus clean/ignored/link/live-reference checks. Keep branch,
commits and permanent evidence. Four older rejected cleanup targets remain separate
and are not retried; see the handoff.

PR505 merged normally at23:34:19 JST as4224a126546be7d32b525158aea168cb3dc10fdf,
with the identical accepted3376ed05021d5e6824252d25c28926de0bf29133 tree. Local
main fast-forwarded cleanly. Ordinary hooks passed112 normative rules and24
registered adversarial cases;216 local document links and1003 final inputs passed.
No hosted CI was started. The connector's PR creation returned403 (integration
permission); authenticated gh created the same Draft PR and performed normal
Ready/squash without bypass. This is distinct from automatic approval rejection.

All773 generated/output files,194,767,135bytes, are archived in task-artifacts.zip
with complete path/size/SHA256 verification. At23:35:31 JST the exact worktree and
output were removed after accepted-tree/main/clean/untracked/ignored/no-link/
no-active-reference/archive checks. Branches, commits and permanent evidence remain.
The first cleanup guard stopped before any deletion because sequential text
substitutions produced a wrong expected byte count. The retained script/failure
record documents the correction to194767135; the same bounded script then passed.
No automatic approval rejected this task's cleanup. Four older rejected targets
remain pending and were not retried. The final daily report/handoff supersede this
record's earlier pending paragraphs for session state.
