# 2026-11-20 Pane focus reference scan — Issue #501

Status: accepted and integrated through PR502; exact task worktree/output cleanup complete.

## Invariant and canonical mechanism

Base main5add0bb uses a LINQ indexed enumeration and provider-aware path comparison
inside PaneReducer.GetFocusIndex on every movement. The completed public/reducer
PaneState already stores the exact Path instance of a visible entry as FocusItem;
empty visible sets have null focus. Creation and movement take actual entry paths.
External preferred paths still resolve through FileSystemPathIdentityComparer,
then RecoverFocus/NearestVisible returns the real entry's path. Sorting and
visibility preserve entry objects, and successful refresh/history navigation
repairs focus against the new listing before movement. Record copies retain the
same relationship. A temporary internal visibility rebuild is repaired before
being returned from the reducer.

Only GetFocusIndex's executable implementation changes: a direct indexed scan
uses reference equality and throws InvalidOperationException if the invariant is
broken. There is no identity fallback, extra transition path, focus-index cache,
new dependency, public representation or filesystem/UI behavior. The search
remains linear. PaneState XML comments explain the existing invariant, including
null for an empty visible set. All production Transition callers remain in the
reducer. The added corrupted internal state test proves a defect is exposed.

Independent design and implementation reviews found no required correction.
The six added Application cases cover external Windows/UNC case variants, WSL
alias/case-sensitive matching, selection and record-copy movement, refreshed
path instances, and rejection of a broken internal reference. Existing selected
cases cover empty/bounds/paging/hidden/sort/navigation/history and Presentation
row-source reuse. Provider identity semantics at external boundaries are unchanged.

## Selected verification and exact commands

ADR-0061 ordinary internal implementation scope: owning Application and directly
affected Presentation consumers, deterministic focused tests, changed-file format,
complexity inspection, source review and one bounded package-free diagnostic.
ARC-001/004/008, CMD-002, CS-001/007/008/013/014/015, QLT-005/009/015 and TST-001/003
apply. No waiver. No native App, filesystem mutation, full/deep CI, coverage,
mutation, release or unrelated provider suite was selected. Public contracts and
trust boundaries are unchanged; focused transition and consumer proof addresses
the actual impact.

Commands ran in D:/NeNeCommander/worktrees/issue-501. Exact arguments/times/logs
are retained in baseline-commands.json, focused-commands.json,
probe-build-commands.json and final-format-command.json under the evidence root.

```powershell
dotnet restore src/NeNeCommander.Application/NeNeCommander.Application.csproj --locked-mode -p:Configuration=Release
dotnet build src/NeNeCommander.Application/NeNeCommander.Application.csproj -c Release --no-restore -m:2 -p:UseSharedCompilation=false
dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --locked-mode -p:Configuration=Release
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore -m:2 -p:UseSharedCompilation=false
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~PaneFocusReferenceTests|FullyQualifiedName~PaneReducerTests|FullyQualifiedName~PaneReducerNavigationTests|FullyQualifiedName~PaneSortOrderTests|FullyQualifiedName~PaneSessionNavigationStateTests|FullyQualifiedName~PaneNavigationHistoryTests|FullyQualifiedName~PaneSessionTests|FullyQualifiedName~DualPaneNavigationTests'
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --locked-mode -p:Configuration=Release
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore -m:2 -p:UseSharedCompilation=false
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~PaneListingReuseTests|FullyQualifiedName~PaneListingPresenterTests|FullyQualifiedName~PanePresentationTests|FullyQualifiedName~DualPanePresenterTests'
dotnet format whitespace tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --include src/NeNeCommander.Application/Panes/PaneReducer.cs src/NeNeCommander.Application/Panes/PaneState.cs tests/NeNeCommander.Application.Tests/PaneFocusReferenceTests.cs --verify-no-changes --no-restore
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
```

