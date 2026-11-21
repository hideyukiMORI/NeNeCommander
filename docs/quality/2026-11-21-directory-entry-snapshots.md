# Directory entry snapshot ownership — 2026-11-21

Issue: #510. Base: `f0f7b31558bcb361edee6367494a7adbebaf191b`.

## Invariant and canonical mechanism

Published directory entries must retain their contents and exact entry/path
references. `DirectoryListing.Create` still validates the count, null entries and
provider identities and applies its original lexical ordering. `EntryOrdering.Apply`
still applies the original pane sorting, including natural names. `PaneState.SelectVisible`
still applies the same visibility predicate. Their policies remain with these owners.

Issue #508's private VisibleEntrySnapshot is moved, without a competing copy, to
internal `Directories/DirectoryEntrySnapshot`. Its private List materializes each
owner's sequence once. It performs no validation, sorting, provider selection or
filesystem effect. This is one DirectoryEntry-specific ownership implementation,
not a new generic collection framework. PaneState's private ListedEntries input
and its copy policy, selection, focus, history and publication routes are unchanged.

Previously, DirectoryListing and EntryOrdering returned ReadOnlyCollection whose
non-generic ICollection.SyncRoot exposed the backing List. Clearing a published
listing left PaneState's visible snapshot intact, but OpenFocused could no longer
find the visible file in the listing. This is an in-process ownership defect;
it does not demonstrate a filesystem exploit or change external admission rules.

The declared IReadOnlyList surface, generic ICollection count/contains/copy,
indexing and both enumeration routes remain. Generic mutators throw. Concrete
ReadOnlyCollection, generic IList and non-generic IList/ICollection casts are no
longer supported for these published entry sequences. A tracked-source search
found no dependent consumer. Record copies retain equality and hash agreement;
separately owned snapshots still compare by their private List identity. No stable
numeric hash value, reflection/private-memory defense or concurrent caller-input
mutation guarantee is introduced.

ADV-011 and SEC-001 now state the published-snapshot ownership scope. Rules:
ARC-001/004/008, CMD-002/007, CS-007/008/009/012/013/015, SEC-001,
QLT-005/014/015, TST-002 and ADR-0061. No dependency, schema, UI, gate or waiver changes.

## Selected evidence

- Initial test build failed on two CA1861 constant-array arguments. Only the new
  test syntax was corrected; original source and log remain. Locked Release
  restore succeeded and the corrected unchanged-product build had zero warnings/errors.
- Eight new baseline cases ran: seven failed and one passed, zero skips. Three
  local/UNC/WSL listing-copy cases and the natural-order case failed on changed
  contents; three fake-port file-handoff cases failed because no target was launched.
  The generic read/copy/rejected-mutator case already passed. No real Shell launch.
- Final Application build: zero warnings/errors. The selected 212 cases pass,
  including all eight new cases, listing validation/bounds, lexical/natural sorting,
  pane ownership/reduction/navigation and Copy/Move/Delete source consumers.
- An initially misspelled optional file-launch class filter matched no additional
  tests. The existing actual PaneSessionFileLaunchTests were then run separately:
  all 14 pass. The two Application result sets have disjoint test cases.
- Presentation Release build: zero warnings/errors; 36 direct row reuse, focus,
  selection and automation projection cases pass.
- Infrastructure locked Release restore/build pass with zero warnings/errors;
  67 directory routing/read/reader cases pass. Local reads use existing test-owned
  roots; UNC and WSL contracts use scripted or locally backed enumerators, not live
  remote environments. Cancellation, bounds and normalization remain covered.
- Changed-file whitespace verification exits 0 without warning output.
- CS-013 Inspect exits 1 with the identical 26 full finding objects as #508's
  accepted evidence. No new finding; this is not a global complexity pass.
- Self-review confirms one materialization per owner, removal of the private
  duplicate, unchanged validation/predicates/comparers and generic collection
  read/copy compatibility. No independent-agent review is claimed.

Focused ordinary scope directly covers the shared entry-sequence owners and their
read/display/launch consumers. The implementation changes no path parser, provider
trust decision or filesystem-effect boundary. That bounded impact does not require
full/deep CI, all-layer coverage/mutation, native UI or release proof. No measured
speedup, UI latency improvement or live UNC/WSL guarantee is claimed.

Exact arguments, times and exit codes are retained in `red-commands.json`,
`red-corrected-commands.json`, `focused-commands.json` and `static-commands.json`
under `D:/NeNeCommander/evidence/entry-snapshots-20261121`.

```powershell
pwsh -NoProfile -File D:/NeNeCommander/evidence/entry-snapshots-20261121/run-red-corrected.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/entry-snapshots-20261121/run-focused.ps1
pwsh -NoProfile -File D:/NeNeCommander/evidence/entry-snapshots-20261121/run-static.ps1
```

These identify historical runs; do not overwrite baseline evidence by executing it
against changed product code. The static runner includes the separate 14 launch
cases, changed-file format and `./eng/measure-complexity.ps1 -Mode Inspect`.
Normal commits retain `./eng/check.ps1 -Mode Commit`. The 1,007 input hashes and
TRX records support reuse when relevant inputs are unchanged.

## Integration and remaining work

The PR closing #510 owns ordinary Draft/Ready/squash integration. Permanent
`completion.json` records the actual merge, accepted-tree equality, main sync,
archive verification and exact task cleanup. Branch/commits/evidence remain.
Do not infer integration or cleanup merely from successful tests.

Prior #508/PR509 merged normally as f0f7b31, with its accepted tree and all 700
artifacts preserved and verified; its exact worktree/output cleanup completed.
PR402 still needs hide's screen acceptance, #491's stopped comparisons remain
unaccepted, #453 remains unresolved and AppSDK PR498 is separate. Published beta.2
and the normal app are not replaced by this change. The older #454/#459 cleanup
targets were already confirmed absent; no rejected deletion is retried.
