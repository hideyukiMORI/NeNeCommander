# CS-013 maintained measurement — 2026-10-18

Status: measured findings; standalone tool, not a global gate.

## Progress after the initial census

The initial table below is the #324 snapshot. Later focused changes keep the same
metric definition and record their own input hashes and complete findings.

| Work | Product findings | Test findings | Engineering findings | Total |
|---|---:|---:|---:|---:|
| #324 initial measurement | 77 | 103 | 0 | 180 |
| #326 palette presentation and matching fixtures | 71 | 102 | 0 | 173 |
| #328 pane presentation and matching fixtures | 66 | 93 | 0 | 159 |
| #330 App view composition | 60 | 93 | 0 | 153 |
| #332 remaining Presentation models and input | 55 | 86 | 0 | 141 |
| #334 shared transfer contracts and gateway execution | 42 | 76 | 0 | 118 |
| #336 managed recycle callback policy | 34 | 76 | 0 | 110 |
| #338 Windows local operation responsibilities and fixtures | 29 | 75 | 0 | 104 |
| #340 WSL provider observations, preflight and affected fixtures | 20 | 60 | 0 | 80 |

Issue #340 removes nine WSL/cross-provider product findings and fifteen affected test findings. All changed/new C# conforms. Its [daily report](../reports/2026-10-19-cs013-priority-daily-report.md) records 273 successful cases, three explicitly unexecuted Live WSL cases, structural proof and the checkpoint after priorities 1–3. The census is 821 sources, 1,260 types, 7,993 callables and 7,347 unresolved invocations; all source hashes match. Method/cognitive/nesting/parameter/type totals are 32/11/5/13/19. Settings, remaining tests/flow and global enforcement remain next.

Issue #338 removes five local-provider findings and the direct fixture type finding.
All changed/new C# conforms; 190 focused cases and exact moved-body proof pass.
Its [report](../reports/2026-10-18-windows-operation-refactor-daily-report.md) records
the 170/169/91-line scheduling/step/preflight owners and remaining priorities.

Issue #336 removes the eight managed callback findings while preserving every native
signature. All changed C# conforms and 82 focused cases pass; its
[report](../reports/2026-10-18-recycle-callback-refactor-daily-report.md) retains
structural proof, commands and the remaining provider/settings/fixture work.

Issue #326 removes all six palette product findings and the large palette test
type finding. Every changed/new declaration is within all five ceilings. Its
[report](../reports/2026-10-18-palette-refactor-daily-report.md) retains proof and
remaining work; the global gate stays disconnected.

Issue #328 removes five pane product findings and nine fixture findings. All
changed/new declarations meet the five ceilings. Its [report](../reports/2026-10-18-pane-refactor-daily-report.md)
records the 74 focused cases, compatible public bindings and retained row/modal
identities. The next ordered step is App view composition.

Issue #330 completes that ordered App step, removing six product findings while
all changed/new C# meets the five ceilings. Its [report](../reports/2026-10-18-app-view-refactor-daily-report.md)
records focus/event/lifecycle compatibility, 65 focused cases and reuse of the
unchanged palette/pane evidence. The remaining 153 findings and global gate
integration remain separate work; the initial measurement is retained below.

Issue #332 removes the five remaining Presentation product findings and seven
related test findings. Map has 12 logical lines, cognitive complexity 6 and nesting
1; the four model constructors each take four arguments. All changed/new C#
meets the ceilings. Its [report](../reports/2026-10-18-presentation-input-refactor-daily-report.md)
records 114 successful focused cases and unchanged public binding/key-table proof.
The current census is 765 sources, 1,205 types and 7,917 callables, with 7,298
unresolved invocations under the unchanged static model. Metric totals for
method/cognitive/nesting/parameters/type are 48/22/10/33/28. The next priority is
shared file-operation contracts, followed by providers, settings, remaining
fixtures/flow and global enforcement after every finding is resolved.

Issue #334 removes 13 product and ten related test findings. FileOperationGateway
is 210 logical lines, its internal transfer attempt 162 and its result validator
67; deletion admission has cognitive complexity 3. All changed/new C# meets the
five ceilings. Its [report](../reports/2026-10-18-transfer-contract-refactor-daily-report.md)
records 275 successful focused cases, the fully migrated resume signature and
retained lease/continuation/provider-step invariants. The current census contains
794 sources, 1,233 types and 7,957 callables, with 7,314 unresolved invocations.
Method/cognitive/nesting/parameter/type totals are 40/20/10/24/24. Provider and
settings responsibility splits precede remaining tests/flow and global adoption.

## Initial census

