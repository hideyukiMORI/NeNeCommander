# Bounded volume GUID buffer — 2026-11-22

Issue #512. Base: `e2f675b63f492cc49b8e190112adcc7944a56da0`.

## Invariant and canonical mechanism

`WindowsLocalVolumeIdentity.ResolveVolumeName` still queries the current mounted
volume for each source and destination. Only its volume GUID output array changes
from32,768 to50 UTF-16 characters. The independent mount-path array stays32,768.
Both generated P/Invoke signatures, length arguments, ReadBuffer, ordinal-ignore-case
comparison, query order and IOException/Win32 error path are unchanged. No cache,
pool, new operation path, admission rule or dependency is introduced.

Microsoft documents50 characters for the largest volume GUID path:
[GetVolumeNameForVolumeMountPointW](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getvolumenameforvolumemountpointw).
The existing49-character GUID path plus null fits. The prior read-only ctypes
probe is retained at D:/NeNeCommander/evidence/volume-name-buffer-20261121:
32,768 and50 returned the same hashed49-character name;49 failed with206;
a non-mount folder failed with4390; all four bounds sentinels stayed unchanged.
That probe is native API evidence, not changed-product marshaling proof.

The new proof below runs the actual changed .NET marshaler through the existing
adapter and gateway. This allocation-only change intentionally preserves behavior;
existing success/refusal tests are reused instead of adding a test of a private
constant. No behavioral red test or measured allocation benchmark is claimed.
The character-array payload arithmetic is65,436 bytes less per GUID-name array;
array headers, alignment, native marshaling, total process allocation and speed
were not measured. Neither #453 nor the stopped #491 comparison is resolved.

Rules: ARC-001/005, CMD-001/004, CS-018/024/028, SEC-003, QLT-014/015,
TST-003 and ADR-0061. No ADR, waiver, gate or threshold change.

## Selected proof and exact commands

Commands ran in D:/NeNeCommander/worktrees/issue-512. The existing pinned SDK
and package locks were used. Before tests, TEMP and TMP pointed to the task-owned
D:/NeNeCommander/outputs/volume-guid-buffer-20261122/temp directory.

```powershell
dotnet restore tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -p:Configuration=Release --locked-mode
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-restore
$filter = 'FullyQualifiedName~WindowsLocalVolumeIdentityTests|FullyQualifiedName~WindowsLocalMoveTests|FullyQualifiedName~WindowsLocalStepAdmissionTests.MoveWhenOnlyTargetDriveIsRejectedFailsWithoutMoving|FullyQualifiedName~MappedDriveLocalMutationBoundaryTests.DirectAdapterWhenDriveRemapsAfterSnapshotRejectsEveryLocalStepWithoutEffect'
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj -c Release --no-build --no-restore --filter $filter --report-trx --results-directory D:/NeNeCommander/outputs/volume-guid-buffer-20261122/results
dotnet format whitespace src/NeNeCommander.Infrastructure.Windows/NeNeCommander.Infrastructure.Windows.csproj --verify-no-changes --no-restore --include src/NeNeCommander.Infrastructure.Windows/FileOperations/WindowsLocalVolumeIdentity.cs
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
```

Restore/build/selected tests/format exited0. The affected Infrastructure and test
builds, including their Application/Domain references, report zero warnings/errors.
All10 tests pass with zero skips: one buffer-read contract, seven native move
cases, one effect-time target-drive rejection and one snapshot-to-remapped-drive
rejection. They prove file/directory and two-file gateway moves with contents,
source disappearance and typed atomic effects, plus missing destination, source
identity change, late collision and source/destination reparse refusal. The test
root harness completed cleanup; no owned test-root residue remains.

The first complexity invocation incorrectly supplied unsupported `-OutputPath`;
it stopped at argument binding with exit1 and its log is retained. The corrected
command exited1 with the same26 existing findings, compared structurally against
the accepted #510 result. No finding was added or suppressed. This is the existing
standalone inspection, not a passing full gate. Product tests were not repeated.

Ordinary focused scope is sufficient: one private allocation size changes, while
both callers and all trust decisions stay byte-equivalent outside that change.
No full CI, coverage, mutation, native App, shared input or release proof ran.
Cross-volume/removable/mounted-folder topology and isolated native UI remain
unexecuted environmental proof; the contract is unchanged.

## Review and preservation

Self-review verified the product diff equals exactly one private constant, its
reason comment and one allocation use. The affected test results and all1,017
source/test/build/tooling input hashes are recorded. All399 generated/output
files,129,170,742 bytes, are archived with full path/size/SHA256 verification at
D:/NeNeCommander/evidence/volume-guid-buffer-20261122/task-artifacts.zip.
The permanent directory retains the verification script, summary and manifest;
the archive also retains both failed and corrected complexity logs. Ordinary
commit hooks remain required. The closing PR and completion record own actual
integration and task-only worktree/output cleanup.
