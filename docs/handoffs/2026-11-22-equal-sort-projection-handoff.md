# Equal sort projection handoff — 2026-11-22

Issue #519. [Quality record](../quality/2026-11-22-equal-sort-projection.md) owns
commands and scope. Keep the equality guard in PaneState.WithSortOrder;
PaneReducer.ApplySortOrder must continue focus recovery even on equal orders.
Never reuse entries across a refreshed listing.

Application218/Presentation36 cases, zero-warning/error App builds and formatting
pass. The same26 existing CS-013 findings remain. All576 planned comparison rows
and1035 input hashes validate. Reuse successful evidence when relevant inputs
are unchanged. No native UI or full gate was selected.

Retain D:/NeNeCommander/worktrees/issue-519 and
D:/NeNeCommander/outputs/equal-sort-projection-20261122 until actual main integration
and artifact preservation are recorded. Permanent evidence is under
D:/NeNeCommander/evidence/equal-sort-projection-20261122. Before exact task cleanup,
check absolute paths, all tracked/untracked/ignored/unique files, live references
and links. Retain branch/commits/evidence.

PR402 hide screen acceptance, stopped #491/#453 native evidence, Draft AppSDK PR517
and Issue518 are separate. No normal App or release update is claimed.
