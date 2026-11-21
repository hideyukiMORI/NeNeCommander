# Visible pane entry ownership — 2026-11-21

Issue: #508. Base: `82b9ed18612b98d4e7b27ae43caadacd56601730`.

## Contract and change

`PaneState.SelectVisible` remains the only visible-sequence construction path.
Its former `List<DirectoryEntry>.AsReadOnly()` result exposed the backing List
through non-generic `ICollection.SyncRoot`. Clearing that object changed published
pane content, copied states and later movement without a reducer transition.
Four unchanged-product regression cases establish this behavior; this is an
in-process ownership defect, not a demonstrated external filesystem exploit.

A private `VisibleEntrySnapshot` now materializes the same filtered enumeration
once into its own List. The filter, ordering, exact entry/path references, focus,
selection, navigation history and state publication routes are unchanged.
`IReadOnlyList<DirectoryEntry>` stays the declared public surface. Generic
`ICollection<DirectoryEntry>` retains Count, Contains and CopyTo and rejects all
mutators; both enumerator routes remain. The backing List never leaves the owner.

The old concrete ReadOnlyCollection and non-generic ICollection/IList casts, and
the generic IList cast, are no longer supported. These were outside the declared
API. A tracked-source search found no product or existing test consumer relying
on those casts. This does not claim all other collection snapshots are audited.
No dependency, schema, provider, command, UI design, gate or waiver changes.

Rules: ARC-001/004, CMD-002, CS-007/009/012/013/015, QLT-005/014/015,
TST-002 and ADR-0061. Focused ordinary scope covers pane construction/reduction,
ordering, navigation, selection, operation-source consumers and retained row
projection. No changed external trust-boundary decision requires full/deep CI.
No native/UI performance, release or general speedup result is claimed.

## Selected proof and failures

- Initial test compilation failed on four style findings: two redundant casts and
  two collection-expression simplifications. Only test syntax was corrected;
  no suppression was introduced. The initial source and failed log are retained.
- Corrected unchanged-product Release build: zero warnings/errors. Nine new
  Application cases ran: five passed and four failed at ordered-entry count
  comparisons after the SyncRoot mutation. The failing creation, navigation,
  visibility and sorting cases are preserved in `red-results/red.trx` (exit 2).
- Final Application Release build: zero warnings/errors. All 139 selected cases
  passed, zero failures/skips, including all nine new ownership cases. The filter
  covers PaneReducer, focus, selection, navigation/history, sort, session and
  Copy/Move/Delete/catalog consumers using their existing fake boundaries.
- Final Presentation Release build: zero warnings/errors. All 36 selected cases
  passed, zero failures/skips, including one new published-entry/cached-row case,
  row reuse, active focus, selection and automation projection.
- Both changed-file whitespace checks exit 0 with no warning output. Complexity
  Inspect exits 1 with exactly the same 26 findings as the accepted #504 record;
  full finding objects compare equal. No new finding. This is not a global
  CS-013 pass. PaneState has 132 logical lines; each private collection type 23.
- Self-review confirms one materialization, unchanged visibility predicate and
  canonical ownership, generic read/copy paths, rejected mutators and direct
  consumer compatibility. No independent-agent review is claimed.

Exact commands, arguments, timing and exits are in `red-commands.json`,
`red-corrected-commands.json`, `focused-commands.json` and `static-commands.json`
under `D:/NeNeCommander/evidence/visible-entry-ownership-20261121`.

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/visible-entry-ownership-20261121/run-red-corrected.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/visible-entry-ownership-20261121/run-focused.ps1
dotnet format whitespace tests/NeNeCommander.Application.Tests/NeNeCommander.Application.Tests.csproj --include src/NeNeCommander.Application/Panes/PaneState.cs tests/NeNeCommander.Application.Tests/PaneVisibleEntryOwnershipTests.cs --verify-no-changes --no-restore
dotnet format whitespace tests/NeNeCommander.Presentation.WinUI.Tests/NeNeCommander.Presentation.WinUI.Tests.csproj --include tests/NeNeCommander.Presentation.WinUI.Tests/PaneVisibleEntryPresentationTests.cs --verify-no-changes --no-restore
pwsh -NoProfile -File ./eng/measure-complexity.ps1 -Mode Inspect
```

The red command identifies historical unchanged-product evidence, not an instruction
to replace it with a current-source run. Successful behavior proof is reused while
its relevant inputs remain unchanged. Product App/native, all-layer coverage,
mutation and hosted CI are not selected for this bounded internal collection fix.

## Integration, retained evidence and remaining work

The PR closing #508 owns final ordinary Draft/Ready/squash integration. The
permanent `completion.json` records its actual merge, accepted-tree equality,
clean main, archive verification and exact task cleanup; none is inferred from a
successful test alone. Preserve branch/commits and permanent evidence.

The initial Issue write was rejected by automatic approval because the destination
and disclosure scope were unverified. Read-only repository metadata established
hide-owned public hosting/admin access and fetched the identical public source.
The same normal registration was then allowed but the connector returned 403;
authenticated gh created #508. No rejected disclosure was routed around the review.

PR402 remains separate pending hide's screen acceptance. #491's two stopped native
series remain unaccepted, and #453 is not resolved. The old #454/#459 four cleanup
targets are now absent: existing result records show completion at 23:53:59 and
23:56:26 on November 20, with fresh read-only absence confirmation on November 21.
No agent deletion retry was made for those historical rejections.