All restores/builds exit0, with zero build warnings/errors. Application103 cases
(including six new) and Presentation28 pass, zero skips/failures. Final whitespace
exits0. Complexity exits1 with exactly the same26 prior findings by
path/metric/value/limit; there is no introduced finding and no repository-wide
complexity pass. Normal Commit checks pass (112 normative rules,24 registered adversarial cases;
no product tests in Commit mode). Final whitespace/diff,214 local links and all
1015 frozen inputs pass. Passing product proof is reused for documents.

## Fixed comparison and retained results

Permanent evidence: D:/NeNeCommander/evidence/pane-focus-scan-20261120.
Dedicated output: D:/NeNeCommander/outputs/issue-501. plan.md was saved before
comparison collection. A retains the unmodified baseline assemblies built in the
same worktree/path with the pinned SDK and locked Release restore. B uses the
final candidate with identical build options. Exactly two of340 baseline inputs
differ: PaneReducer implementation/comment and PaneState XML comments. Domain DLLs
match; Application DLLs differ. One final package-free probe references A's
unchanged public API, and its identical DLL/deps/runtimeconfig is copied to both
sides. Only those three probe files are copied; A's Application is not copied over B.

The preparation script was invoked twice before collection because the review
clarified a null-focus XML comment. Both invocations also called probe build;
compiler invocation count is not established. Thus the literal plan phrase
"compiled once" is not asserted. The relevant controlled condition is the same
final probe binary for every block, with no rebuild or change during collection.

The frozen manifest contains1015 source/test/tool/configuration/probe/distribution/
plan/runner files. Each block verifies these hashes before starting; a final
verification passes after all blocks. run-campaign.py creates raw files exclusively,
refuses an already-attempted campaign and stops on a nonzero exit, malformed or
incomplete output, wrong outcome or changed input. Four distinct processes ran
A1/B1/B2/A2 in order at10:48:49–10:48:59 JST, each exiting0 with empty stderr.

Each block uses100/1000/10000 in-memory Windows entries, near-start then near-end.
Exactly two items remain selected; one-row inward/outward movements alternate1000
times per batch and finish at the original endpoint. Each scenario has one retained
but pre-excluded warmup, then five measured batches. All144 batches/120 measured
batches pass fixed checks for focus, both selected paths, shared visible list and
the unchanged starting snapshot. No replacement or outlier removal occurred.
Initialization durations are separately retained (24 records,0.759–173.754ms).

The timed boundary is PaneReducer.Apply plus loop and constant reference/count
checks. Allocation is current-thread allocated bytes across that loop. Values
below are each1000-call batch's mean per call; the range spans all five measured
batches in that block. Every warmup and measured tick/allocation is retained in
A1/B1/B2/A2.jsonl, analysis.json and all-batches.csv.

| Block | Rows | Position | ms/call min–max (5 batches) | allocated bytes/call min–max |
| --- | ---: | --- | ---: | ---: |
| A1 | 100 | near-start | 0.0012772–0.0016025 | 392–392 |
| A1 | 100 | near-end | 0.0197016–0.0529676 | 392–392 |
| A1 | 1000 | near-start | 0.0013803–0.0014313 | 392–392 |
| A1 | 1000 | near-end | 0.0511261–0.1142783 | 328–380.416 |
| A1 | 10000 | near-start | 0.0002792–0.0003300 | 328–328 |
| A1 | 10000 | near-end | 0.3559993–0.3906505 | 328–328 |
| B1 | 100 | near-start | 0.0005861–0.0008135 | 192–192 |
| B1 | 100 | near-end | 0.0045321–0.0417211 | 192–192 |
| B1 | 1000 | near-start | 0.0006603–0.0006769 | 192–192 |
| B1 | 1000 | near-end | 0.0050655–0.0457928 | 192–192 |
| B1 | 10000 | near-start | 0.0001685–0.0001903 | 192–192 |
| B1 | 10000 | near-end | 0.0794513–0.0864325 | 192–192 |
| B2 | 100 | near-start | 0.0007501–0.0007823 | 192–192 |
| B2 | 100 | near-end | 0.0033066–0.0296884 | 192–192 |
| B2 | 1000 | near-start | 0.0006138–0.0012269 | 192–192 |
| B2 | 1000 | near-end | 0.0049412–0.0468198 | 192–192 |
| B2 | 10000 | near-start | 0.0001739–0.0001856 | 192–192 |
| B2 | 10000 | near-end | 0.0782022–0.0885625 | 192–192 |
| A2 | 100 | near-start | 0.0013196–0.0013761 | 392–392 |
| A2 | 100 | near-end | 0.0189219–0.0469590 | 392–392 |
| A2 | 1000 | near-start | 0.0010607–0.0012079 | 392–392 |
| A2 | 1000 | near-end | 0.0379871–0.1267576 | 328–369.6 |
| A2 | 10000 | near-start | 0.0002208–0.0002323 | 328–328 |
| A2 | 10000 | near-end | 0.3774501–0.4153311 | 328–328 |

