# Windows local operation responsibility refactor — 2026-10-18

Issue #338 continues the provider-boundary priority authorized by hide.

## Result and invariant

WindowsLocalFileOperationAdapter remains the sole public local port and keeps its
constructors, schedulers, recycle owner, guards and shared revalidation/normalizer.
Its logical size falls 408→170. WindowsLocalOperationSteps (169 lines) owns the
same 17 local operation/capability/verification bodies under that scheduler.
WindowsLocalTransferPreflight (91) owns one batch's case-insensitive reservations,
plan and conflicts, separating destination/source admission from candidate names.
No state survives a preflight call. Source/drive/link/containment checks, exact
skip/keep-both choices, candidate replacement and source order are preserved.

The existing destination Exists fact now owns the former string-path existence
check as well as its typed entry point. Tree-copy admission changes from nested
conditionals to one ordered guarded switch, retaining short-circuit evaluation,
exclusive creation and partial effects. No identity query, native import, provider
route, source deletion mechanism, public contract, dependency or trust rule changes.
The 644-line adapter test fixture becomes seven responsibility classes plus one
shared assertion/snapshot context; its 40 methods and all attributes remain.

## Verification — ADR-0061

SDK 10.0.401; TEMP/TMP is D:/NeNeCommander/outputs/issue-338/tmp.
Commands run from D:/NeNeCommander/worktrees/issue-338:

```powershell
dotnet restore tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --locked-mode -p:Configuration=Release
dotnet format style tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --no-restore --include <changed/new C#> --diagnostics IDE0005 --verbosity minimal
dotnet format whitespace tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --no-restore --include <changed/new C#> --verbosity minimal
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsLocalInspectionTests|FullyQualifiedName~WindowsLocalPreflightTests|FullyQualifiedName~WindowsLocalNameEntryTests|FullyQualifiedName~WindowsLocalCopyTests|FullyQualifiedName~WindowsLocalMoveTests|FullyQualifiedName~WindowsLocalDeletionTests|FullyQualifiedName~WindowsLocalOperationContractTests|FullyQualifiedName~WindowsTreeAdmissionTests|FullyQualifiedName~MappedDriveLocalMutationBoundaryTests|FullyQualifiedName~TransferConflictWindowsTests|FullyQualifiedName~VerifiedAdapterBoundaryTests|FullyQualifiedName~VerifiedCopyAdmissionBoundaryTests|FullyQualifiedName~VerifiedMoveEvidenceTests|FullyQualifiedName~VerifiedMoveSourceRaceTests|FullyQualifiedName~VerifiedVerificationBoundaryTests|FullyQualifiedName~VerifiedSourceDeletionBoundaryTests|FullyQualifiedName~WindowsToWslTransfer|FullyQualifiedName~WslToWindowsTransfer|FullyQualifiedName~ProviderFileOperationPortTests'
python -X utf8 D:/NeNeCommander/evidence/windows-operation-refactor-20261018/Verify-WindowsMigration.py
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
```

Release build: zero warnings/errors. All 190 selected cases pass, zero failures
or skips. Cases exercise owned-root local I/O, collisions/reservations, identity
races, descendant/drive admission, partial targets, verified copy/source deletion
and direct cross-provider/router consumers. WSL paths use existing deterministic
seams; no new live distribution, Shell recycle or desktop observation is made.

Structural proof compares all 40 test bodies and attributes, four helper bodies,
17 moved operation bodies and 15 adapter/scheduler/guard bodies after explicit
qualification changes. Their statement order and string literals are retained.
The split preflight is separately reviewed against the original ordered admission,
ordinary/skip/keep-both and replacement branches and the collision tests. Changed
C# formatting passes. Initial builds exposed a missing moved namespace import,
IDE0046 on a return-only branch, and a duplicated test class header; all were
corrected before the final successful build and tests. No diagnostic is suppressed.

The unchanged checker reports 803 sources, 1,242 types, 7,949 callables and 7,316
unresolved calls; all source hashes match. Exit 1 reports 104 remaining findings:
29 product, 75 tests, zero engineering. Method/cognitive/nesting/parameter/type
counts are 39/18/9/16/22. All changed/new declarations meet all five ceilings.
The previous checker fixture/launcher proof is reused, with no tool change.

Normal Commit checks remain enabled. Exact moved-body proof plus the affected
operation/collision/race/consumer cases establish this internal responsibility
refactor. Public contracts and trust predicates are unchanged, with no unresolved
wider impact requiring full/deep CI. No unrelated regression, coverage/mutation,
live provider/release #94 tier, waiver, baseline or gate change is selected.

## Integration and remaining priority

Evidence is under D:/NeNeCommander/evidence/windows-operation-refactor-20261018/.
The Issue PR and completion.json own squash integration, clean synchronized main
and checked task worktree/output cleanup. Retain branches/commits and proof.
Continue remaining WSL and cross-provider findings, settings persistence, remaining
tests/flow and global checker adoption after all findings are resolved.