Issue [#324](https://github.com/hideyukiMORI/NeNeCommander/issues/324) implements
[ADR-0075](../adr/0075-standalone-cs013-measurement.md). The product/test baseline is
`85a6a9956ae37ae1fb0437542d0eaf6b1e556e74`; the measurement also includes this
Issue's engineering sources. The report records all 722 source hashes and Roslyn
5.9.0.0 from SDK 10.0.401. The metric version is `cs013-v1`.

## Result

`Inspect` correctly returns **1**, status `findings`. The repository is not yet
CS-013 conformant. A finding is one exceeded metric at one declaration; the same
declaration may contribute more than one finding.

| Scope | C# files | Types | Measured callables | Method lines >40 | Cognitive >10 | Nesting >3 | Owned parameters >4 | Type lines >300 | Findings |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Product (`src`) | 502 | 894 | 3,747 | 7 | 22 | 14 | 29 | 5 | 77 |
| Tests (`tests`) | 192 | 240 | 3,748 | 52 | 4 | 2 | 15 | 30 | 103 |
| Engineering (`eng`) | 28 | 28 | 309 | 0 | 0 | 0 | 0 | 0 | 0 |
| Total | 722 | 1,162 | 7,804 | 59 | 26 | 16 | 44 | 35 | 180 |

The 13 externally fixed native signatures above four parameters remain in the
member report with raw counts and semantic classification. They do not produce
an owned-parameter finding. Eight managed mirrors remain among the 29 product
parameter findings. No whole file/type is exempt, and no finding is suppressed.

The report counts 7,222 unresolved invocation expressions. This source-only model
uses framework/SDK references and transitive source-project references; it does
not restore product packages, run generators or build WinUI/MSTest projects.
Resolved source targets and ambiguous source candidates contribute cycle edges;
unknown calls do not. The cognitive result follows that explicit static definition
and is not a complete runtime call graph or compiler-success claim. Line, syntax
flow, nesting and explicit-parameter counts do not depend on successful product
compilation. Unsupported input fails with exit 2 rather than being omitted.

## New information for the adoption order

The [earlier plan](cs013-readiness-2026-10-18.md) remains the responsibility-specific
sequence. Its metric/native-policy/tool step is now complete. Its historical
member counts excluded anonymous functions and used provisional control depth;
do not compare those figures as if the definitions were unchanged.

| Product member | Cognitive | Nesting | Logical lines | Owning future work |
|---|---:|---:|---:|---|
| `WindowsLocalSettingsStore.WriteDocument` | 34 | 2 | 200 | Settings attempt ownership, publication and cleanup |
| `WindowsLocalFileOperationAdapter.Preflight` | 31 | 3 | 60 | Local provider admission |
| `CommandPalettePresenter.Reason` | 22 | 6 | 18 | Presentation reason projection |
| `FileOperationGateway.ExecuteDeleteAsync` | 20 | 3 | 32 | Shared deletion orchestration |
| `WslToWindowsTransfer.Preflight` | 18 | 3 | 23 | WSL transfer admission |
| `KeyboardIntentMapper.Map` | 17 | 2 | 38 | Deterministic input mapping |

Full locations, values, types, members and native reasons are retained in the
machine report. The source hashes identify the exact inputs. Resolve findings
through the planned responsibility splits and their directly affected tests;
do not raise thresholds, introduce a baseline or wire a changed-files-only gate.
Global integration is a subsequent explicitly scoped change.

## Reproduction and proof

Use the pinned SDK, then run from the repository:

```powershell
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode SelfTest
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
dotnet format whitespace ./eng/complexity --folder --verify-no-changes --verbosity minimal
```

The first command compiles only the tool with warnings as errors and passes 24
fixture groups. These cover all threshold edges, multiline/token handling,
partial/nested/file-local identities, logical operator runs, flow weights,
direct/indirect/generic/partial recursion, transitive references, valid and spoofed
native shapes, untracked/ignored sources, and refused unsupported/missing inputs.
The second returns 1 with the table above. Formatting returns 0.

The retained `Prove-Launcher.ps1` exercises seven external entry-point cases:
conformant input; invalid source; empty project; missing project; compiler failure;
missing repository root; and a real source junction. Every invalid run returns 2
for its intended reason. Existing successful JSON is replaced; a compile failure
cannot execute an old binary. The missing root is not created. The owned junction
is removed without changing its target. These proofs all pass.

Evidence is retained at `D:/NeNeCommander/evidence/cs013-tool-20261018/`: final
inspection/self-test JSON and logs, launcher proof/script/logs, input manifest and
tool hashes. The Issue PR and completion record own integration and cleanup facts.
ADR-0061 selects these tool proofs and normal Commit checks. No product build,
product test suite, full/deep CI, coverage, all-layer mutation or live environment
proof is selected. Release confirmation #94 is unchanged and remains separate.
