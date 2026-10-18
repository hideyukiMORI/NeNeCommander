# Transfer contract and gateway refactor — 2026-10-18

Issue #334; the second priority in hide's 2026-10-18 remaining-work authorization.

## Result and invariant

FileOperationGateway remains the only public mutation route and keeps its one
SemaphoreSlim lease, continuation issuing-owner object, cancellation ordering and
single-use consumption. A foreign/busy refusal does not consume the continuation;
cancel/replay behavior is unchanged. Its logical size falls 442→210.

The internal TransferExecution (162 lines) owns one admitted frozen attempt's
preflight, capability discovery, ordered copy/verify/delete or atomic move,
progress, effects and skipped sources. TransferPreflightValidation (67 lines)
retains the six exact gateway predicates. These components have no independent
lease or externally callable mutation entry point. Delete admission and one-source
execution are separated; admission cognitive complexity falls 20→3, while
confirmed recycle receipts still survive later failure/cancellation.

FrozenTransfer owns the original request/source snapshots/destination, and new
continuations share that immutable value rather than recopying its list at each
pause. Source entry identity and issuing-token identity retain their contracts.
FileEntryTransfer groups target/choice annotations; FileOperationEffectSummary
owns effects/skips, and TransferConflictPause pairs conflicts with their token.
Public snapshot/outcome projections are preserved.

The one public API migration is deliberate and complete:

```csharp
await gateway.ResumeAsync(
    continuation,
    TransferConflictResponse.Create(decision, scope),
    progress,
    cancellationToken);
```

The obsolete five-argument overload is removed. DualPaneSession and all test and
Windows integration callers use the four-argument contract. The response factory
requires both existing closed values; it supplies no default and grants no token
ownership. No provider interface/implementation, request policy, confirmation,
capability, dependency, UI design or filesystem trust boundary changes.

## Focused proof — ADR-0061

All commands use SDK 10.0.401 and task-local D-drive TEMP/TMP, from
`D:/NeNeCommander/worktrees/issue-334`:

```powershell
dotnet restore tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --locked-mode -p:Configuration=Release
dotnet restore tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --locked-mode -p:Configuration=Release
dotnet restore tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --locked-mode -p:Configuration=Release
dotnet restore tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --locked-mode -p:Configuration=Release
dotnet build tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~Gateway|FullyQualifiedName~Transfer|FullyQualifiedName~DualPane|FullyQualifiedName~VerifiedCopyContractTests|FullyQualifiedName~FileOperationRequestTests|FullyQualifiedName~CreateDirectoryRequestTests|FullyQualifiedName~RenameRequestTests|FullyQualifiedName~FileOperationProgressTests|FullyQualifiedName~NullGuardTests.InvokeWhenRequiredArgumentIsNull|FullyQualifiedName~NullGuardTests.DualPaneSessionWhenRequiredArgumentIsNull|FullyQualifiedName~NullGuardTests.ExecuteAsyncWhenRequestIsNull'
dotnet build tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-restore --disable-build-servers -p:BuildProjectReferences=false
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~TransferConflictWindowsTests|FullyQualifiedName~WindowsToWslTransferGatewayTests|FullyQualifiedName~WslToWindowsTransferGatewayTests|FullyQualifiedName~WslTrailingDotGatewayTests|FullyQualifiedName~VerifiedMoveEvidenceTests|FullyQualifiedName~VerifiedMoveSourceRaceTests|FullyQualifiedName~ProviderFileOperationPortTests'
dotnet build tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-restore --disable-build-servers -p:BuildProjectReferences=false
dotnet test --project tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~DualPaneTransferPresenterTests|FullyQualifiedName~DualPaneConflictPresenterTests|FullyQualifiedName~DualPaneDeletionPresenterTests|FullyQualifiedName~DualPaneProgressPresenterTests|FullyQualifiedName~DualPaneNameEntryPresenterTests|FullyQualifiedName~DualPanePresentationTests'
dotnet test --project tests/NeNeCommander.Architecture.Tests/NeNeCommander.Architecture.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~ProjectDependencyTests'
python -X utf8 D:/NeNeCommander/evidence/transfer-contract-refactor-20261018/Verify-TransferMigration.py
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
```

The four affected test projects and App build successfully with zero warnings and
errors. Architecture first builds all production dependencies, so the two later
test-assembly builds reuse those verified outputs. Results: Application 174,
Windows boundary 72, Presentation 28 and dependency graph 1; all 275 pass, with no
failure/skip. Windows filesystem cases stay in test-owned local roots; WSL routes
use existing deterministic ports. No new live WSL/network/desktop observation.

The initial two Application build attempts exposed IDE0046 as deletion branches
became a return-only chain. A flat guarded switch fixes the cause without changing
order or suppressing diagnostics; final builds and tests above follow that fix.

All 109 old test methods move once with names/attributes/assertions retained.
The migration proof compares 105 normalized method bodies after exact fixture,
API and named assertion extractions. It separately compares all eight hostile
unresolved-plan and six resolved-plan cases in order; the two remaining null-guard
cases migrate to the grouped factories and add absent-response/frozen-value guards.
Three new tests prove response payload/copy behavior, snapshot annotation equality
and read-only outcome collection copies. Eighteen gateway/predicate/step bodies
are unchanged after field qualification, and owner/Interlocked consumption bodies
are identical. The remaining orchestrator/deletion splits are reviewed against
cancellation, effect, progress and hostile provider tests.

Changed-C# formatting uses `dotnet format style NeNeCommander.slnx --no-restore
--include <changed C#> --diagnostics IDE0005 --verbosity minimal` and
`dotnet format whitespace NeNeCommander.slnx --no-restore --include <changed C#>
--verbosity minimal`. Both exit 0 with the recorded workspace-load warning.
Every nonblank line is identical before/after whitespace formatting apart from
leading/trailing whitespace, so the compiled behavior evidence remains applicable.
Normal Commit policy/security/message checks remain enabled.

The final cs013-v1 report has 794 source hashes, 1,233 types, 7,957 callables and
7,314 unresolved invocations. It returns 1 for 118 remaining findings: 42 product,
76 tests, zero engineering. Method/cognitive/nesting/parameter/type findings are
40/20/10/24/24. All changed/new declarations have zero findings and every source
hash matches. The unchanged #324 checker fixture/launcher proof is reused.

Focused evidence establishes this shared-contract refactor: all resume callers
compile, every affected orchestration boundary is exercised, exact provider-result
and effect-bearing predicates are retained, and the provider port/adapters are
unchanged. No unresolved wider trust-boundary effect justifies full/deep CI or
all-layer mutation here. No broad regression, coverage or release #94 tier is
selected or claimed.

## Integration and remaining priority

Permanent evidence is under
`D:/NeNeCommander/evidence/transfer-contract-refactor-20261018/`.
The Issue PR and completion record own squash integration, synchronized clean main
and checked removal of the completed task worktree/output. Keep branches, commits
and permanent proof. Continue Windows/WSL provider boundaries, settings attempt
ownership, remaining tests/flow, then global checker adoption after all findings
are resolved. Release #94 remains separate hide-controlled environmental work.
