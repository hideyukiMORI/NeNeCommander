# Equal sort projection report — 2026-11-22

Issue #519 reuses existing pane projections for value-equal sort orders. The
reducer still resolves the requested focus; changed orders and fresh listings
retain their normal paths.

Six baseline failures establish the repeated allocation/rebuild contract.
Final Application218 and Presentation36 cases pass, App builds have zero
warnings/errors, formatting passes and the exact26 existing CS-013 findings remain.

The fixed A1/B1/B2/A2 comparison validates all576 planned records and1035 frozen
inputs. At10000 entries with equal default order and no preferred focus, allocation
falls401096 to96bytes/call; measured batch means move9.215600–9.910630 to
0.045935–0.059680ms/call. Preferred-focus work remains significant. These are
managed reducer costs, not native UI latency or whole-app speedup.

See [quality](../quality/2026-11-22-equal-sort-projection.md) for exact commands,
scope, complete comparison records and limitations. The closing PR/completion
record owns integration and cleanup. PR402 hide acceptance remains separate.
