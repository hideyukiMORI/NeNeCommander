# Incremental pane projection handoff — 2026-09-05

Status: informational

## Work item

- Issue: #57
- Branch: `perf/57-incremental-pane-projection`
- Decision: ADR-0028
- Invariant: row meaning and order remain presenter-owned; unchanged content retains its row source; only affected immutable rows are replaced.
- Canonical mechanism: `PaneListingPresenter` over `PaneRows`, composed by `DualPanePresenter`.

## Verification checkpoint

- Failure-first activity-only reuse test: failed on distinct row collections before the fix.
- Release build: PASS, zero warnings.
- Presentation tests: PASS, 64/64.
- Application 155 and Architecture 4: PASS.
- Conformance and security without negative fixtures: PASS; 18 adversarial cases remained registered.
- 10,000 entries × 100 updates: 214.774 ms / 168,496,112 bytes one-shot; 0.185 ms / 64,224 bytes incremental.

## Integration steps

1. Review path-index identity, focus/selection/frame affected-set logic, and observable row-source ownership.
2. Run conformance/security and Application/Presentation/Architecture dependency-impact tests.
3. Commit through the existing Commit-mode hook, push, and create a Draft PR closing #57.
4. Verify remote PR head equals local HEAD, mark the final candidate Ready, and require fresh canonical CI success on the latest base.
5. Squash merge, synchronize clean `main`, and do not duplicate the successful CI full gate locally.

## Remaining environmental proof

None. The optimized behavior is deterministic Presentation logic; the framework boundary only avoids assigning an identical ItemsSource and receives standard observable replacement notifications.