The primary10000-row near-end scenario spans0.3559993–0.4153311ms/call across
A blocks and0.0782022–0.0885625ms/call across B blocks. B allocates192bytes/call
in every measured batch; A ranges328–392bytes/call (batch means include intermediate
values). The bounded observation supports removing this redundant internal
comparison. It does not establish UI responsiveness, input/display latency,
filesystem performance, pure instruction cost, cross-provider timing or a
production guarantee. There is no percentile/significance claim. Default JIT/
tiering, GC, scheduling, caches and shared-host activity remain potential effects;
ABBA does not eliminate them. The differing early/late costs and all slow batches
remain visible. Prior Session/Presenter observations are a separate boundary and
are not pooled. Issue453, incomplete Issue491 native comparison and PR402 hide
screen acceptance remain separate.

## Attempts, corrections and evidence limits

- A setup read used a nonexistent PaneNavigationTests.cs name; subsequent reads
  selected actual classes. No product execution occurred. Literal wildcard reads
  were corrected to directory/glob searches.
- Initial whitespace exited2 because the new test used LF. CRLF correction is
  normalized-content-equivalent (eol-correction.json); final verification exits0.
  Successful behavior proof is reused after this EOL-only change and the later
  XML comment. No executable change followed those tests.
- Both preparation builds succeeded. The preparation script overwrote its initial
  successful build logs/command timestamps on the second pre-collection invocation.
  Initial observed build tails were zero-warning/error (candidate1.29s,probe3.94s),
  but exact first timestamps/binary hashes are not retained. Final build logs,
  baseline freeze and final campaign freeze are authoritative; no comparison ran
  against the earlier B distribution.
- First analysis created complete JSON/CSV/Markdown, then console output failed
  because cp932 could not encode an en dash. analyze-initial.py is preserved;
  console-only ASCII output corrected the failure and analysis exits0. No data
  collection was repeated. attempt-record.md retains these corrections.

Independent final read-only review at10:52:31 JST confirms all176 raw records,144
batches,24 initialization records and24 summary rows, plus all1015 frozen hashes.
No required correction; no outlier was removed. See final-review.md.

## Integration and cleanup

PR502 merged normally at10:57:06 JST as1aed7bad89806bb6b0c8f46e1417dda38222bd8b.
The squash tree exactly matches accepted6216db1bd4ae144bed84f628259bb2574b020b8a
(tree c762463312a25209235b025a26aeb3931f688e81). Main synchronized cleanly. The PR
had no additional CI checks under the selected ordinary scope; no bypass was used.

All725 generated/output files (192,725,213bytes) are preserved in task-artifacts.zip
with complete path/size/SHA256 verification. After merged-head/tree, clean tracked/
untracked state, reviewed ignored groups, archive/current file metadata, exact
absolute paths, no-links and no-live-reference checks, only the issue501 worktree
and output were removed at2026-11-20T10:57:57.5322690+09:00. Absence is confirmed in cleanup-result.json.
Branch, commits and permanent evidence remain. No release/settings operation or
cleanup of unfinished PR402/491 work occurred; the four older automatically
rejected cleanup targets were not retried. Closure documents reuse the successful
proof because executable inputs remain unchanged.
