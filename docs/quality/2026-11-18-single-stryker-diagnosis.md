# Single-mutant Stryker diagnosis (Issue #429)

The pinned Stryker/VSTest path reports the `PaneReducer.GetFocusIndex`
`First()` to `FirstOrDefault()` mutant as **Survived**, both with the two recorded
CI killer IDs and with all 31 recorded covered IDs. The historical CI **Killed**
cause remains unestablished. This follows the source-only diagnosis in
[Issue #424](2026-11-18-pane-mutant-diagnosis.md).

## Fixed inputs and scope

The diagnostic worktree used `b5d9af8dcf0c04622be517aaa3ce6bf0390e8b3e` so that
all 424 source, test, dependency and tool hashes match #424. Current-main fixture
moves were absent from the experiment. No permanent implementation, test,
dependency, configuration, threshold or exclusion changed.

The span filter selects `**/PaneReducer.cs{12475..12537}`. The run-local active
mutant ID is 1813; the earlier local report used 1816. Location and replacement,
not the ordinal ID, establish that it is the same mutant. Stryker internally
generated 2815 instrumented mutants, rolled back 400 compilation errors and
skipped 2414 outside the diagnostic span. Only the target mutant ran. This is
not a whole-layer mutation run or a measurement of its score.

## Commands and observed outcomes

| Command | Outcome |
| --- | --- |
| `dotnet --info` | exit 0; environment recorded |
| `dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj -p:Configuration=Release --locked-mode` | exit 0 |
| `dotnet tool restore` | exit 0; pinned packages retained |
| `dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --configuration Release --no-restore` | exit 0 |
| From the Application test directory: `dotnet stryker --config-file ../../stryker-config.json --project NeNeCommander.Application.csproj --break-at 95 --skip-version-check --mutate **/PaneReducer.cs{12475..12537} --diag --log-to-file --test-case-filter <two exact IDs> --output <owned output>` | target Survived; exit 2 |
| Same command with the 31 exact IDs and a separate owned output | target Survived; exit 2 |

The initial, coverage and active-mutant phases each ran exactly the selected ID
set: 2/2 passed in all three phases of the first run, and 31/31 passed in all three
phases of the second. No selected test failed, skipped, aborted or was cancelled.
Each run retained 15 identical injected-source snapshots, including the active-ID
branch selecting `FirstOrDefault()`, and individual results with failure/stack fields.

Exit 2 is the unchanged 95% break threshold applied to one surviving mutant (0/1
killed), not a passing mutation gate or a runner crash. The first outer PowerShell
invocation reported exit 1; its child exit 2 is retained separately. The second
wrapper propagated exit 2. VSTest cache and missing data-collection trace warnings
are retained; they are not established as the historical CI failure's cause.

After Stryker completed, all 424 input hashes and both original assembly hashes
matched, and the detached worktree was clean. Only then was that worktree switched
to current main for this documentation-only integration.

## Evidence, limits and next condition

The permanent root is `D:/NeNeCommander/evidence/issue-429/`. `summary.json` owns
the interpreted result; `commands.json` and `commands-result-summary.json` contain
every exact argument, directory, time and exit. `stryker-two-verification.json`
and `stryker-31-verification.json` identify the selected/executed IDs and individual
outcomes. Raw reports, injection snapshots and trace logs are preserved with a
74-file SHA256 manifest. Integration and cleanup are recorded in `completion.json`.

The original CI batch composition, preceding/following mutants and execution
history were not reproduced. The experiment did not run in CI. The result agrees
with the normal-state source argument for equivalence; it does not prove equivalence
for every possible input or identify a CI scheduler/environment defect. Previous
CI mutation scores remain unchanged.

The next minimal comparison is one CI run using these fixed inputs, this one
mutant and the same 31 IDs, preserving the same trace evidence. A difference must
be investigated through its first individual failure, injected source and active
ID. No difference would call for the original batch history, not an unsupported
conclusion. This diagnosis alone is not a reason for a whole-layer rerun. No full
gate, GUI, live WSL or real-data operation was selected.

Rules: TST-008/012, QLT-015, GIT-001–004, ADR-0048/0061. Waivers: none.
