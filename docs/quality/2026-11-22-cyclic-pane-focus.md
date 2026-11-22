# Cyclic file-list focus — Issue #524

Date: 2026-11-22
Status: implemented and locally verified; the closing PR/completion record owns integration and cleanup.

## Scope and invariant

hide requested file-list focus to cycle at the upper/lower edge after the beta.3
release. That release completed before this work. The base is accepted main
`2dbef98cf99efea28ca501a9cb0420c65a5842e4`.

`PaneReducer.Apply` remains the sole state transition. Only `MoveNext` and
`MovePrevious` wrap within `VisibleEntries`. Empty panes retain their snapshot;
one visible entry keeps its focus. The exact owned path, selected identities,
visible projection, history and passive pane remain intact. Paging and explicit
first/last targeting retain their existing behavior, including a one-row page
step. Locations, Settings and Bookmarks retain their separate declared contexts.

The existing mapper still maps j/Down and k/Up to those same intents. There is no
new command, view-specific movement, setting, dependency, provider operation or
trust boundary. CMD-002/003/009, KBD-004/005 and ARC-001/004 remain unchanged.
The command and keyboard documents now explicitly describe file-list cycling.

## Implementation and review

The reducer computes the cyclic index only for the two single-step intents.
The existing nonempty guard and owned focus-index invariant run first. For
1..10,000 visible entries, currentIndex+offset+count is nonnegative and at most
19,999, so modulo cannot divide by zero or overflow. All other movement uses
the existing clamp. Selection and projection references continue through the
same `PaneState.Transition`.

Two new test classes cover both directions, empty/all-hidden sets, one visible
entry, descending order with hidden entries at both ends, paging with capacities
1/4, a refreshed listing that removes the former endpoint, and both active sides.
Existing equal-state publication tests now request the explicit first target
rather than relying on an upward step stopping at the first row. The hidden-row
test and the selection ownership movement sequence adopt the requested cyclic
endpoints; their ownership and equality assertions remain.

This small deterministic change received direct source/diff review. No additional
agent, new ADR, full CI, all-layer coverage/mutation or native session was needed.
No shared desktop input was injected.

## Evidence and failures retained

Permanent evidence: `D:/NeNeCommander/evidence/cyclic-pane-focus-20261122`.
Worktree: `D:/NeNeCommander/worktrees/issue-524`.

1. On unchanged main product code, the ten new cases produce six causal focus
   assertion failures and four passes, zero skips, test exit2. The reducer SHA256
   stays `f80d0a1263faa8bb9c7fc500cd0a257ce37368b7592068ad9beaf00585425704`
   (SHA256). baseline-summary.json and baseline-tests.log
   retain the original failures.
2. After the reducer change, the directly affected Application scope runs291
   cases:289 pass and two existing parameter rows still expect edge stopping.
   The selection sequence is corrected from [1,0,2,0,2,2,0,0] to
   [1,0,2,0,2,0,0,2]. The full ownership class then passes12/12. Product code is
   unchanged by that correction; the other successful cases are reused. The
   distinct accepted Application set is291, not289+12.
3. Seven directly affected Presentation classes pass37/37, zero failed/skipped.
   They prove canonical aliases and focus/selection/visible-row projections.
   App and the selected test builds have zero warnings/errors.
4. The existing CS-013 inspection exits1 for exactly the same five pre-existing
   findings as the accepted beta.3 correction record. No changed file introduces
   a finding. PaneReducer.Apply measures22 logical lines,2 parameters,cognitive10,
   nesting2. The five findings are not suppressed or baseline-exempted.
5. The first whitespace check detects LF in two inserted product lines. The six
   changed C# files are normalized to the existing CRLF convention. Every file's
   newline-normalized text remains identical, with before/after hashes recorded.
   Both final changed-file formatting checks succeed; no behavioral test is
   repeated for this newline-only correction.

Application scope is recorded as the exact30-class filter in application-scope.json,
selected by direct use of PaneReducer or the changed single-step intents. This
includes selection/operation consumers and modal ownership boundaries; it is not
an unfiltered Application suite. Presentation-scope.json records the seven-class
filter. Success is reused only for unchanged relevant code and test bodies.

## Commands

Run from the issue524 worktree. Logs and exit results are preserved under the
evidence directory; `$project`, `$scope.filter` and `$filter` below are the exact
values recorded by the corresponding scope JSON. Final restore/build/format commands
exit0. Baseline tests exit2; the first291-case run exits2; selected final runs exit0.

```powershell
dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-restore -maxcpucount:4
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~PaneFocusCycle'
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter $scope.filter
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -c Release --no-build --no-restore -- --filter 'FullyQualifiedName~PaneSelectionOwnershipTests'
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-restore -maxcpucount:4
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj -c Release --no-build --no-restore -- --filter $filter
dotnet restore src/NeNeCommander.App/NeNeCommander.App.csproj --locked-mode --disable-parallel -p:Configuration=Release
dotnet build src/NeNeCommander.App/NeNeCommander.App.csproj -c Release --no-restore -maxcpucount:4
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
dotnet format whitespace src/NeNeCommander.Application/NeNeCommander.Application.csproj --no-restore --verify-no-changes --include src/NeNeCommander.Application/Panes/PaneReducer.cs
dotnet format whitespace tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --no-restore --verify-no-changes --include tests/NeNeCommander.Application.Tests/PaneReducerTests.cs tests/NeNeCommander.Application.Tests/PaneSelectionOwnershipTests.cs tests/NeNeCommander.Application.Tests/PaneSelectionSessionTests.cs tests/NeNeCommander.Application.Tests/PaneFocusCycleTests.cs tests/NeNeCommander.Application.Tests/PaneFocusCycleSessionTests.cs
```

The final commit uses the usual `eng/check.ps1 -Mode Commit`. No new release or
native key-routing/latency proof is claimed. This change does not resolve #523,
#550, #453/#491 or AppSDK PR517. The beta.3 tag/assets and current user App/settings
remain separate. Preserve branches/commits and evidence; archive unique generated
outputs before removing the dedicated task worktree after accepted integration.
