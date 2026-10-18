# Recycle callback policy refactor — 2026-10-18

Issue #336 is the first provider-boundary part of hide's third remaining-work priority.

## Result and invariant

The eight unsupported native rename/move/copy/new callbacks now call one managed
`RejectUnrequestedMutation` policy. It records the same first ProviderUnavailable
failure and abort code. IWindowsRecycleSink no longer mirrors arguments that the
policy never consumes; no parameter bag or native-signature exception is added.
INativeWindowsRecycleSink is unchanged, and every generated COM implementation
signature remains exact. Delete admission/completion bodies, cancellation,
receipt ownership/release and the one gateway deletion route are unchanged.

The existing eight callback cases retain their inputs and assertions. Each also
verifies that an ensuing pre-delete stays vetoed and no effect is reported. The
existing managed refusal test uses the new policy. No test identity is removed.

## Verification — ADR-0061

SDK 10.0.401; task-local TEMP/TMP under D:/NeNeCommander/outputs/issue-336/tmp.
Commands run from D:/NeNeCommander/worktrees/issue-336:

```powershell
dotnet restore tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --locked-mode -p:Configuration=Release
dotnet build tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-restore --disable-build-servers
dotnet test --project tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~WindowsRecycle&FullyQualifiedName!~Live'
dotnet format whitespace tests/NeNeCommander.Infrastructure.Windows.Tests/NeNeCommander.Infrastructure.Windows.Tests.csproj --no-restore --include <four changed C# files> --verify-no-changes --verbosity minimal
python -X utf8 D:/NeNeCommander/evidence/recycle-callback-refactor-20261018/Verify-Callbacks.py
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
```

The final Release build has zero warnings/errors; all 82 selected cases pass,
with zero failures/skips. These cover deterministic Shell callbacks, admission,
failure/effect handling, native wrappers and ownership. A preliminary successful
build preceded a test-only fixture correction to reuse its one source snapshot;
the final build and all tests follow that correction. Formatting passes.

Structural proof confirms unchanged native interface/signatures and all four
native/managed pre/post-delete bodies, all 794 source hashes and zero findings in
changed C#. Initial proof-script assertions were corrected for JSON hash casing
and Git/working-tree newline representation; product code was unaffected.
The unchanged cs013-v1 tool reports 110 remaining findings (34 product, 76 tests,
zero engineering), with 1,233 types, 7,943 callables and 7,316 unresolved calls.
Its exit 1 correctly denotes those remaining findings. Metric totals for
method/cognitive/nesting/parameters/type are 40/20/10/16/24. The #324 checker
fixtures remain applicable because tool/metric inputs are unchanged.

Normal Commit policy/security/message checks remain enabled. Focused proof is
sufficient: the internal policy drops unused arguments while its exact refusal
and every native callback route remain covered; no new trust boundary is added.
No full/deep CI, broad regression, coverage/mutation, live recycle/WSL/desktop or
release #94 tier is selected or claimed. Dependencies, thresholds and gate wiring
are unchanged. No waiver is introduced.

## Completion and next work

Evidence is under D:/NeNeCommander/evidence/recycle-callback-refactor-20261018/.
The Issue PR and completion.json own squash integration, clean synchronized main
and checked task-copy cleanup. Keep branches, commits and permanent evidence.
Continue Windows/WSL preflight and execution, then settings, remaining tests/flow
and global checker adoption after every in-scope finding is resolved.
