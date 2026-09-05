# Daily report — incremental pane projection — 2026-09-05

Status: informational

## Scope

Issue #57 removes repeated whole-pane row projection and ItemsSource reset from focus, activity, and operation-progress updates. It does not change pane state, row-mark precedence, entry order, keyboard behavior, or visual resources. ADR-0028 records the incremental projection mechanism.

## Invariant and canonical mechanism

`PaneListingPresenter` remains the only mechanism that translates pane state into rows, focus, status, and address. Its owned `PaneRows` source retains provider-aware row indexes and replaces only affected immutable rows. `DualPanePresenter` composes the two pane results, and the App only applies their render-ready identities.

## Failure-first proof

`PresentWhenOnlyActivityChangesReusesRows` projected one listed pane, changed only its activity from idle to loading, and supplied the first presentation as the previous value. Before incremental reuse, the test failed because `initial.Rows` and `updated.Rows` were distinct `ReadOnlyCollection<PaneRow>` instances despite identical content.

## Same-process Release measurement

The ignored measurement harness warmed both paths once, then projected 100 snapshots in the same Release process and build. The baseline called the public one-shot projection for each update; the incremental path supplied the prior presentation. This isolates reuse from machine and build variation; the one-shot path also pays the new index construction cost, so it is a conservative compatibility-path comparison rather than a historical binary benchmark. Allocation is `GC.GetAllocatedBytesForCurrentThread` for the measured loop.

| Entries | Updates | One-shot baseline time | One-shot baseline bytes | Incremental time | Incremental bytes |
|---:|---:|---:|---:|---:|---:|
| 0 | 100 | 0.055 ms | 54,512 | 0.005 ms | 72 |
| 100 | 100 | 3.647 ms | 1,768,112 | 0.123 ms | 64,224 |
| 10,000 | 100 | 214.774 ms | 168,496,112 | 0.185 ms | 64,224 |

At 10,000 entries, measured projection time fell by 99.91% and thread allocation by 99.96%. The incremental allocation stayed independent of entry count after initial projection.

## Changes

- Added one stable read-only observable row source and provider-aware index per projected listing.
- Reused a complete pane presentation when snapshot and frame identity did not change.
- Replaced only prior/new focus rows and selection-difference rows for a retained listing.
- Retained the latest dual-pane presentation in the window and stopped redundant ItemsSource assignments.
- Updated the row contract to describe immutable per-row replacement instead of whole-list replacement.

## Focused verification

- Release locked restore and solution build: PASS, zero warnings.
- Presentation tests: PASS, 64/64.
- Application tests: PASS, 155/155.
- Architecture tests: PASS, 4/4.
- `eng/conformance.ps1 -Quiet`: PASS.
- `eng/security-check.ps1 -SkipProof`: PASS; all 18 adversarial cases remained registered.

The final Draft-to-Ready canonical CI gate is pending.
